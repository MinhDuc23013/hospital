# Kafka DLQ Operations Guide

Dead-letter queue (DLQ) pattern for Kafka consumers — how it works, when it triggers, how to replay from DLQ.

## Architecture

```
Producer → hospital.payment-completed ─┬─→ Consumer (PaymentCompletedConsumer)
                                       │     │
                                       │     ├─ Attempt 1 → fail
                                       │     ├─ Attempt 2 → fail (backoff 1s)
                                       │     ├─ Attempt 3 → fail (backoff 2s)
                                       │     └─ Publish to DLQ → commit offset
                                       │
                                       └─→ hospital.payment-completed.dlq
                                               (poison messages land here)
```

## How it triggers

Handler inside consumer throws → `KafkaConsumerRetryHelper.HandleWithDlqAsync` wraps with:

1. Try handler — if success → commit, done
2. If throw → log warning, wait backoff, retry (attempts 2, 3)
3. If still failing after `MaxAttempts = 3` → publish original message to `{topic}.dlq`
4. Commit offset on main topic → partition unblocks, next message consumed
5. Ops sees message in DLQ topic → inspects + decides

**NOT DLQ-worthy (handler returns cleanly, no throw):**
- `null` payload / missing required field → log warning, skip, commit
- Duplicate / idempotency short-circuit → log info, skip, commit

**DLQ-worthy (handler throws):**
- JSON deserialization failure (schema drift)
- External service (ES, DB) errors that survive 3 retries
- Unexpected code bugs

## DLQ message format

Kafka message published to `{topic}.dlq` with **identical key/value** but enriched headers:

| Header | Value |
|---|---|
| `x-original-topic` | `hospital.payment-completed` |
| `x-original-partition` | `0` |
| `x-original-offset` | `42` |
| `x-consumer-group` | `appointment-service` |
| `x-failure-reason` | Exception message (truncated 1000 chars) |
| `x-failure-type` | Exception class full name |
| `x-attempts` | `3` |
| `x-failed-at` | ISO-8601 UTC timestamp |
| `x-original-<h>` | Any header from original message (e.g., `outbox-id`) |

## Monitor DLQ

### Kafka UI

```
http://localhost:9080
  → Topics → search: ".dlq"
```

Sẽ thấy:
- `hospital.payment-completed.dlq`
- `hospital.appointment-scheduled.dlq`
- `hospital.patient-created.dlq`
- `hospital.patient-updated.dlq`
- `hospital.slot-reserved.dlq`

Topic DLQ được **tạo lazy** — chỉ xuất hiện sau lần đầu có message fail.

### Grafana dashboard

"Kafka Consumer Lag" dashboard sẽ show panel "Produce Rate per Topic" — DLQ topic có produce rate > 0 = có poison message vừa mới ship.

### Alert rule (gợi ý thêm vào `infra/grafana/provisioning/alerting/alerts.yml`)

```yaml
- alert: KafkaDlqMessageReceived
  expr: sum by (topic) (rate(kafka_topic_partition_current_offset{topic=~".+\\.dlq"}[5m])) > 0
  for: 1m
  annotations:
    summary: "Poison message landed in DLQ topic {{$labels.topic}} — investigate"
```

## Replay workflow

### Bước 1: Inspect message trong DLQ

```
Kafka UI → Topics → hospital.payment-completed.dlq → Messages
→ Click message → xem Key, Value, Headers
→ Headers cho biết: offset gốc, lý do fail, timestamp
```

### Bước 2: Phân loại

| Tình huống | Action |
|---|---|
| Bug code đã fix, muốn chạy lại | Re-produce sang topic gốc |
| Message format sai, không fix được | Archive + để nguyên (không replay) |
| External dependency đã recover (ES was down) | Re-produce sang topic gốc |
| Data đã được xử manual qua API | Archive + bỏ |

### Bước 3: Re-produce sang topic gốc

#### Option A — Kafka UI (1 message)

```
1. Topic dlq → Messages → click message cần replay
2. Copy Value (JSON payload)
3. Sang topic gốc (vd: hospital.payment-completed)
4. Click "Produce Message"
5. Paste Value, giữ nguyên Key (copy từ DLQ message)
6. Submit
```

→ Consumer chính sẽ lấy message này ở offset mới → xử bình thường → (hy vọng) thành công vì code đã fix.

#### Option B — CLI / script (bulk replay)

