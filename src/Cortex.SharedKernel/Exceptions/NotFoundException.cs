namespace Cortex.SharedKernel.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found in the data store.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
    }
}
