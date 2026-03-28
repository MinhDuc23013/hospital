using HospitalShared.Constants;
using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using PatientService.Application.Commands;
using PatientService.Domain.Entities;
using PatientService.Infrastructure.MessageBus;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class CreatePatientHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repo;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<CreatePatientHandler> _logger;

    public CreatePatientHandler(
        IPatientRepository repo, EventPublisher events,
        NotificationPublisher notifications, ILogger<CreatePatientHandler> logger)
    {
        _repo = repo;
        _events = events;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand cmd, CancellationToken ct)
    {
        var patient = Patient.Create(cmd.Email, cmd.FirstName, cmd.LastName, cmd.DateOfBirth, cmd.PhoneNumber);
        await _repo.AddAsync(patient, ct);
        await _repo.SaveChangesAsync(ct);
        _logger.LogInformation("[STEP 1] Patient saved to DB: {PatientId}", patient.Id);

        var @event = new PatientCreatedEvent
        {
            PatientId = patient.Id,
            Email = patient.Email,
            FirstName = patient.FirstName,
            LastName = patient.LastName
        };

        try
        {
            // Kafka — audit/trace event (SearchService index, analytics)
            await _events.PublishAsync(@event, EventTypes.PatientCreated, ct);
            _logger.LogInformation("[STEP 2] PatientCreatedEvent published to Kafka: {PatientId}", patient.Id);

            // RabbitMQ — trigger welcome email/SMS via NotificationService
            await _notifications.SendNotificationAsync(@event, ct);
            _logger.LogInformation("[STEP 3] PatientCreatedEvent sent to RabbitMQ: {PatientId}", patient.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STEP 2-3 FAILED] Failed to publish PatientCreatedEvent: {PatientId}", patient.Id);
        }

        return MapToDto(patient);
    }

    internal static PatientDto MapToDto(Patient p) => new()
    {
        Id = p.Id,
        Email = p.Email,
        FirstName = p.FirstName,
        LastName = p.LastName,
        DateOfBirth = DateOnly.FromDateTime(p.DateOfBirth),
        PhoneNumber = p.PhoneNumber,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
