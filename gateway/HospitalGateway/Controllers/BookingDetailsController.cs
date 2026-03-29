using HospitalGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalGateway.Controllers;

/// <summary>
/// Aggregate endpoint that combines appointment, patient, doctor, and payment
/// data into a single response — avoids multiple round-trips from the client.
/// </summary>
[ApiController]
[Route("api/booking-details")]
[Authorize]
public class BookingDetailsController : ControllerBase
{
    private readonly BookingAggregationService _service;

    public BookingDetailsController(BookingAggregationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns aggregated booking details for a given appointment ID.
    /// Partial results are returned when downstream services are unavailable.
    /// </summary>
    [HttpGet("{appointmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BookingDetailsResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid appointmentId, CancellationToken ct)
    {
        var result = await _service.GetBookingDetailsAsync(appointmentId, ct);

        if (result is null)
            return NotFound(new { message = "Appointment not found" });

        return Ok(result);
    }
}
