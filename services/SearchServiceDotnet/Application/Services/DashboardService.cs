using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Application.Services;

/// <summary>
/// Computes dashboard metrics via Elasticsearch count and aggregation queries.
/// All date comparisons use UTC to stay consistent with stored document timestamps.
/// Queries run concurrently; each helper returns 0 on failure (non-fatal).
/// </summary>
public class DashboardService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<DashboardService> _logger;

    private const string AppointmentIndex = "hospital-appointments";
    private const string PaymentIndex     = "hospital-payments";
    private const string PatientIndex     = "hospital-patients";
    private const string SlotIndex        = "hospital-slots";

    public DashboardService(ElasticsearchClient client, ILogger<DashboardService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Returns all dashboard metrics in one call using parallel ES queries.
    /// </summary>
    public async Task<DashboardMetrics> GetDashboardAsync(CancellationToken ct = default)
    {
        var now        = DateTime.Now;
        var todayStart = now.Date;
        var todayEnd   = todayStart.AddDays(1);
        var weekStart  = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        // Run all four queries concurrently
        var t1 = CountTodayAppointmentsAsync(todayStart, todayEnd, ct);
        var t2 = SumMonthRevenueAsync(monthStart, ct);
        var t3 = CountWeekNewPatientsAsync(weekStart, ct);
        var t4 = CountTodayAvailableSlotsAsync(todayStart, todayEnd, ct);

        await Task.WhenAll(t1, t2, t3, t4);

        return new DashboardMetrics
        {
            TodayAppointments   = t1.Result,
            MonthRevenue        = t2.Result,
            WeekNewPatients     = t3.Result,
            TodayAvailableSlots = t4.Result
        };
    }

    // ── Today appointments ────────────────────────────────────────────────────

    private async Task<long> CountTodayAppointmentsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            var response = await _client.CountAsync<AppointmentDocument>(c => c
                .Indices(AppointmentIndex)
                .Query(q => q
                    .Range(r => r
                        .DateRange(d => d
                            .Field(f => f.ScheduledTime)
                            .Gte(from.ToString("O"))
                            .Lt(to.ToString("O"))
                        )
                    )
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("TodayAppointments count failed: {Debug}", response.DebugInformation);
                return 0;
            }

            return response.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting today appointments");
            return 0;
        }
    }

    // ── Month revenue ─────────────────────────────────────────────────────────

    private async Task<decimal> SumMonthRevenueAsync(DateTime monthStart, CancellationToken ct)
    {
        try
        {
            var response = await _client.SearchAsync<PaymentDocument>(s => s
                .Indices(PaymentIndex)
                .Size(0)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t
                                .Field(f => f.Status)
                                .Value("Completed")),
                            m => m.Range(r => r
                                .DateRange(d => d
                                    .Field(f => f.CreatedAt)
                                    .Gte(monthStart.ToString("O"))
                                )
                            )
                        )
                    )
                )
                .Aggregations(a => a
                    .Sum("total_revenue", sv => sv
                        .Field(f => f.Amount)
                    )
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("MonthRevenue aggregation failed: {Debug}", response.DebugInformation);
                return 0m;
            }

            var sumAgg = response.Aggregations?.GetSum("total_revenue");
            return (decimal)(sumAgg?.Value ?? 0.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summing month revenue");
            return 0m;
        }
    }

    // ── Week new patients ─────────────────────────────────────────────────────

    private async Task<long> CountWeekNewPatientsAsync(DateTime weekStart, CancellationToken ct)
    {
        try
        {
            var response = await _client.CountAsync<PatientDocument>(c => c
                .Indices(PatientIndex)
                .Query(q => q
                    .Range(r => r
                        .DateRange(d => d
                            .Field(f => f.CreatedAt)
                            .Gte(weekStart.ToString("O"))
                        )
                    )
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("WeekNewPatients count failed: {Debug}", response.DebugInformation);
                return 0;
            }

            return response.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting week new patients");
            return 0;
        }
    }

    // ── Today available slots ─────────────────────────────────────────────────

    private async Task<long> CountTodayAvailableSlotsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            var response = await _client.CountAsync<SlotDocument>(c => c
                .Indices(SlotIndex)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Range(r => r
                                .DateRange(d => d
                                    .Field(f => f.ScheduledTime)
                                    .Gte(from.ToString("O"))
                                    .Lt(to.ToString("O"))
                                )
                            )
                        )
                        .MustNot(
                            mn => mn.Term(t => t
                                .Field(f => f.Status)
                                .Value("Booked"))
                        )
                    )
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("TodayAvailableSlots count failed: {Debug}", response.DebugInformation);
                return 0;
            }

            return response.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting today available slots");
            return 0;
        }
    }
}

// ── Response DTO ──────────────────────────────────────────────────────────────

/// <summary>
/// Flat metrics object returned by GET /api/dashboard.
/// Property names are serialized as camelCase by ASP.NET Core's default JSON options.
/// </summary>
public sealed class DashboardMetrics
{
    public long    TodayAppointments   { get; set; }
    public decimal MonthRevenue        { get; set; }
    public long    WeekNewPatients     { get; set; }
    public long    TodayAvailableSlots { get; set; }
}
