namespace Cortex.Shared.Exceptions;

/// <summary>
/// Thrown when an authenticated user lacks permission for a specific action.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }
}
