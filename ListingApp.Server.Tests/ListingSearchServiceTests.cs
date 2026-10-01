using ListingApp.Server.Models;
using ListingApp.Server.Services;

namespace ListingApp.Server.Tests;

public sealed class ListingSearchServiceTests
{
    private static readonly DateOnly Today = new(2026, 9, 4);
    private readonly ListingSearchService _service = new(new FixedTimeProvider(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Search_FiltersCityAndDescriptionKeywordCaseInsensitively()
    {
        var listings = new[]
        {
            CreateListing("A1", "Springfield", "Bright condo near shops", 450000, 2, Today),
            CreateListing("A2", "Springfield", "Updated fenced yard", 525000, 3, Today),
            CreateListing("A3", "Fairfax", "Bright starter home", 399000, 2, Today)
        };

        var result = _service.Search(listings, new ListingSearchQuery { City = " springFIELD ", Keyword = "BRIGHT" });

        var item = Assert.Single(result.Items);
        Assert.Equal("A1", item.Listing.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void Search_RanksBudgetFitAndRecencyWithExpectedWeights()
    {
        var listings = new[]
        {
            CreateListing("A1", "Springfield", "First listing", 100000, 2, Today),
            CreateListing("A2", "Springfield", "Second listing", 150000, 2, Today.AddDays(-10))
        };

        var result = _service.Search(listings, new ListingSearchQuery { TargetBudget = 100000 });

        Assert.Equal("A1", result.Items[0].Listing.Id);
        Assert.Equal(100, result.Items[0].RelevanceScore);
        Assert.Equal(65.56, result.Items[1].RelevanceScore);
    }

    [Fact]
    public void Search_WithoutBudgetRanksOnlyByRecency()
    {
        var listings = new[]
        {
            CreateListing("OLDER", "Springfield", "Older listing", 100000, 2, Today.AddDays(-91)),
            CreateListing("NEWER", "Springfield", "Newer listing", 900000, 2, Today)
        };

        var result = _service.Search(listings, new ListingSearchQuery());

        Assert.Equal("NEWER", result.Items[0].Listing.Id);
        Assert.Equal(100, result.Items[0].RelevanceScore);
        Assert.Equal(0, result.Items[1].RelevanceScore);
    }

    [Fact]
    public void Search_PaginatesAndReportsTotals()
    {
        var listings = Enumerable.Range(1, 5)
            .Select(index => CreateListing($"A{index}", "Springfield", "Home", 100000, 2, Today.AddDays(-index)))
            .ToArray();

        var result = _service.Search(listings, new ListingSearchQuery { Page = 2, PageSize = 2 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public void Search_RejectsInvalidPriceBudgetBedroomAndPaginationInputs()
    {
        var query = new ListingSearchQuery
        {
            MinPrice = 500000,
            MaxPrice = 400000,
            MinBedrooms = -1,
            TargetBudget = 0,
            Page = 0,
            PageSize = 0
        };

        var exception = Assert.Throws<ListingSearchValidationException>(() => _service.Search([], query));

        Assert.Contains(nameof(query.MinPrice), exception.Errors.Keys);
        Assert.Contains(nameof(query.MinBedrooms), exception.Errors.Keys);
        Assert.Contains(nameof(query.TargetBudget), exception.Errors.Keys);
        Assert.Contains(nameof(query.Page), exception.Errors.Keys);
        Assert.Contains(nameof(query.PageSize), exception.Errors.Keys);
    }

    private static Listing CreateListing(string id, string city, string description, decimal price, int bedrooms, DateOnly listedDate) => new()
    {
        Id = id,
        Source = "MLS_A",
        Address = $"{id} Main Street",
        City = city,
        State = "VA",
        Zip = "22150",
        Price = price,
        Bedrooms = bedrooms,
        Bathrooms = 1,
        Sqft = 900,
        Latitude = 38.7893,
        Longitude = -77.1873,
        ListedDate = listedDate,
        Status = "active",
        Description = description
    };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
