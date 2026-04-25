using HospitalShared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Queries;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments/invoices")]
[Authorize(Roles = Roles.AdminDoctorReceptionistPatient)]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Generate an invoice for an appointment by aggregating costs from
    /// Lab, Imaging, Pharmacy, and adding the consultation fee.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await _mediator.Send(new GetInvoiceQuery(request.AppointmentId, request.PatientId), ct);
        return Ok(invoice);
    }
}

public record GenerateInvoiceRequest(Guid AppointmentId, Guid PatientId);
