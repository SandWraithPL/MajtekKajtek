using System.Collections.Generic;
using System.Linq;

public abstract class BlackjackPlayer
{
    public string name;
    public List<BlackjackHand> hands = new List<BlackjackHand>();
    public int insuranceBet;
    public bool insuranceDecided;

    public abstract bool WantsToHit(BlackjackGame game, BlackjackHand hand);

    public virtual bool WantsInsurance(BlackjackGame game) => false;

    public virtual bool WantsToDouble(BlackjackGame game, BlackjackHand hand) => false;

    public virtual bool WantsToSplit(BlackjackGame game, BlackjackHand hand) => false;

    public void Reset()
    {
        hands.Clear();
        insuranceBet = 0;
        insuranceDecided = false;
    }

    public bool AllHandsDone()
    {
        return hands.Count > 0 && hands.All(h => h.stand || h.bust);
    }
}

public class HumanPlayer : BlackjackPlayer
{
    public override bool WantsToHit(BlackjackGame game, BlackjackHand hand) => false;
}

// Uproszczona strategia podstawowa: ubezpieczenie prawie nigdy się nie opłaca,
// na 12–16 zostaje gdy krupier ma słabą kartę, dzieli asy i ósemki.
public class StatisticalPlayer : BlackjackPlayer
{
    public override bool WantsInsurance(BlackjackGame game) => false;

    public override bool WantsToHit(BlackjackGame game, BlackjackHand hand)
    {
        int total = hand.Calculate();
        if (total >= 21) return false;
        if (hand.IsSoft && total <= 17) return true;
        if (total >= 17) return false;
        if (total <= 11) return true;
        return game.DealerUpcardValue() >= 7;
    }

    public override bool WantsToDouble(BlackjackGame game, BlackjackHand hand)
    {
        int total = hand.Calculate();
        int up = game.DealerUpcardValue();
        if (total == 11) return true;
        if (total == 10) return up <= 9;
        if (total == 9) return up >= 3 && up <= 6;
        return false;
    }

    public override bool WantsToSplit(BlackjackGame game, BlackjackHand hand)
    {
        Rank rank = hand.cards[0].rank;
        int up = game.DealerUpcardValue();
        if (rank == Rank.Ace || rank == Rank.Eight) return true;
        if (rank == Rank.Five || rank >= Rank.Ten) return false;
        return up <= 6;
    }
}

// Celowo gra za ostro: dobiera do 18, bierze ubezpieczenie, dzieli każdą parę.
public class RiskyPlayer : BlackjackPlayer
{
    public override bool WantsInsurance(BlackjackGame game) => true;

    public override bool WantsToHit(BlackjackGame game, BlackjackHand hand)
    {
        return hand.Calculate() < 19;
    }

    public override bool WantsToDouble(BlackjackGame game, BlackjackHand hand)
    {
        int total = hand.Calculate();
        return total >= 9 && total <= 11;
    }

    public override bool WantsToSplit(BlackjackGame game, BlackjackHand hand) => true;
}
