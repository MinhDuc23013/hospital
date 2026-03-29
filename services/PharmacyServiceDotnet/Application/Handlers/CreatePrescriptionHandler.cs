using MediatR;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Application.Commands;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.MessageBus;
using PharmacyServiceDotnet.Infrastructure.Repositories;
using HospitalShared.Events;

namespace PharmacyServiceDotnet.Application.Handlers;

public class CreatePrescriptionHandler : IRequestHandler<CreatePrescriptionCommand, PrescriptionResult>
{
    private readonly IPrescriptionRepository _repo;
    private readonly EventPublisher _publisher;

    public CreatePrescriptionHandler(IPrescriptionRepository repo, EventPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<PrescriptionResult> Handle(CreatePrescriptionCommand cmd, CancellationToken ct)
    {
        var prescription = Prescription.Create(cmd.PatientId, cmd.DoctorId, cmd.AppointmentId, cmd.Items, cmd.Notes);
        await _repo.AddAsync(prescription, ct);
        await _repo.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new PrescriptionIssuedEvent
        {
            PrescriptionId = prescription.Id,
            PatientId = prescription.PatientId,
            DrugId = string.Empty,
            Quantity = 0,
            Timestamp = DateTime.Now
        }, ct);

        return PharmacyMapper.ToResult(prescription);
    }
}
