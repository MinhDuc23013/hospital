namespace PharmacyServiceDotnet.Domain.Exceptions;

/// <summary>Raised when a requested resource does not exist.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, Guid id) : base($"{resource} with id '{id}' was not found.") { }
}
