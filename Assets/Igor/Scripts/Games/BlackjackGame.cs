using System.Collections.Generic;

public enum GamePhase
{
    Idle,
    Dealing,
    Insurance,
    PlayerTurns,
    DealerTurn,
    Finished
}

public class BlackjackGame
{
    public List<BlackjackPlayer> players = new List<BlackjackPlayer>();
    public List<Card> dealerHand = new List<Card>();

    public bool dealerBust;
    public bool dealerHasBlackjack;
    public bool roundFinished;
    public GamePhase phase = GamePhase.Idle;

    public float actionDelay = 0.5f;

    private Deck deck = new Deck();
    private readonly Queue<System.Action> dealQueue = new Queue<System.Action>();
    private float actionTimer;
    private int currentPlayerIndex;
    private bool dealerHoleRevealed;

    public bool CanStartRound => phase == GamePhase.Idle || phase == GamePhase.Finished;

    public bool IsDealerOpen => dealerHoleRevealed || phase == GamePhase.Finished;

    public int DealerTotal => CalculateHand(dealerHand);

    public BlackjackPlayer ActivePlayer
    {
        get
        {
            if (phase != GamePhase.PlayerTurns || currentPlayerIndex >= players.Count)
                return null;
            return players[currentPlayerIndex];
        }
    }

    public BlackjackHand ActiveHand
    {
        get
        {
            var player = ActivePlayer;
            if (player == null) return null;
            return FirstOpenHand(player);
        }
    }

    public void StartNewRound()
    {
        deck = new Deck();
        dealerHand.Clear();
        dealerBust = false;
        dealerHasBlackjack = false;
        dealerHoleRevealed = false;
        roundFinished = false;
        dealQueue.Clear();
        actionTimer = 0f;
        currentPlayerIndex = 0;
        phase = GamePhase.Dealing;

        foreach (var player in players)
        {
            player.Reset();
            player.hands.Add(new BlackjackHand());
        }

        for (int round = 0; round < 2; round++)
        {
            foreach (var player in players)
            {
                var seated = player;
                dealQueue.Enqueue(() => DealToPlayer(seated));
            }
            dealQueue.Enqueue(DealToDealer);
        }

        dealQueue.Enqueue(AfterDealing);
    }

    public void PlaceBet(BlackjackHand hand, int amount)
    {
        hand.bet = amount;
    }

    public void Tick(float deltaTime)
    {
        if (phase == GamePhase.Dealing)
            StepQueue(deltaTime);
        else if (phase == GamePhase.Insurance)
            StepInsurance(deltaTime);
        else if (phase == GamePhase.PlayerTurns)
            StepPlayers(deltaTime);
        else if (phase == GamePhase.DealerTurn)
            StepDealer(deltaTime);
    }

    public string PhaseText()
    {
        switch (phase)
        {
            case GamePhase.Idle:
                return "Wybierz zakład i rozdaj karty.";
            case GamePhase.Dealing:
                return "Rozdawanie kart";
            case GamePhase.Insurance:
                return "Ubezpieczenie — decyzja: " + PendingInsuranceName();
            case GamePhase.PlayerTurns:
                return ActivePlayer == null ? "Tury graczy" : "Tura: " + ActivePlayer.name;
            case GamePhase.DealerTurn:
                return "Tura krupiera";
            case GamePhase.Finished:
                return "Koniec rundy";
            default:
                return "";
        }
    }

    public bool WaitingForInsurance(BlackjackPlayer player)
    {
        if (phase != GamePhase.Insurance || player.insuranceDecided)
            return false;

        foreach (var other in players)
        {
            if (other == player) return true;
            if (!other.insuranceDecided) return false;
        }
        return false;
    }

    public bool TakeInsurance(BlackjackPlayer player, bool take)
    {
        if (!WaitingForInsurance(player)) return false;

        player.insuranceDecided = true;
        if (take && player.hands.Count > 0)
            player.insuranceBet = player.hands[0].bet / 2;

        if (player is HumanPlayer)
            actionTimer = 0f;
        return true;
    }

    public void Hit(BlackjackPlayer player, BlackjackHand hand)
    {
        if (!CanAct(player, hand)) return;

        Card card = deck.Draw();
        hand.cards.Add(card);
        Resolve(hand);
        AfterPlayerAction(player);
    }

    public void Stand(BlackjackPlayer player, BlackjackHand hand)
    {
        if (!CanAct(player, hand)) return;

        hand.stand = true;
        AfterPlayerAction(player);
    }

    public bool DoubleDown(BlackjackPlayer player, BlackjackHand hand)
    {
        if (!CanAct(player, hand) || hand.cards.Count != 2 || hand.doubled)
            return false;

        hand.doubled = true;
        hand.bet *= 2;
        Card card = deck.Draw();
        hand.cards.Add(card);
        Resolve(hand);
        hand.stand = true;
        AfterPlayerAction(player);
        return true;
    }

