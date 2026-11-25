public enum Suit { Spades, Hearts, Diamonds, Clubs }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

public class Card
{
    public Suit suit;
    public Rank rank;

    public Card(Suit s, Rank r) { suit = s; rank = r; }

    public string SuitSymbol()
    {
        switch (suit)
        {
            case Suit.Spades: return "♠";
            case Suit.Hearts: return "♥";
            case Suit.Diamonds: return "♦";
            case Suit.Clubs: return "♣";
            default: return "?";
        }
    }

    public string RankSymbol()
    {
        switch (rank)
        {
            case Rank.Two: return "2";
            case Rank.Three: return "3";
            case Rank.Four: return "4";
            case Rank.Five: return "5";
            case Rank.Six: return "6";
            case Rank.Seven: return "7";
            case Rank.Eight: return "8";
            case Rank.Nine: return "9";
            case Rank.Ten: return "10";
            case Rank.Jack: return "J";
            case Rank.Queen: return "Q";
            case Rank.King: return "K";
            case Rank.Ace: return "A";
            default: return "?";
        }
    }

    public override string ToString() => SuitSymbol() + RankSymbol();
}