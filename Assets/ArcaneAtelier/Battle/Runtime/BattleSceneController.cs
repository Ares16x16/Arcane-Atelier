using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        private const int MaxRecentEvents = 8;

        [SerializeField] private BattleContentDatabase contentDatabase;
        [SerializeField] private BattleVisualManager visualManager;
        [SerializeField] private BattleHudPresenter hudPresenter;
        [SerializeField] private string startingBossId = "enemy.ash.imp";
        [SerializeField] private int playerMaxHealth = 100;

        private readonly List<string> recentEvents = new List<string>();
        public BattleUnit Player { get; private set; }
        public BattleUnit Boss { get; private set; }
        public BattleSimulation Simulation { get; private set; }
        public BattleBossDefinition CurrentBossDefinition { get; private set; }
        public BattleResult CurrentResult { get; private set; }
        public IReadOnlyList<string> RecentEvents => recentEvents;
        public BattleVisualManager VisualManager => visualManager;
        public bool IsPlayerInputAllowed =>
            Simulation != null &&
            Simulation.State == BattleState.WaitingForPlayer &&
            !inputBlocked;
        public string BossIntentDescription
        {
            get
            {
                if (Simulation == null || Simulation.BossAI == null || Simulation.State == BattleState.BattleEnded)
                {
                    return "No action.";
                }

                BattleBossAction nextAction = Simulation.BossAI.PeekNextAction();
                return string.IsNullOrWhiteSpace(nextAction.Description)
                    ? "No action."
                    : nextAction.Description;
            }
        }

        private List<WorkshopBattleCardEntry> currentDeck = new List<WorkshopBattleCardEntry>();
        private bool inputBlocked;

        private void Awake()
        {
            InitializePlayer();
            LoadWorkshopPayload();
            InitializeBoss();
        }

        private void Start()
        {
            if (CurrentBossDefinition == null)
            {
                Debug.LogError("BattleSceneController: cannot start battle without a valid boss definition.");
                enabled = false;
                return;
            }

            StartBattle();
        }

        private void Update()
        {
            if (Simulation == null || Simulation.State == BattleState.BattleEnded || inputBlocked)
            {
                return;
            }

            HandlePlayerInput();
        }

        private void OnDestroy()
        {
            if (Simulation != null)
            {
                Simulation.PlayerActionResolved -= OnPlayerActionResolved;
                Simulation.BossActionResolved -= OnBossActionResolved;
                Simulation.PlayerTurnSkipped -= OnPlayerTurnSkipped;
                Simulation.TurnCycleComplete -= OnTurnCycleComplete;
                Simulation.BattleEnded -= OnBattleEnded;
            }
        }

        private void InitializePlayer()
        {
            Player = new BattleUnit
            {
                DisplayName = "Player",
                MaxHealth = playerMaxHealth,
                CurrentHealth = playerMaxHealth,
                Shield = 0,
                Element = WorkshopElementAttribute.None
            };
        }

        private void LoadWorkshopPayload()
        {
            if (WorkshopBattlePayloadBridge.TryConsume(out WorkshopBattlePayload payload))
            {
                currentDeck = new List<WorkshopBattleCardEntry>(payload.Cards);
                Debug.Log($"BattleScene: loaded {payload.Cards.Count} card type(s) from workshop.");
                foreach (WorkshopBattleCardEntry card in payload.Cards)
                {
                    Debug.Log($"  - {card.DisplayName} x{card.Amount} ({card.Role}, {card.Element})");
                }
            }
            else
            {
                Debug.LogWarning("BattleScene: no workshop payload found. Using fallback deck.");
                currentDeck.Clear();
            }
        }

        private void InitializeBoss()
        {
            if (contentDatabase == null)
            {
                Debug.LogError("BattleSceneController: contentDatabase is not assigned.");
                return;
            }

            CurrentBossDefinition = contentDatabase.FindBoss(startingBossId);
            if (CurrentBossDefinition == null)
            {
                Debug.LogError($"BattleSceneController: boss '{startingBossId}' not found in database.");
                return;
            }

            Boss = new BattleUnit
            {
                DisplayName = CurrentBossDefinition.DisplayName,
                MaxHealth = CurrentBossDefinition.MaxHealth,
                CurrentHealth = CurrentBossDefinition.MaxHealth,
                Shield = 0,
                Element = CurrentBossDefinition.Element
            };

            Debug.Log($"BattleScene: Boss '{Boss.DisplayName}' initialized with {Boss.MaxHealth} HP.");
        }

        private void StartBattle()
        {
            WorkshopBattlePayload payload = currentDeck.Count > 0
                ? new WorkshopBattlePayload { Cards = currentDeck }
                : null;

            BattleDeckController deck = new BattleDeckController(contentDatabase, payload);
            BattleBossAI bossAI = new BattleBossAI(CurrentBossDefinition);
            bossAI.BindUnits(Boss, Player);

            Simulation = new BattleSimulation(Player, Boss, deck, bossAI);
            CurrentResult = null;
            recentEvents.Clear();
            Simulation.PlayerActionResolved += OnPlayerActionResolved;
            Simulation.BossActionResolved += OnBossActionResolved;
            Simulation.PlayerTurnSkipped += OnPlayerTurnSkipped;
            Simulation.TurnCycleComplete += OnTurnCycleComplete;
            Simulation.BattleEnded += OnBattleEnded;

            if (visualManager == null)
            {
                visualManager = GetComponent<BattleVisualManager>();
                if (visualManager == null)
                {
                    visualManager = gameObject.AddComponent<BattleVisualManager>();
                }
            }
            visualManager.Initialize(Simulation, Player, Boss, contentDatabase, CurrentBossDefinition.BossId);

            if (hudPresenter == null)
            {
                hudPresenter = GetComponent<BattleHudPresenter>();
                if (hudPresenter == null)
                {
                    hudPresenter = gameObject.AddComponent<BattleHudPresenter>();
                }
            }
            hudPresenter.Initialize(this);

            AddRecentEvent($"Battle started vs {Boss.DisplayName}.");
            LogHandState();
            Debug.Log($"=== Battle started vs {Boss.DisplayName} ({Boss.MaxHealth} HP) ===");
        }

        private void HandlePlayerInput()
        {
            if (Simulation.State != BattleState.WaitingForPlayer)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Simulation.SkipTurn();
                return;
            }

            for (int i = 0; i < Mathf.Min(9, Simulation.Deck.Hand.Count); i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    Simulation.TryPlayCard(i);
                    break;
                }
            }
        }

        public bool TryPlayCardFromHud(int handIndex)
        {
            if (!IsPlayerInputAllowed)
            {
                return false;
            }

            return Simulation.TryPlayCard(handIndex);
        }

        public bool SkipTurnFromHud()
        {
            if (!IsPlayerInputAllowed)
            {
                return false;
            }

            Simulation.SkipTurn();
            return true;
        }

        private void OnPlayerActionResolved(BattleActionResolution resolution)
        {
            AddRecentEvent(resolution.LogDescription);
            Debug.Log(resolution.LogDescription);
            LogUnitStatus();
            LogHandState();
        }

        private void OnBossActionResolved(BattleActionResolution resolution)
        {
            AddRecentEvent(resolution.LogDescription);
            Debug.Log(resolution.LogDescription);
            LogUnitStatus();
        }

        private void OnPlayerTurnSkipped()
        {
            AddRecentEvent("Player skipped their turn.");
            Debug.Log("Player skipped their turn.");
            LogHandState();
        }

        private void OnTurnCycleComplete(int turnNumber)
        {
            AddRecentEvent($"Turn {turnNumber} complete.");
            Debug.Log($"--- Turn {turnNumber} complete ---");
        }

        private void OnBattleEnded(BattleResult result)
        {
            CurrentResult = result;
            string outcome = result.ResultType == BattleResultType.Victory ? "VICTORY" : "DEFEAT";
            AddRecentEvent(outcome);
            Debug.Log($"=== {outcome} ===");
            Debug.Log($"Damage dealt: {result.TotalDamageDealt} | Healing: {result.TotalHealingDone} | Shield: {result.TotalShieldGained}");
            Debug.Log($"Cards played: {result.CardsPlayed} | Turns elapsed: {result.TurnsElapsed}");

            BattleResultBridge.Commit(result);
            Debug.Log("BattleResult committed to bridge.");
        }

        private void LogUnitStatus()
        {
            Debug.Log($"[Player] HP: {Player.CurrentHealth}/{Player.MaxHealth} | Shield: {Player.Shield}");
            Debug.Log($"[Boss]   HP: {Boss.CurrentHealth}/{Boss.MaxHealth} | Shield: {Boss.Shield}");
        }

        private void LogHandState()
        {
            if (Simulation == null || Simulation.State == BattleState.BattleEnded)
            {
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Hand: ");
            for (int i = 0; i < Simulation.Deck.Hand.Count; i++)
            {
                WorkshopBattleCardEntry card = Simulation.Deck.Hand[i];
                sb.Append($"[{i + 1}]{card.DisplayName}({card.Role}) ");
            }
            sb.Append($"| Draw: {Simulation.Deck.DrawPileCount} | Discard: {Simulation.Deck.DiscardPileCount}");
            Debug.Log(sb.ToString());
        }

        private void AddRecentEvent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            recentEvents.Add(message);
            if (recentEvents.Count > MaxRecentEvents)
            {
                recentEvents.RemoveAt(0);
            }
        }
    }
}
