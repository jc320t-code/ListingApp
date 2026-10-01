namespace ListingApp.Server.Services;

public sealed class ListingSearchValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more search parameters are invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
