namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>Drug entity — represents a medication item in the pharmacy inventory.</summary>
public class Drug
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Dosage { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public int LowStockThreshold { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Drug() { } // EF Core

    public static Drug Create(string name, string code, string? dosage, int quantity, decimal price, int lowStockThreshold = 10)
    {
        return new Drug
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Dosage = dosage,
            Quantity = quantity,
            Price = price,
            LowStockThreshold = lowStockThreshold,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void Update(string? name, string? dosage, int? quantity, decimal? price, int? lowStockThreshold)
    {
        if (name is not null) Name = name;
        if (dosage is not null) Dosage = dosage;
        if (quantity.HasValue) Quantity = quantity.Value;
        if (price.HasValue) Price = price.Value;
        if (lowStockThreshold.HasValue) LowStockThreshold = lowStockThreshold.Value;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Decrements stock by the given amount. Returns true if stock is now below threshold.</summary>
    public bool UpdateStock(int quantity)
    {
        Quantity -= quantity;
        UpdatedAt = DateTime.Now;
        return Quantity < LowStockThreshold;
    }
}
