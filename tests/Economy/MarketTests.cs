using Xunit;
using DerlictEmpires.Core.Economy;

namespace DerlictEmpires.Tests.Economy;

public class CurrencyManagerTests
{
    [Fact]
    public void AddCredits_IncreasesBalance()
    {
        var mgr = new CurrencyManager();
        mgr.SetBalance(0, 100);
        mgr.AddCredits(0, 50);
        Assert.Equal(150, mgr.GetBalance(0));
    }

    [Fact]
    public void SpendCredits_DecreasesBalance()
    {
        var mgr = new CurrencyManager();
        mgr.SetBalance(0, 100);
        Assert.True(mgr.SpendCredits(0, 40));
        Assert.Equal(60, mgr.GetBalance(0));
    }

    [Fact]
    public void SpendCredits_FailsWhenInsufficient()
    {
        var mgr = new CurrencyManager();
        mgr.SetBalance(0, 30);
        Assert.False(mgr.SpendCredits(0, 50));
        Assert.Equal(30, mgr.GetBalance(0));
    }
}

public class MarketServiceTests
{
    [Fact]
    public void CreateListing_Visible()
    {
        var currency = new CurrencyManager();
        var market = new MarketService(currency);
        var listing = market.CreateListing(0, "resource", "red_simple_energy", 10, 50);

        Assert.Single(market.ActiveListings);
        Assert.Equal(50, listing.Price);
    }

    [Fact]
    public void BuyListing_TransfersCredits()
    {
        var currency = new CurrencyManager();
        currency.SetBalance(0, 1000); // seller
        currency.SetBalance(1, 500);  // buyer

        var market = new MarketService(currency);
        var listing = market.CreateListing(0, "resource", "item", 1, 100);

        Assert.True(market.BuyListing(1, listing.Id));
        Assert.Equal(400, currency.GetBalance(1));   // buyer paid 100
        Assert.Equal(1100, currency.GetBalance(0));  // seller received 100
        Assert.True(listing.IsSold);
    }

    [Fact]
    public void BuyListing_FailsWhenCantAfford()
    {
        var currency = new CurrencyManager();
        currency.SetBalance(1, 10);

        var market = new MarketService(currency);
        var listing = market.CreateListing(0, "resource", "item", 1, 100);

        Assert.False(market.BuyListing(1, listing.Id));
        Assert.False(listing.IsSold);
    }

    [Fact]
    public void Auction_HighestBidWins()
    {
        var currency = new CurrencyManager();
        currency.SetBalance(1, 1000);
        currency.SetBalance(2, 1000);

        var market = new MarketService(currency);
        var listing = market.CreateListing(0, "resource", "item", 1, 50, isAuction: true, expirationTick: 10);

        Assert.True(market.PlaceBid(1, listing.Id, 60));
        Assert.True(market.PlaceBid(2, listing.Id, 80));
        Assert.False(market.PlaceBid(1, listing.Id, 70)); // Lower than current

        Assert.Equal(2, listing.HighestBidderId);
        Assert.Equal(80, listing.HighestBid);
    }

    [Fact]
    public void ExpiredAuction_CompletesToHighestBidder()
    {
        var currency = new CurrencyManager();
        currency.SetBalance(0, 0);
        currency.SetBalance(1, 1000);

        var market = new MarketService(currency);
        var listing = market.CreateListing(0, "resource", "item", 1, 50, isAuction: true, expirationTick: 5);
        market.PlaceBid(1, listing.Id, 75);

        bool sold = false;
        market.SaleCompleted += (_, _) => sold = true;

        market.ProcessExpiredListings(5);

        Assert.True(sold);
        Assert.True(listing.IsSold);
        Assert.Equal(925, currency.GetBalance(1)); // Paid 75
        Assert.Equal(75, currency.GetBalance(0));  // Received 75
    }
}
