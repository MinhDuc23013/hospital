using ImagingService.Application.DTOs;
using MediatR;

namespace ImagingService.Application.Commands;

public record SubmitImagingResultCommand(
    Guid OrderId,
    string Findings,
    string Impression,
    string? ImageUrl,
    string ReportedBy
) : IRequest<ImagingOrderDto>;
