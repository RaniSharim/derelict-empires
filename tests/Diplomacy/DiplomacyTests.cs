using Xunit;
using DerlictEmpires.Core.Diplomacy;

namespace DerlictEmpires.Tests.Diplomacy;

public class DiplomacyTests
{
    [Fact]
    public void FirstContact_RequiresExplicitEstablishment()
    {
        var mgr = new DiplomacyManager();
        Assert.False(mgr.HasContact(0, 1));

        mgr.EstablishContact(0, 1);
        Assert.True(mgr.HasContact(0, 1));
        Assert.True(mgr.HasContact(1, 0)); // Symmetric
    }

    [Fact]
    public void Agreement_CanBeProposedAndAccepted()
    {
        var mgr = new DiplomacyManager();
        var agreement = mgr.ProposeAgreement(AgreementType.NonAggression, 0, 1, currentTick: 10);

        Assert.Equal(AgreementStatus.Proposed, agreement.Status);
        Assert.True(mgr.AcceptAgreement(agreement.Id));
        Assert.Equal(AgreementStatus.Active, agreement.Status);
    }

    [Fact]
    public void BreakAgreement_CausesReputationDamage()
    {
        var mgr = new DiplomacyManager();
        var agreement = mgr.ProposeAgreement(AgreementType.Alliance, 0, 1, currentTick: 0);
        mgr.AcceptAgreement(agreement.Id);

        float relBefore = mgr.Reputation.GetRelation(0, 1);
        float repBefore = mgr.Reputation.GetReliability(0);

        mgr.BreakAgreement(agreement.Id, 0);

        Assert.True(mgr.Reputation.GetRelation(0, 1) < relBefore);
        Assert.True(mgr.Reputation.GetReliability(0) < repBefore);
    }

    [Fact]
    public void Alliance_BreachPenalty_GreaterThan_Trade()
    {
        var mgr1 = new DiplomacyManager();
        var alliance = mgr1.ProposeAgreement(AgreementType.Alliance, 0, 1, 0);
        mgr1.AcceptAgreement(alliance.Id);
        mgr1.BreakAgreement(alliance.Id, 0);
        float alliancePenalty = 50f - mgr1.Reputation.GetReliability(0);

        var mgr2 = new DiplomacyManager();
        var trade = mgr2.ProposeAgreement(AgreementType.Trade, 0, 1, 0);
        mgr2.AcceptAgreement(trade.Id);
        mgr2.BreakAgreement(trade.Id, 0);
        float tradePenalty = 50f - mgr2.Reputation.GetReliability(0);

        Assert.True(alliancePenalty > tradePenalty);
    }

    [Fact]
    public void ReputationDecay_MovesTowardNeutral()
    {
        var mgr = new DiplomacyManager();
        mgr.Reputation.ModifyRelation(0, 1, 50f);

        float before = mgr.Reputation.GetRelation(0, 1);
        mgr.Reputation.DecayTowardsNeutral(5f);
        float after = mgr.Reputation.GetRelation(0, 1);

        Assert.True(after < before);
        Assert.True(after > 0); // Still positive, just closer to 0
    }
}
