using AppointmentService.Application.Saga;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Repositories;

namespace AppointmentService.Infrastructure.BackgroundJobs;

/// <summary>
/// Cancels bookings that have been awaiting payment for too long (default 30 minutes).
/// Runs every 60 seconds, finds expired sagas, and triggers compensation.
/// </summary>
public class PaymentTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentTimeoutWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(30);

    public PaymentTimeoutWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentTimeoutWorker started (timeout={Timeout}min, poll={Poll}s)",
            PaymentTimeout.TotalMinutes, PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentTimeoutWorker error");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task CheckExpiredPaymentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sagaRepo = scope.ServiceProvider.GetRequiredService<IBookingSagaRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<BookingSagaOrchestrator>();

        var cutoff = DateTime.Now.Subtract(PaymentTimeout);
        var expiredSagas = await sagaRepo.GetByStepOlderThanAsync(BookingSagaStep.AwaitingPayment, cutoff, ct);

        foreach (var saga in expiredSagas)
        {
            _logger.LogWarning("Saga {SagaId} payment timed out (created {CreatedAt})", saga.Id, saga.CreatedAt);
            await orchestrator.CancelExpiredAsync(saga.Id, ct);
        }
    }
}
