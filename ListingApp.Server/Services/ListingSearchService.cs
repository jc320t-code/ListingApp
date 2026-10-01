using ListingApp.Server.Models;

namespace ListingApp.Server.Services;

public sealed class ListingSearchService(TimeProvider timeProvider)
{
    private const double RecencyWindowDays = 90;
    private const int MaxPageSize = 100;

    public ListingSearchResponse Search(IReadOnlyList<Listing> listings, ListingSearchQuery query)
    {
        Validate(query);

        var city = query.City?.Trim();
        var keyword = query.Keyword?.Trim();
        var matches = listings
            .Where(listing => !query.MinPrice.HasValue || listing.Price >= query.MinPrice.Value)
            .Where(listing => !query.MaxPrice.HasValue || listing.Price <= query.MaxPrice.Value)
            .Where(listing => !query.MinBedrooms.HasValue || listing.Bedrooms >= query.MinBedrooms.Value)
            .Where(listing => string.IsNullOrEmpty(city) || listing.City.Contains(city, StringComparison.OrdinalIgnoreCase))
            .Where(listing => string.IsNullOrEmpty(keyword) || listing.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var scored = matches
            .Select(listing =>
            {
                var ageInDays = Math.Max(0, (today.ToDateTime(TimeOnly.MinValue) - listing.ListedDate.ToDateTime(TimeOnly.MinValue)).TotalDays);
                var recencyScore = Math.Clamp(100 * (1 - ageInDays / RecencyWindowDays), 0, 100);
                var score = query.TargetBudget is decimal targetBudget
                    ? (0.6 * BudgetFitScore(listing.Price, targetBudget)) + (0.4 * recencyScore)
                    : recencyScore;

                return new ListingSearchItem(listing, Math.Round(score, 2));
            })
            .OrderByDescending(item => item.RelevanceScore)
            .ThenByDescending(item => item.Listing.ListedDate)
            .ThenBy(item => item.Listing.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Listing.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var totalCount = scored.Length;
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        var offset = (long)(query.Page - 1) * query.PageSize;
        var items = offset >= totalCount
            ? Array.Empty<ListingSearchItem>()
            : scored.Skip((int)offset).Take(query.PageSize).ToArray();

        return new ListingSearchResponse(items, query.Page, query.PageSize, totalCount, totalPages);
    }

    private static double BudgetFitScore(decimal price, decimal targetBudget)
    {
        var relativeDifference = (double)(Math.Abs(price - targetBudget) / targetBudget);
        return Math.Max(0, 100 * (1 - relativeDifference));
    }

    private static void Validate(ListingSearchQuery query)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        static void AddError(Dictionary<string, List<string>> errors, string field, string message)
        {
            if (!errors.TryGetValue(field, out var fieldErrors))
            {
                fieldErrors = [];
                errors[field] = fieldErrors;
            }

            fieldErrors.Add(message);
        }

        if (query.MinPrice < 0)
        {
            AddError(errors, nameof(query.MinPrice), "Minimum price must be zero or greater.");
        }

        if (query.MaxPrice < 0)
        {
            AddError(errors, nameof(query.MaxPrice), "Maximum price must be zero or greater.");
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
        {
            AddError(errors, nameof(query.MinPrice), "Minimum price cannot exceed maximum price.");
        }

        if (query.MinBedrooms < 0)
        {
            AddError(errors, nameof(query.MinBedrooms), "Minimum bedrooms must be zero or greater.");
        }

        if (query.TargetBudget <= 0)
        {
            AddError(errors, nameof(query.TargetBudget), "Target budget must be greater than zero.");
        }

        if (query.Page <= 0)
        {
            AddError(errors, nameof(query.Page), "Page must be greater than zero.");
        }

        if (query.PageSize <= 0 || query.PageSize > MaxPageSize)
        {
            AddError(errors, nameof(query.PageSize), $"Page size must be between 1 and {MaxPageSize}.");
        }

        if (errors.Count > 0)
        {
            throw new ListingSearchValidationException(errors.ToDictionary(
                error => error.Key,
                error => error.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase));
        }
    }
}
