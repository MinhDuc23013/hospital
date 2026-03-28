using MediatR;

namespace PatientService.Application.Commands;

public record DeletePatientCommand(Guid Id) : IRequest;
