public class Wallet
{
    public int Money { get; private set; }

    public Wallet(int startMoney = 100) { Money = startMoney; }

    public bool TryBet(int amount)
    {
        if (amount <= 0 || amount > Money) return false;
        Money -= amount;
        return true;
    }

    public void Win(int amount) => Money += amount;
    public void Push(int amount) => Money += amount;
}