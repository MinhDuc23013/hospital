using HospitalShared.Constants;
using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using PatientService.Application.Commands;
using PatientService.Domain.Entities;
using PatientService.Infrastructure.HttpClients;
using PatientService.Infrastructure.MessageBus;
using PatientService.Infrastructure.Repositories;

namespace PatientService.Application.Handlers;

public class CreatePatientHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _repo;
    private readonly AuthServiceClient _authService;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly ILogger<CreatePatientHandler> _logger;

    public CreatePatientHandler(
        IPatientRepository repo, AuthServiceClient authService, EventPublisher events,
        NotificationPublisher notifications, ILogger<CreatePatientHandler> logger)
    {
        _repo = repo;
        _authService = authService;
        _events = events;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand cmd, CancellationToken ct)
    {
        string? keycloakUserId = null;

        // Step 1: Create Keycloak user via Auth Service (if password provided)
        if (!string.IsNullOrWhiteSpace(cmd.Password))
        {
            keycloakUserId = await _authService.CreateUserAsync(
                cmd.Email, cmd.Password, cmd.FirstName, cmd.LastName, "patient", ct);
            _logger.LogInformation("Keycloak user created for patient {Email}: {KeycloakUserId}", cmd.Email, keycloakUserId);
        }

        try
        {
            // Step 2: Create Patient record in DB
            var patient = Patient.Create(cmd.Email, cmd.FirstName, cmd.LastName, cmd.DateOfBirth, cmd.PhoneNumber, keycloakUserId);
            await _repo.AddAsync(patient, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("[STEP 1] Patient saved to DB: {PatientId} KeycloakUserId={KeycloakUserId}", patient.Id, keycloakUserId);

            // Step 3: Publish events
            var @event = new PatientCreatedEvent
            {
                PatientId = patient.Id,
                Email = patient.Email,
                FirstName = patient.FirstName,
                LastName = patient.LastName
            };

            try
            {
                await _events.PublishAsync(@event, EventTypes.PatientCreated, ct);
                _logger.LogInformation("[STEP 2] PatientCreatedEvent published to Kafka: {PatientId}", patient.Id);

                await _notifications.SendNotificationAsync(@event, ct);
                _logger.LogInformation("[STEP 3] PatientCreatedEvent sent to RabbitMQ: {PatientId}", patient.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STEP 2-3 FAILED] Failed to publish PatientCreatedEvent: {PatientId}", patient.Id);
            }

            return MapToDto(patient);
        }
        catch when (keycloakUserId != null)
        {
            // Compensation: delete Keycloak user if DB save fails
            _logger.LogWarning("DB save failed, compensating Keycloak user {KeycloakUserId}", keycloakUserId);
            await _authService.DeleteUserAsync(keycloakUserId, ct);
            throw;
        }
    }

    internal static PatientDto MapToDto(Patient p) => new()
    {
        Id = p.Id,
        KeycloakUserId = p.KeycloakUserId,
        Email = p.Email,
        FirstName = p.FirstName,
        LastName = p.LastName,
        DateOfBirth = DateOnly.FromDateTime(p.DateOfBirth),
        PhoneNumber = p.PhoneNumber,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
