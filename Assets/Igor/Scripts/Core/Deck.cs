using System.Collections.Generic;

public class Deck
{
    private List<Card> cards = new List<Card>();
    private System.Random rng = new System.Random();

    public Deck()
    {
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
                cards.Add(new Card(s, r));
        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public Card Draw()
    {
        if (cards.Count == 0)
        {
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
                foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
                    cards.Add(new Card(s, r));
            Shuffle();
        }

        var c = cards[0];
        cards.RemoveAt(0);
        return c;
    }
}