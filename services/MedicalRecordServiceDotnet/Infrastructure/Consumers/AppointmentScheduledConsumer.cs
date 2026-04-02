using System.Text.Json;
using Confluent.Kafka;
using HospitalShared.Events;
using MedicalRecordServiceDotnet.Domain.Entities;
using MedicalRecordServiceDotnet.Infrastructure.Repositories;

namespace MedicalRecordServiceDotnet.Infrastructure.Consumers;

/// <summary>
/// Kafka consumer that listens for AppointmentScheduled events
/// and auto-creates an empty medical record for the appointment.
/// </summary>
public class AppointmentScheduledConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentScheduledConsumer> _logger;
    private readonly string _bootstrapServers;
    private const string Topic = "hospital.appointment-scheduled";
    private const string GroupId = "medical-record-service";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AppointmentScheduledConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<AppointmentScheduledConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentScheduledConsumer starting, topic={Topic}", Topic);

        await Task.Yield(); // Release startup thread

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null) continue;

                var evt = JsonSerializer.Deserialize<AppointmentScheduledEvent>(result.Message.Value, JsonOpts);
                if (evt is null) continue;

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IMedicalRecordRepository>();

                var record = new MedicalRecord
                {
                    PatientId = evt.PatientId.ToString(),
                    AppointmentId = evt.AppointmentId.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await repo.CreateAsync(record, stoppingToken);
                _logger.LogInformation(
                    "Auto-created medical record {RecordId} for appointment {AppointmentId}",
                    record.Id, evt.AppointmentId);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AppointmentScheduled event");
            }
        }

        consumer.Close();
    }
}