    public bool CanSplit(BlackjackPlayer player, BlackjackHand hand)
    {
        return CanAct(player, hand)
            && hand.cards.Count == 2
            && hand.cards[0].rank == hand.cards[1].rank
            && player.hands.Count < 4;
    }

    public bool Split(BlackjackPlayer player, BlackjackHand hand)
    {
        if (!CanSplit(player, hand)) return false;

        bool aces = hand.cards[0].rank == Rank.Ace;
        var other = new BlackjackHand
        {
            bet = hand.bet,
            fromSplit = true,
            splitAces = aces
        };

        hand.fromSplit = true;
        hand.splitAces = aces;
        other.cards.Add(hand.cards[1]);
        hand.cards.RemoveAt(1);
        hand.cards.Add(deck.Draw());
        other.cards.Add(deck.Draw());
        player.hands.Add(other);

        if (aces)
        {
            hand.stand = true;
            other.stand = true;
        }
        else
        {
            Resolve(hand);
            Resolve(other);
        }

        AfterPlayerAction(player);
        return true;
    }

    public int CalculateHand(List<Card> hand)
    {
        int sum = 0;
        int aces = 0;
        foreach (var card in hand)
        {
            if (card.rank == Rank.Ace) { aces++; sum += 11; }
            else if (card.rank >= Rank.Jack) sum += 10;
            else sum += (int)card.rank;
        }
        while (sum > 21 && aces > 0) { sum -= 10; aces--; }
        return sum;
    }

    public int DealerUpcardValue()
    {
        if (dealerHand.Count == 0) return 0;
        Rank rank = dealerHand[0].rank;
        if (rank == Rank.Ace) return 11;
        if (rank >= Rank.Jack) return 10;
        return (int)rank;
    }

    public bool IsBlackjack(List<Card> hand)
    {
        return hand.Count == 2 && CalculateHand(hand) == 21;
    }

    public float GetHandPayout(BlackjackHand hand)
    {
        if (hand.bust) return 0f;
        if (hand.IsBlackjack())
            return dealerHasBlackjack ? 1f : 2.5f;
        if (dealerHasBlackjack) return 0f;
        if (dealerBust) return 2f;

        int playerTotal = hand.Calculate();
        int dealerTotal = DealerTotal;
        if (playerTotal > dealerTotal) return 2f;
        if (playerTotal < dealerTotal) return 0f;
        return 1f;
    }

    public int GetInsurancePayout(BlackjackPlayer player)
    {
        return dealerHasBlackjack ? player.insuranceBet * 3 : 0;
    }

    public string GetHandResult(BlackjackHand hand)
    {
        if (hand.bust) return "Przegrana";
        if (hand.IsBlackjack())
            return dealerHasBlackjack ? "Remis" : "Blackjack 3:2";
        if (dealerHasBlackjack) return "Przegrana";
        if (dealerBust) return "Wygrana";

        int playerTotal = hand.Calculate();
        int dealerTotal = DealerTotal;
        if (playerTotal > dealerTotal) return "Wygrana";
        if (playerTotal < dealerTotal) return "Przegrana";
        return "Remis";
    }

    public string GetDealerDisplay()
    {
        if (dealerHand.Count == 0) return "—";
        if (dealerHand.Count == 1) return dealerHand[0].ToString();
        if (!IsDealerOpen) return dealerHand[0] + "  ??";
        return string.Join("  ", dealerHand) + $"  ({DealerTotal})";
    }

    public bool DealerShowsAce()
    {
        return dealerHand.Count > 0 && dealerHand[0].rank == Rank.Ace;
    }

    private void StepQueue(float deltaTime)
    {
        actionTimer += deltaTime;
        if (actionTimer < actionDelay || dealQueue.Count == 0) return;
        actionTimer = 0f;
        dealQueue.Dequeue().Invoke();
    }

    private void StepInsurance(float deltaTime)
    {
        actionTimer += deltaTime;
        if (actionTimer < actionDelay) return;

        foreach (var player in players)
        {
            if (player.insuranceDecided) continue;
            if (player is HumanPlayer)
            {
                actionTimer = 0f;
                return;
            }

            actionTimer = 0f;
            TakeInsurance(player, player.WantsInsurance(this));
            if (AllInsuranceDecided())
                FinishInsurance();
            return;
        }

        actionTimer = 0f;
        FinishInsurance();
    }

    private void StepPlayers(float deltaTime)
    {
        actionTimer += deltaTime;
        if (actionTimer < actionDelay) return;
        actionTimer = 0f;
        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        if (roundFinished || phase != GamePhase.PlayerTurns) return;

        SkipFinishedPlayers();
        if (currentPlayerIndex >= players.Count)
        {
            StartDealerTurn();
            return;
        }

        var player = players[currentPlayerIndex];
        var hand = FirstOpenHand(player);
        if (hand == null)
        {
            currentPlayerIndex++;
            actionTimer = 0f;
            return;
        }

        if (player is HumanPlayer)
        {
            actionTimer = 0f;
            return;
        }

        if (hand.cards.Count == 2 && CanSplit(player, hand) && player.WantsToSplit(this, hand))
            Split(player, hand);
        else if (hand.cards.Count == 2 && !hand.doubled && player.WantsToDouble(this, hand))
            DoubleDown(player, hand);
        else if (player.WantsToHit(this, hand))
            Hit(player, hand);
        else
            Stand(player, hand);

        actionTimer = 0f;
    }

