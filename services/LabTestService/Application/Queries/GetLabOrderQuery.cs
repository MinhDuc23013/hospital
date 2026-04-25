using LabTestService.Application.DTOs;
using MediatR;

namespace LabTestService.Application.Queries;

public record GetLabOrderQuery(Guid Id) : IRequest<LabOrderDto?>;
