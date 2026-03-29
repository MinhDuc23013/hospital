using AppointmentService.Application.Commands;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.MessageBus;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.Events;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Marks appointment as completed after consultation. Doctor/admin action.</summary>
public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand>
{
    private readonly IAppointmentRepository _repo;
    private readonly EventPublisher _events;
    private readonly ILogger<CompleteAppointmentHandler> _logger;

    public CompleteAppointmentHandler(IAppointmentRepository repo, EventPublisher events, ILogger<CompleteAppointmentHandler> logger)
    {
        _repo = repo;
        _events = events;
        _logger = logger;
    }

    public async Task Handle(CompleteAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _repo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        if (appointment.Status == AppointmentStatus.Completed)
            return;

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new DomainException("Cannot complete a cancelled appointment.");

        appointment.Complete();
        await _repo.SaveChangesAsync(ct);

        await _events.PublishAsync(new AppointmentScheduledEvent
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            ScheduledTime = appointment.ScheduledTime,
            DurationMinutes = appointment.DurationMinutes,
            Status = "Completed"
        }, ct);

        _logger.LogInformation("Appointment {AppointmentId} marked as completed, event published", cmd.AppointmentId);
    }
}
