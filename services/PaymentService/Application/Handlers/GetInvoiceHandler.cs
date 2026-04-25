using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.Queries;
using PaymentService.Infrastructure.HttpClients;

namespace PaymentService.Application.Handlers;

/// <summary>
/// Aggregates billing data from LabTestService, ImagingService, and PharmacyService
/// into a single invoice for the given appointment.
/// </summary>
public class GetInvoiceHandler : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    private readonly LabTestServiceClient _labClient;
    private readonly ImagingServiceClient _imagingClient;
    private readonly PharmacyServiceClient _pharmacyClient;
    private readonly AppointmentServiceClient _appointmentClient;
    private readonly IConfiguration _config;

    public GetInvoiceHandler(
        LabTestServiceClient labClient,
        ImagingServiceClient imagingClient,
        PharmacyServiceClient pharmacyClient,
        AppointmentServiceClient appointmentClient,
        IConfiguration config)
    {
        _labClient = labClient;
        _imagingClient = imagingClient;
        _pharmacyClient = pharmacyClient;
        _appointmentClient = appointmentClient;
        _config = config;
    }

    public async Task<InvoiceDto> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var consultationFee = _config.GetValue<decimal>("InvoiceSettings:ConsultationFee", 150_000);

        // Fetch all data in parallel
        var (labOrders, imagingOrders, prescriptions) = await FetchAllAsync(query.AppointmentId, ct);

        var items = new List<InvoiceLineItem>();

        // Consultation fee
        items.Add(new InvoiceLineItem("Consultation", "Phí khám bệnh", 1, consultationFee, consultationFee));

        // Lab tests
        foreach (var order in labOrders)
        {
            foreach (var item in order.Items)
            {
                if (item.UnitPrice > 0)
                    items.Add(new InvoiceLineItem(
                        "Lab", $"Xét nghiệm: {item.TestName} ({item.TestCode})",
                        1, item.UnitPrice, item.UnitPrice));
            }
        }

        // Imaging
        foreach (var order in imagingOrders.Where(o => o.Price > 0))
        {
            items.Add(new InvoiceLineItem(
                "Imaging", $"Chẩn đoán hình ảnh: {order.Type} - {order.BodyPart}",
                1, order.Price, order.Price));
        }

        // Medications — fetch drug price for each unique drug
        var drugPriceCache = new Dictionary<Guid, decimal>();
        foreach (var prescription in prescriptions)
        {
            foreach (var item in prescription.Items)
            {
                if (!drugPriceCache.TryGetValue(item.DrugId, out var unitPrice))
                {
                    var drug = await _pharmacyClient.GetDrugAsync(item.DrugId, ct);
                    unitPrice = drug?.Price ?? 0;
                    drugPriceCache[item.DrugId] = unitPrice;
                }

                if (unitPrice > 0)
                    items.Add(new InvoiceLineItem(
                        "Medication", $"Thuốc: {item.DrugName}",
                        item.Quantity, unitPrice, unitPrice * item.Quantity));
            }
        }

        var total = items.Sum(i => i.Total);

        return new InvoiceDto(
            query.AppointmentId,
            query.PatientId,
            items,
            Subtotal: total,
            Total: total,
            Currency: "VND",
            GeneratedAt: DateTime.UtcNow);
    }

    private async Task<(List<LabOrderResponse>, List<ImagingOrderResponse>, List<PrescriptionResponse>)>
        FetchAllAsync(Guid appointmentId, CancellationToken ct)
    {
        var labTask = _labClient.GetByAppointmentAsync(appointmentId, ct);
        var imagingTask = _imagingClient.GetByAppointmentAsync(appointmentId, ct);
        var prescriptionTask = _pharmacyClient.GetByAppointmentAsync(appointmentId, ct);

        await Task.WhenAll(labTask, imagingTask, prescriptionTask);

        return (labTask.Result, imagingTask.Result, prescriptionTask.Result);
    }
}