```bash
# Dump DLQ → file
docker exec hospital-kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic hospital.payment-completed.dlq \
  --from-beginning --max-messages 100 \
  --property print.key=true \
  --property print.value=true \
  --property key.separator="|" > dlq-dump.txt

# Re-produce tường dòng sang topic chính
while IFS='|' read -r key value; do
  echo "$key|$value" | docker exec -i hospital-kafka /opt/kafka/bin/kafka-console-producer.sh \
    --bootstrap-server localhost:9092 \
    --topic hospital.payment-completed \
    --property parse.key=true \
    --property key.separator="|"
done < dlq-dump.txt
```

#### Option C — Admin endpoint (build khi cần)

Thêm endpoint trong service: `POST /admin/dlq/replay` → nhận `topic`, `offset` → đọc DLQ, re-produce message sang topic chính. Chưa implement, đề xuất nếu DLQ trở thành ops routine.

### Bước 4: Verify

- Grafana dashboard "Kafka Consumer Lag" → lag của consumer chính tăng tạm, giảm về 0 sau khi xử xong
- Check DB/ES xem data đã được tạo/update đúng chưa
- Log consumer không xuất hiện exception nữa

### Bước 5: Clean up DLQ (optional)

DLQ topic cũng có retention. Có thể:
- Để mặc định 7 ngày tự xóa
- Xóa message đã replay thành công bằng cách reset consumer group của "DLQ-viewer" (nếu có) → skip qua
- Không có cách "xóa 1 message cụ thể" — Kafka append-only

## Tuning

### MaxAttempts per consumer

Trong từng consumer, sửa `MaxAttempts = 3` nếu cần khác:

```csharp
private const int MaxAttempts = 5; // payment events cần retry nhiều hơn
```

### Backoff

Helper dùng exponential backoff: 1s → 2s → 4s. Chỉnh qua param `initialBackoff`:

```csharp
await KafkaConsumerRetryHelper.HandleWithDlqAsync(
    result, handler, _dlq, GroupId, _logger, ct,
    maxAttempts: MaxAttempts,
    initialBackoff: TimeSpan.FromSeconds(5));
```

### DLQ topic retention (nếu muốn khác default)

```bash
docker exec hospital-kafka /opt/kafka/bin/kafka-configs.sh \
  --bootstrap-server localhost:9092 \
  --entity-type topics --entity-name hospital.payment-completed.dlq \
  --alter --add-config retention.ms=2592000000   # 30 ngày
```

## Consumer nào đã wire DLQ

| Service | Consumer | Topic | DLQ topic |
|---|---|---|---|
| AppointmentService | `PaymentCompletedConsumer` | `hospital.payment-completed` | `hospital.payment-completed.dlq` |
| SearchServiceDotnet | `PaymentEventConsumer` | `hospital.payment-completed` | `hospital.payment-completed.dlq` |
| SearchServiceDotnet | `AppointmentEventConsumer` | `hospital.appointment-scheduled` | `hospital.appointment-scheduled.dlq` |
| SearchServiceDotnet | `PatientEventConsumer` | `hospital.patient-{created,updated}` | `hospital.patient-{created,updated}.dlq` |
| SearchServiceDotnet | `SlotEventConsumer` | `hospital.slot-reserved` | `hospital.slot-reserved.dlq` |

**Chưa wire:** `MedicalRecordServiceDotnet/AppointmentScheduledConsumer` — dùng `EnableAutoCommit = true`, cần refactor sang manual commit trước khi thêm DLQ.

## Gotchas

- **Idempotency quan trọng** — replay = chạy lại handler. Handler phải idempotent (check trạng thái trước khi tạo record).
- **DLQ publish fail** = không commit offset → message redeliver poll tiếp theo → retry cycle tiếp tục. Log CRITICAL khi DLQ publish fail.
- **Retry state in-memory** — consumer restart giữa retry loop → state mất, lần poll tiếp theo đếm lại từ 1. Worst case: 2× MaxAttempts trước khi vào DLQ.
- **Không có ordering guarantee** khi message bị ship sang DLQ rồi replay — offset mới, khác timestamp gốc.
- **Handler long-running** — retry loop chạy trong cùng consumer thread → partition bị block suốt thời gian retry. Nếu handler mất 30s × 3 retry = 90s, consumer không consume message khác trong partition đó.

## Unresolved questions

- Có cần wire DLQ cho `MedicalRecordServiceDotnet` không? Hiện đang `EnableAutoCommit = true` — khác pattern, chưa refactor.
- Muốn auto-replay từ DLQ sau khi deploy fix không? Cần background worker riêng đọc DLQ → re-produce sang topic chính (nguy hiểm nếu lỗi chưa fix thật).
- DLQ alert hiện chỉ là đề xuất — cần quyết channel nhận (Slack, email) và threshold.
