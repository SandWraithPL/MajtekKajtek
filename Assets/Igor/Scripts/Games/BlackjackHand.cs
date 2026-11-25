using System.Collections.Generic;

public class BlackjackHand
{
    public List<Card> cards = new List<Card>();
    public int bet;
    public bool stand;
    public bool bust;
    public bool doubled;
    public bool fromSplit;
    public bool splitAces;

    public bool IsBlackjack()
    {
        return cards.Count == 2 && !fromSplit && Calculate() == 21;
    }

    public bool IsOpen => !stand && !bust;

    public int Calculate()
    {
        return Score(out _);
    }

    public bool IsSoft
    {
        get
        {
            Score(out bool soft);
            return soft;
        }
    }

    private int Score(out bool soft)
    {
        int sum = 0;
        int aces = 0;
        foreach (var c in cards)
        {
            if (c.rank == Rank.Ace) { aces++; sum += 11; }
            else if (c.rank >= Rank.Jack) sum += 10;
            else sum += (int)c.rank;
        }
        while (sum > 21 && aces > 0) { sum -= 10; aces--; }
        soft = aces > 0;
        return sum;
    }
}
