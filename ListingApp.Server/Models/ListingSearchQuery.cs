namespace ListingApp.Server.Models;

public sealed class ListingSearchQuery
{
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public string? City { get; init; }
    public string? Keyword { get; init; }
    public decimal? TargetBudget { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