    private void StepDealer(float deltaTime)
    {
        actionTimer += deltaTime;
        if (actionTimer < actionDelay) return;
        actionTimer = 0f;
        DealerStep();
    }

    private void DealerStep()
    {
        if (roundFinished) return;

        if (!dealerHoleRevealed)
        {
            dealerHoleRevealed = true;
            int total = DealerTotal;

            if (dealerHasBlackjack || AllPlayerHandsBusted() || total >= 17)
            {
                FinishRound();
            }
            return;
        }

        if (dealerBust)
        {
            FinishRound();
            return;
        }

        int sum = DealerTotal;
        if (sum < 17)
        {
            Card card = deck.Draw();
            dealerHand.Add(card);
            if (DealerTotal > 21)
                dealerBust = true;
            return;
        }

        FinishRound();
    }

    private void DealToPlayer(BlackjackPlayer player)
    {
        if (player.hands.Count == 0) return;
        Card card = deck.Draw();
        player.hands[0].cards.Add(card);
    }

    private void DealToDealer()
    {
        Card card = deck.Draw();
        dealerHand.Add(card);
    }

    private void AfterDealing()
    {
        dealerHasBlackjack = IsBlackjack(dealerHand);
        CloseFinishedHands();

        if (DealerShowsAce())
        {
            phase = GamePhase.Insurance;
            actionTimer = 0f;
            return;
        }

        if (dealerHasBlackjack)
            StartDealerTurn();
        else
            BeginPlayerTurns();
    }

    private void FinishInsurance()
    {
        if (phase != GamePhase.Insurance) return;
        if (dealerHasBlackjack)
            StartDealerTurn();
        else
            BeginPlayerTurns();
    }

    private void BeginPlayerTurns()
    {
        phase = GamePhase.PlayerTurns;
        currentPlayerIndex = 0;
        actionTimer = 0f;
        CloseFinishedHands();
        SkipFinishedPlayers();
        if (currentPlayerIndex >= players.Count)
            StartDealerTurn();
    }

    private void StartDealerTurn()
    {
        if (phase == GamePhase.DealerTurn || phase == GamePhase.Finished) return;
        phase = GamePhase.DealerTurn;
        dealerHoleRevealed = false;
        actionTimer = 0f;
    }

    private void FinishRound()
    {
        if (roundFinished) return;

        dealerHoleRevealed = true;
        roundFinished = true;
        phase = GamePhase.Finished;
    }

    private void AfterPlayerAction(BlackjackPlayer player)
    {
        if (player is HumanPlayer)
            actionTimer = 0f;
        if (phase != GamePhase.PlayerTurns) return;

        SkipFinishedPlayers();
        if (currentPlayerIndex >= players.Count)
            StartDealerTurn();
    }

    private void SkipFinishedPlayers()
    {
        while (currentPlayerIndex < players.Count && players[currentPlayerIndex].AllHandsDone())
            currentPlayerIndex++;
    }

    private void CloseFinishedHands()
    {
        foreach (var player in players)
        {
            foreach (var hand in player.hands)
            {
                if (!hand.IsOpen) continue;

                int total = hand.Calculate();
                if (total > 21)
                    hand.bust = true;
                else if (total == 21)
                    hand.stand = true;
            }
        }
    }

    private void Resolve(BlackjackHand hand)
    {
        int total = hand.Calculate();
        if (total > 21) hand.bust = true;
        else if (total == 21) hand.stand = true;
    }

    private bool CanAct(BlackjackPlayer player, BlackjackHand hand)
    {
        if (roundFinished || phase != GamePhase.PlayerTurns) return false;
        if (hand == null || !hand.IsOpen) return false;
        if (currentPlayerIndex >= players.Count || players[currentPlayerIndex] != player) return false;
        return FirstOpenHand(player) == hand;
    }

    private BlackjackHand FirstOpenHand(BlackjackPlayer player)
    {
        foreach (var hand in player.hands)
            if (hand.IsOpen) return hand;
        return null;
    }

    private bool AllInsuranceDecided()
    {
        foreach (var player in players)
            if (!player.insuranceDecided) return false;
        return players.Count > 0;
    }

    private bool AllPlayerHandsBusted()
    {
        if (players.Count == 0) return false;
        foreach (var player in players)
            foreach (var hand in player.hands)
                if (!hand.bust) return false;
        return true;
    }

    private string PendingInsuranceName()
    {
        foreach (var player in players)
            if (!player.insuranceDecided) return player.name;
        return "—";
    }
}