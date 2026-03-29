namespace PharmacyServiceDotnet.Domain.Exceptions;

/// <summary>Raised when a business rule is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
