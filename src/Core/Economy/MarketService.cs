using System;
using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.Economy;

public class MarketListing
{
    public int Id { get; set; }
    public int SellerEmpireId { get; set; }
    public string ItemType { get; set; } = ""; // "resource", "component", "ship"
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
    public long Price { get; set; }
    public bool IsAuction { get; set; }
    public long HighestBid { get; set; }
    public int HighestBidderId { get; set; } = -1;
    public int ExpirationTick { get; set; }
    public bool IsSold { get; set; }
}

/// <summary>Central market for trading resources, components, and ships. Pure C#.</summary>
public class MarketService
{
    private readonly List<MarketListing> _listings = new();
    private readonly CurrencyManager _currency;
    private int _nextListingId;

    public event Action<MarketListing>? ListingCreated;
    public event Action<MarketListing, int>? SaleCompleted; // listing, buyerEmpireId

    public IReadOnlyList<MarketListing> ActiveListings =>
        _listings.Where(l => !l.IsSold).ToList();

    public MarketService(CurrencyManager currency)
    {
        _currency = currency;
    }

    public MarketListing CreateListing(int sellerEmpireId, string itemType, string itemId,
        int quantity, long price, bool isAuction = false, int expirationTick = -1)
    {
        var listing = new MarketListing
        {
            Id = _nextListingId++,
            SellerEmpireId = sellerEmpireId,
            ItemType = itemType,
            ItemId = itemId,
            Quantity = quantity,
            Price = price,
            IsAuction = isAuction,
            ExpirationTick = expirationTick
        };
        _listings.Add(listing);
        ListingCreated?.Invoke(listing);
        return listing;
    }

    public bool BuyListing(int buyerEmpireId, int listingId)
    {
        var listing = _listings.FirstOrDefault(l => l.Id == listingId && !l.IsSold);
        if (listing == null) return false;
        if (listing.SellerEmpireId == buyerEmpireId) return false;
        if (listing.IsAuction) return false; // Must use PlaceBid

        if (!_currency.SpendCredits(buyerEmpireId, listing.Price)) return false;

        _currency.AddCredits(listing.SellerEmpireId, listing.Price);
        listing.IsSold = true;
        SaleCompleted?.Invoke(listing, buyerEmpireId);
        return true;
    }

    public bool PlaceBid(int bidderEmpireId, int listingId, long bidAmount)
    {
        var listing = _listings.FirstOrDefault(l => l.Id == listingId && !l.IsSold && l.IsAuction);
        if (listing == null) return false;
        if (bidAmount <= listing.HighestBid) return false;
        if (bidAmount < listing.Price) return false;

        listing.HighestBid = bidAmount;
        listing.HighestBidderId = bidderEmpireId;
        return true;
    }

    /// <summary>Expire auctions and complete sales. Call periodically.</summary>
    public void ProcessExpiredListings(int currentTick)
    {
        foreach (var listing in _listings.Where(l => !l.IsSold && l.ExpirationTick > 0 && l.ExpirationTick <= currentTick))
        {
            if (listing.IsAuction && listing.HighestBidderId >= 0)
            {
                if (_currency.SpendCredits(listing.HighestBidderId, listing.HighestBid))
                {
                    _currency.AddCredits(listing.SellerEmpireId, listing.HighestBid);
                    listing.IsSold = true;
                    SaleCompleted?.Invoke(listing, listing.HighestBidderId);
                }
            }
            else
            {
                listing.IsSold = true; // Expired unsold
            }
        }
    }
}
