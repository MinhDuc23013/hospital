using LabTestService.Application.DTOs;
using LabTestService.Domain.Enums;
using MediatR;

namespace LabTestService.Application.Commands;

public record LabResultInput(
    Guid ItemId,
    string Result,
    string? NormalRange,
    string? Unit,
    LabResultStatus ResultStatus);

public record SubmitLabResultsCommand(Guid OrderId, List<LabResultInput> Results) : IRequest<LabOrderDto>;
