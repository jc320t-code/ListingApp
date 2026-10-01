namespace ListingApp.Server.Models;

public sealed record Listing
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string Zip { get; init; }
    public decimal Price { get; init; }
    public int Bedrooms { get; init; }
    public decimal Bathrooms { get; init; }
    public int Sqft { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateOnly ListedDate { get; init; }
    public required string Status { get; init; }
    public required string Description { get; init; }
}
