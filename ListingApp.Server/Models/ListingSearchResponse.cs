namespace ListingApp.Server.Models;

public sealed record ListingSearchItem(Listing Listing, double RelevanceScore);

public sealed record ListingSearchResponse(
    IReadOnlyList<ListingSearchItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
