using UnityEngine;
using UnityEngine.InputSystem;

public class BlackjackTester : MonoBehaviour
{
    [SerializeField] private float actionDelay = 0.5f;

    private readonly BlackjackGame game = new BlackjackGame();
    private readonly Wallet wallet = new Wallet(1000);
    private readonly HumanPlayer human = new HumanPlayer { name = "Kajtek" };
    private readonly StatisticalPlayer careful = new StatisticalPlayer { name = "Statystyk" };
    private readonly RiskyPlayer risky = new RiskyPlayer { name = "Ryzykant" };

    private int pendingBet = 10;
    private bool settled;
    private string walletLine = "";
    private Vector2 scroll;

    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle winStyle;
    private GUIStyle loseStyle;
    private GUIStyle pushStyle;
    private GUIStyle buttonStyle;

    void Awake()
    {
        game.actionDelay = actionDelay;
        game.players.Add(human);
        game.players.Add(careful);
        game.players.Add(risky);
    }

    void Update()
    {
        game.Tick(Time.deltaTime);

        if (game.phase == GamePhase.Finished && !settled)
        {
            settled = true;
            Settle();
        }

        ReadKeys();
    }

    void OnGUI()
    {
        EnsureStyles();

        // Jeden ScrollView na cały ekran – wszystko idzie od góry do dołu.
        scroll = GUILayout.BeginScrollView(scroll);

        // --- 1. Pasek statusu ---
        GUILayout.Label($"Portfel: {wallet.Money}$    Zakład: {pendingBet}$", headerStyle);
        GUILayout.Label("Status: " + game.PhaseText(), labelStyle);

        // --- 2. Sterowanie zakładem i start ---
        if (game.CanStartRound)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", buttonStyle, GUILayout.Width(60f), GUILayout.Height(32f)))
                pendingBet = Mathf.Max(10, pendingBet - 10);
            if (GUILayout.Button("+10", buttonStyle, GUILayout.Width(60f), GUILayout.Height(32f)))
                pendingBet += 10;

            GUI.enabled = wallet.Money >= pendingBet && pendingBet > 0;
            string caption = game.phase == GamePhase.Finished ? "Nowa runda  [Spacja]" : "Rozdaj  [Spacja]";
            if (GUILayout.Button(caption, buttonStyle, GUILayout.Height(32f)))
                TryStartRound();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10f);

        // --- 3. Krupier ---
        GUILayout.Label("KRUPIER", headerStyle);
        GUILayout.Label(game.GetDealerDisplay(), labelStyle);

        GUILayout.Space(10f);

        // --- 4. Gracze (każdy blok osobno, od góry do dołu) ---
        foreach (var player in game.players)
            DrawPlayerBlock(player);

        GUILayout.Space(10f);

