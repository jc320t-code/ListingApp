using System.Text.Json;
using ListingApp.Server.Models;

namespace ListingApp.Server.Services;

public sealed class ListingRepository
{
    private readonly IReadOnlyList<Listing> _listings;

    public ListingRepository(IWebHostEnvironment environment)
    {
        var filePath = Path.Combine(environment.ContentRootPath, "Data", "sample_listings.json");

        try
        {
            var json = File.ReadAllText(filePath);
            var listings = JsonSerializer.Deserialize<List<Listing>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("The listing data file contains no listing array.");

            ValidateListings(listings);
            _listings = listings.AsReadOnly();
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
        {
            throw new InvalidOperationException($"Unable to load listing data from '{filePath}': {exception.Message}", exception);
        }
    }

    public IReadOnlyList<Listing> GetAll() => _listings;

    private static void ValidateListings(IReadOnlyList<Listing> listings)
    {
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var listing in listings)
        {
            if (string.IsNullOrWhiteSpace(listing.Id) || string.IsNullOrWhiteSpace(listing.Source) ||
                string.IsNullOrWhiteSpace(listing.Address) || string.IsNullOrWhiteSpace(listing.City) ||
                string.IsNullOrWhiteSpace(listing.State) || string.IsNullOrWhiteSpace(listing.Zip) ||
                string.IsNullOrWhiteSpace(listing.Status) || listing.Description is null)
            {
                throw new InvalidDataException("Each listing must include its ID, source, address, city, state, ZIP, status, and description.");
            }

            if (!identities.Add($"{listing.Source}\0{listing.Id}"))
            {
                throw new InvalidDataException($"Duplicate listing identity '{listing.Source}/{listing.Id}'.");
            }

            if (listing.Price <= 0 || listing.Bedrooms < 0 || listing.Bathrooms < 0 || listing.Sqft < 0 ||
                listing.Latitude is < -90 or > 90 || listing.Longitude is < -180 or > 180 ||
                !double.IsFinite(listing.Latitude) || !double.IsFinite(listing.Longitude))
            {
                throw new InvalidDataException($"Listing '{listing.Source}/{listing.Id}' contains invalid numeric data.");
            }
        }
    }
}
