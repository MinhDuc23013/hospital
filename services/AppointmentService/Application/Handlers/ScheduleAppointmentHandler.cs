using AppointmentService.Application.Commands;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.HttpClients;
using AppointmentService.Infrastructure.MessageBus;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;

namespace AppointmentService.Application.Handlers;

public class ScheduleAppointmentHandler : IRequestHandler<ScheduleAppointmentCommand, AppointmentDto>
{
    private readonly IAppointmentRepository _repo;
    private readonly EventPublisher _events;
    private readonly NotificationPublisher _notifications;
    private readonly PatientServiceClient _patientClient;

    public ScheduleAppointmentHandler(
        IAppointmentRepository repo, EventPublisher events,
        NotificationPublisher notifications, PatientServiceClient patientClient)
    {
        _repo = repo;
        _events = events;
        _notifications = notifications;
        _patientClient = patientClient;
    }

    public async Task<AppointmentDto> Handle(ScheduleAppointmentCommand cmd, CancellationToken ct)
    {
        // Validate patient exists (graceful degradation if PatientService unavailable)
        var patient = await _patientClient.GetPatientAsync(cmd.PatientId, ct);
        if (patient is null)
            throw new DomainException($"Patient {cmd.PatientId} not found or PatientService unavailable.");

        var appointment = Appointment.Create(
            cmd.PatientId, cmd.ProviderId, cmd.ScheduledTime, cmd.DurationMinutes, cmd.Notes);
        await _repo.AddAsync(appointment, ct);
        await _repo.SaveChangesAsync(ct);

        var @event = new AppointmentScheduledEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            ProviderId = appointment.ProviderId,
            ScheduledTime = appointment.ScheduledTime,
            DurationMinutes = appointment.DurationMinutes
        };

        // Kafka — audit/trace event
        await _events.PublishAsync(@event, ct);
        // RabbitMQ — trigger SMS/email notification
        await _notifications.SendNotificationAsync(@event, ct);

        return MapToDto(appointment);
    }

    internal static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        ProviderId = a.ProviderId,
        ScheduledTime = a.ScheduledTime,
        DurationMinutes = a.DurationMinutes,
        Status = a.Status.ToString(),
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };
}