        // --- 5. Ubezpieczenie ---
        if (game.WaitingForInsurance(human))
        {
            int cost = human.hands.Count > 0 ? human.hands[0].bet / 2 : 0;
            GUILayout.Label($"Krupier ma asa. Ubezpieczenie: {cost}$", headerStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = wallet.Money >= cost;
            if (GUILayout.Button("Biorę [A]", buttonStyle, GUILayout.Height(32f)))
                TryInsurance(true, cost);
            GUI.enabled = true;
            if (GUILayout.Button("Nie [S]", buttonStyle, GUILayout.Height(32f)))
                game.TakeInsurance(human, false);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        // --- 6. Wyniki na końcu ---
        if (game.phase == GamePhase.Finished)
            DrawResults();

        GUILayout.EndScrollView();
    }

    private void DrawPlayerBlock(BlackjackPlayer player)
    {
        bool isActive = game.ActivePlayer == player;
        string role = player is HumanPlayer
            ? "Ty"
            : player is StatisticalPlayer ? "NPC ostrożny" : "NPC ryzykowny";

        string title = isActive ? "► " + player.name : player.name;
        GUILayout.Label($"{title}  ({role})", headerStyle);

        for (int i = 0; i < player.hands.Count; i++)
        {
            var hand = player.hands[i];
            string prefix = player.hands.Count > 1 ? $"[{i + 1}] " : "";
            GUILayout.Label(prefix + HandLine(hand), labelStyle);
        }

        // Przyciski akcji – tylko gdy to Twoja tura i jest otwarta ręka
        var activeHand = game.ActiveHand;
        if (isActive && player is HumanPlayer && activeHand != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dobierz [A]", buttonStyle, GUILayout.Height(32f)))
                game.Hit(human, activeHand);
            if (GUILayout.Button("Pasuj [S]", buttonStyle, GUILayout.Height(32f)))
                game.Stand(human, activeHand);
            if (activeHand.cards.Count == 2 && !activeHand.doubled && wallet.Money >= activeHand.bet)
            {
                if (GUILayout.Button("Podwój [D]", buttonStyle, GUILayout.Height(32f)))
                    TryDouble(activeHand);
            }
            if (game.CanSplit(human, activeHand) && wallet.Money >= activeHand.bet)
            {
                if (GUILayout.Button("Split [F]", buttonStyle, GUILayout.Height(32f)))
                    TrySplit(activeHand);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8f);
    }

    private void DrawResults()
    {
        GUILayout.Label("WYNIKI", headerStyle);
        GUILayout.Label($"Krupier: {game.GetDealerDisplay()}", labelStyle);

        foreach (var player in game.players)
        {
            for (int i = 0; i < player.hands.Count; i++)
            {
                var hand = player.hands[i];
                string name = player.hands.Count > 1 ? $"{player.name} [{i + 1}]" : player.name;
                string result = game.GetHandResult(hand);
                int net = Returned(hand) - hand.bet;
                string money = player is HumanPlayer
                    ? (net > 0 ? $"+{net}$" : net < 0 ? $"{net}$" : "0$")
                    : "—";
                GUILayout.Label($"{name}: {result}  ({money})", StyleFor(result));
            }
        }

        if (!string.IsNullOrEmpty(walletLine))
            GUILayout.Label(walletLine, headerStyle);
    }

    private string HandLine(BlackjackHand hand)
    {
        if (hand.cards.Count == 0) return "—";
        string cards = string.Join(" ", hand.cards);
        int total = hand.Calculate();

        string state = "";
        if (hand.bust) state = "  bust";
        else if (hand.IsBlackjack()) state = "  blackjack";
        else if (hand.stand) state = "  pas";

        return $"{cards}  ({total}){state}";
    }

    private void TryStartRound()
    {
        if (!game.CanStartRound || pendingBet <= 0 || !wallet.TryBet(pendingBet))
            return;

        int stake = pendingBet;
        settled = false;
        walletLine = "";
        game.StartNewRound();
        game.PlaceBet(human.hands[0], stake);
        game.PlaceBet(careful.hands[0], stake);
        game.PlaceBet(risky.hands[0], stake);
    }

    private void TryDouble(BlackjackHand hand)
    {
        int extra = hand.bet;
        if (wallet.Money < extra) return;
        if (game.DoubleDown(human, hand))
            wallet.TryBet(extra);
    }

    private void TrySplit(BlackjackHand hand)
    {
        int extra = hand.bet;
        if (wallet.Money < extra) return;
        if (game.Split(human, hand))
            wallet.TryBet(extra);
    }

    private void TryInsurance(bool take, int cost)
    {
        if (take && wallet.Money < cost) return;
        if (game.TakeInsurance(human, take) && take && cost > 0)
            wallet.TryBet(cost);
    }

    private void Settle()
    {
        int before = wallet.Money;
        if (human.insuranceBet > 0)
            wallet.Win(game.GetInsurancePayout(human));

        foreach (var hand in human.hands)
            wallet.Win(Returned(hand));

        int delta = wallet.Money - before;
        string sign = delta > 0 ? $"+{delta}" : delta.ToString();
        walletLine = $"Rozliczenie: {sign}$  (teraz {wallet.Money}$)";
    }

    private int Returned(BlackjackHand hand)
    {
        return (int)(hand.bet * game.GetHandPayout(hand));
    }

    private void ReadKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame && game.CanStartRound)
        {
            TryStartRound();
            return;
        }

        if (game.WaitingForInsurance(human))
        {
            int cost = human.hands.Count > 0 ? human.hands[0].bet / 2 : 0;
            if (keyboard.aKey.wasPressedThisFrame) TryInsurance(true, cost);
            if (keyboard.sKey.wasPressedThisFrame) game.TakeInsurance(human, false);
            return;
        }

        var hand = game.ActiveHand;
        if (!(game.ActivePlayer is HumanPlayer) || hand == null) return;

        if (keyboard.aKey.wasPressedThisFrame) game.Hit(human, hand);
        if (keyboard.sKey.wasPressedThisFrame) game.Stand(human, hand);
        if (keyboard.dKey.wasPressedThisFrame) TryDouble(hand);
        if (keyboard.fKey.wasPressedThisFrame) TrySplit(hand);
    }

    private GUIStyle StyleFor(string result)
    {
        if (result.StartsWith("Wygrana") || result.StartsWith("Blackjack")) return winStyle;
        if (result.StartsWith("Przegrana")) return loseStyle;
        return pushStyle;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = false };
        labelStyle.normal.textColor = Color.white;

        headerStyle = new GUIStyle(labelStyle) { fontSize = 18, fontStyle = FontStyle.Bold };

        winStyle = new GUIStyle(labelStyle);
        winStyle.normal.textColor = new Color(0.55f, 1f, 0.55f);

        loseStyle = new GUIStyle(labelStyle);
        loseStyle.normal.textColor = new Color(1f, 0.55f, 0.5f);

        pushStyle = new GUIStyle(labelStyle);
        pushStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
    }
}