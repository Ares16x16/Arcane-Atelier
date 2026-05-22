using ArcaneAtelier.Audio;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        private const int MaxRecentEvents = 8;
        private const float BossTurnTransitionDelay = 1.1f;

        [SerializeField] private BattleContentDatabase contentDatabase;
        [SerializeField] private BattleVisualManager visualManager;
        [SerializeField] private BattleHudPresenter hudPresenter;
        [SerializeField] private BattleFeedbackPresenter feedbackPresenter;
        [SerializeField] private string startingBossId = "enemy.ash.imp";
        [SerializeField] private int playerMaxHealth = 60;
        [SerializeField] private string[] encounterSequence = new[]
        {
            "enemy.ash.imp",
            "enemy.mist.leech",
            "enemy.moss.shell",
            "boss.earth.golem"
        };

        private readonly List<string> recentEvents = new List<string>();
        private readonly List<BattleBossDefinition> encounterDefinitions = new List<BattleBossDefinition>();
        public BattleUnit Player { get; private set; }
        public BattleUnit Boss { get; private set; }
        public BattleSimulation Simulation { get; private set; }
        public BattleBossDefinition CurrentBossDefinition { get; private set; }
        public BattleResult CurrentResult { get; private set; }
        public IReadOnlyList<string> RecentEvents => recentEvents;
        public BattleVisualManager VisualManager => visualManager;
        public int CurrentEncounterNumber => currentEncounterIndex + 1;
        public int TotalEncounterCount => encounterDefinitions.Count;
        public bool ShouldShowRunSummaryPage =>
            CurrentResult != null &&
            CurrentResult.ResultType == BattleResultType.Victory &&
            RunProgressBridge.CurrentSummary.RunEnded &&
            RunProgressBridge.CurrentSummary.RunWon;
        public bool IsPlayerInputAllowed =>
            Simulation != null &&
            Simulation.State == BattleState.WaitingForPlayer &&
            !inputBlocked;
        public bool CanEndTurn =>
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
        private BattleDeckController persistentDeck;
        private bool inputBlocked;
        private int currentEncounterIndex;
        private int totalDamageDealt;
        private int totalHealingDone;
        private int totalShieldGained;
        private int totalCardsPlayed;
        private int totalTurnsElapsed;
        private float lastCardFeedbackAt = -10f;
        private float pendingBossTurnUntil = -1f;
        private BattleState lastObservedState = BattleState.WaitingForPlayer;
        public BattleFeedbackPresenter FeedbackPresenter => feedbackPresenter;
        public bool IsBossTurnPending => Simulation != null && Simulation.State == BattleState.BossTurnPending;
        public float BossTurnWindupProgress
        {
            get
            {
                if (!IsBossTurnPending)
                {
                    return 0f;
                }

                if (pendingBossTurnUntil < 0f)
                {
                    return 0f;
                }

                float remaining = pendingBossTurnUntil - Time.unscaledTime;
                return Mathf.Clamp01(1f - remaining / BossTurnTransitionDelay);
            }
        }

        private void Awake()
        {
            InitializePlayer();
            LoadWorkshopPayload();
            BuildEncounterSequence();
        }

        private void Start()
        {
            if (encounterDefinitions.Count == 0)
            {
                Debug.LogError("BattleSceneController: cannot start battle without a valid encounter sequence.");
                enabled = false;
                return;
            }

            var currentEncounter = RunProgressBridge.CurrentEncounter;
    
            if (currentEncounter != null)
            {
                this.currentEncounterIndex = currentEncounter.EncounterIndex - 1;                
                Debug.Log($"Successfully loaded encounter: {currentEncounter.EncounterLabel}");
            }
            else
            {
                Debug.LogWarning("No encounter data found in RunProgressBridge! Defaulting to 0.");
                this.currentEncounterIndex = 0;
            }

            totalDamageDealt = 0;
            totalHealingDone = 0;
            totalShieldGained = 0;
            totalCardsPlayed = 0;
            totalTurnsElapsed = 0;
            lastObservedState = BattleState.WaitingForPlayer;
            StartEncounter(currentEncounterIndex, resetDeck: true);
        }

        private void Update()
        {
            if (Simulation == null || Simulation.State == BattleState.BattleEnded || inputBlocked)
            {
                return;
            }

            ObserveBattleState();
            AdvancePendingBossTurn();
            HandlePlayerInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSimulation();
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

        private void BuildEncounterSequence()
        {
            if (contentDatabase == null)
            {
                Debug.LogError("BattleSceneController: contentDatabase is not assigned.");
                return;
            }

            encounterDefinitions.Clear();
            string[] configuredSequence = encounterSequence != null && encounterSequence.Length > 0
                ? encounterSequence
                : new[] { startingBossId };

            foreach (string bossId in configuredSequence)
            {
                if (string.IsNullOrWhiteSpace(bossId))
                {
                    continue;
                }

                BattleBossDefinition definition = contentDatabase.FindBoss(bossId);
                if (definition == null)
                {
                    Debug.LogError($"BattleSceneController: encounter '{bossId}' not found in database.");
                    continue;
                }

                encounterDefinitions.Add(definition);
            }

            if (encounterDefinitions.Count == 0 && !string.IsNullOrWhiteSpace(startingBossId))
            {
                BattleBossDefinition fallbackDefinition = contentDatabase.FindBoss(startingBossId);
                if (fallbackDefinition != null)
                {
                    encounterDefinitions.Add(fallbackDefinition);
                }
            }
        }

        private void StartEncounter(int encounterIndex, bool resetDeck)
        {
            if (encounterIndex < 0 || encounterIndex >= encounterDefinitions.Count)
            {
                Debug.LogError($"BattleSceneController: encounter index '{encounterIndex}' is out of range.");
                return;
            }

            CurrentBossDefinition = encounterDefinitions[encounterIndex];
            Boss = new BattleUnit
            {
                DisplayName = CurrentBossDefinition.DisplayName,
                MaxHealth = CurrentBossDefinition.MaxHealth,
                CurrentHealth = CurrentBossDefinition.MaxHealth,
                Shield = 0,
                Element = CurrentBossDefinition.Element
            };

            if (resetDeck || persistentDeck == null)
            {
                WorkshopBattlePayload payload = currentDeck.Count > 0
                    ? new WorkshopBattlePayload { Cards = currentDeck }
                    : null;
                persistentDeck = new BattleDeckController(contentDatabase, payload);
            }

            ClearPlayerEncounterState();
            BattleBossAI bossAI = new BattleBossAI(CurrentBossDefinition);
            bossAI.BindUnits(Boss, Player);

            UnsubscribeFromSimulation();
            Simulation = new BattleSimulation(Player, Boss, persistentDeck, bossAI, contentDatabase);
            lastObservedState = Simulation.State;
            CurrentResult = null;
            recentEvents.Clear();
            ApplyPreparationBonus();
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
            feedbackPresenter = EnsureFeedbackPresenter();
            feedbackPresenter.Initialize(this, visualManager.BattleCamera);

            if (hudPresenter == null)
            {
                hudPresenter = GetComponent<BattleHudPresenter>();
                if (hudPresenter == null)
                {
                    hudPresenter = gameObject.AddComponent<BattleHudPresenter>();
                }
            }
            hudPresenter.Initialize(this);
            PublishTurnBanner("Your Turn", 1);

            AddRecentEvent($"Encounter {encounterIndex + 1}/{encounterDefinitions.Count}: {Boss.DisplayName}");
            LogHandState();
            Debug.Log($"=== Encounter {encounterIndex + 1}/{encounterDefinitions.Count} started vs {Boss.DisplayName} ({Boss.MaxHealth} HP) ===");

            AudioManager.PlayMusic(MusicTrack.Battle);
        }

        private void HandlePlayerInput()
        {
            if (Simulation.State != BattleState.WaitingForPlayer)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Simulation.EndTurn();
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

            if (handIndex < 0 || handIndex >= Simulation.Deck.HandCount)
            {
                return false;
            }

            WorkshopBattleCardEntry card = Simulation.Deck.Hand[handIndex];
            int apCost = BattleDeckController.GetActionPointCost(card.Role);
            if (Simulation.ActionPoints < apCost)
            {
                AudioManager.PlaySFX(SFXType.ErrorBuzz);
                return false;
            }

            bool success = Simulation.TryPlayCard(handIndex);
            if (success)
            {
                var sfx = card.Element switch
                {
                    WorkshopElementAttribute.Fire    => SFXType.FireHit,
                    WorkshopElementAttribute.Water   => SFXType.WaterHit,
                    WorkshopElementAttribute.Wind    => SFXType.WindHit,
                    WorkshopElementAttribute.Earth   => SFXType.EarthHit,
                    WorkshopElementAttribute.Ice     => SFXType.IceHit,
                    WorkshopElementAttribute.Thunder => SFXType.ThunderHit,
                    WorkshopElementAttribute.Light   => SFXType.LightHit,
                    WorkshopElementAttribute.Dark    => SFXType.DarkHit,
                    _                                => SFXType.CardPlayWhoosh,
                };
                AudioManager.PlaySFX(sfx);
            }
            return success;
        }

        public bool CanPlayCard(int handIndex)
        {
            if (!IsPlayerInputAllowed)
            {
                return false;
            }

            if (handIndex < 0 || handIndex >= Simulation.Deck.HandCount)
            {
                return false;
            }

            WorkshopBattleCardEntry card = Simulation.Deck.Hand[handIndex];
            int apCost = BattleDeckController.GetActionPointCost(card.Role);
            return Simulation.ActionPoints >= apCost;
        }

        public bool EndTurnFromHud()
        {
            if (!CanEndTurn)
            {
                return false;
            }

            AudioManager.PlaySFX(SFXType.EndTurnConfirm);
            Simulation.EndTurn();
            return true;
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenuScene");
        }

        public void ReturnToWorkshop()
        {
            if (CurrentResult != null)
            {
                BattleResultBridge.Commit(CurrentResult);
            }
            
            SceneManager.LoadScene("WorkshopScene");
        }

        public BattleCardDefinition GetCardDefinition(string cardId)
        {
            if (contentDatabase == null || string.IsNullOrWhiteSpace(cardId))
            {
                return null;
            }

            return contentDatabase.FindCardDefinition(cardId);
        }

        private void OnPlayerActionResolved(BattleActionResolution resolution)
        {
            PublishResolutionFeedback(resolution, isPlayerAction: true);
            AddRecentEvent(resolution.LogDescription);
            Debug.Log(resolution.LogDescription);
            LogUnitStatus();
            LogHandState();
            PlayResolutionSFX(resolution, isPlayerAction: true);
        }

        private void OnBossActionResolved(BattleActionResolution resolution)
        {
            PublishResolutionFeedback(resolution, isPlayerAction: false);
            AddRecentEvent(resolution.LogDescription);
            Debug.Log(resolution.LogDescription);
            LogUnitStatus();
            PlayResolutionSFX(resolution, isPlayerAction: false);
        }

        private void PlayResolutionSFX(BattleActionResolution resolution, bool isPlayerAction)
        {
            if (resolution.DamageDealt > 0)
            {
                // Player damage SFX already played on card play — skip generic hit
                if (!isPlayerAction)
                    AudioManager.PlaySFX(SFXType.AttackHitGeneric);
            }
            else if (resolution.HealingDone > 0)
                AudioManager.PlaySFX(SFXType.HealRestore);
            else if (resolution.ShieldGained > 0)
                AudioManager.PlaySFX(SFXType.ShieldBlock);
        }

        private void OnPlayerTurnSkipped()
        {
            feedbackPresenter?.Show(new BattleFeedbackRequest(BattleFeedbackKind.ActionCallout, BattleFeedbackTarget.None, "Turn End"));
            AddRecentEvent("Player skipped their turn.");
            Debug.Log("Player skipped their turn.");
            LogHandState();
        }

        private void OnTurnCycleComplete(int turnNumber)
        {
            PublishTurnBanner("Your Turn", turnNumber + 1);
            AddRecentEvent($"Turn {turnNumber} complete.");
            Debug.Log($"--- Turn {turnNumber} complete ---");
        }

        private void OnBattleEnded(BattleResult result)
        {
            AccumulateEncounterStats(result);
            BattleResult finalResult = BuildFinalResult(result);
            finalResult.TokensEarned = BattleRewardCalculator.Compute(finalResult, CurrentBossDefinition);
            CurrentResult = finalResult;
            RecordRunProgress(finalResult);

            if (result.ResultType == BattleResultType.Victory)
            {
                AudioManager.StopMusic();
                AudioManager.PlaySting(MusicTrack.VictorySting);
                string clearedName = result.BossDisplayName;
                
                int healAmount = 15;
                Player.Heal(healAmount);
                
                AddRecentEvent($"{clearedName} defeated. Recovered {healAmount} HP.");
                Debug.Log($"=== ENCOUNTER CLEARED: {clearedName} === Recovered {healAmount} HP ===");
                
            }
            else
            {
                AudioManager.StopMusic();
                AudioManager.PlaySting(MusicTrack.DefeatSting);
            }


            string outcome = finalResult.ResultType == BattleResultType.Victory ? "VICTORY" : "DEFEAT";
            AddRecentEvent(outcome);
            Debug.Log($"=== {outcome} ===");
            Debug.Log($"Damage dealt: {finalResult.TotalDamageDealt} | Healing: {finalResult.TotalHealingDone} | Shield: {finalResult.TotalShieldGained}");
            Debug.Log($"Cards played: {finalResult.CardsPlayed} | Turns elapsed: {finalResult.TurnsElapsed}");

            BattleResultBridge.Commit(finalResult);
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

        private BattleFeedbackPresenter EnsureFeedbackPresenter()
        {
            if (feedbackPresenter == null)
            {
                feedbackPresenter = GetComponent<BattleFeedbackPresenter>();
                if (feedbackPresenter == null)
                {
                    feedbackPresenter = gameObject.AddComponent<BattleFeedbackPresenter>();
                }
            }

            return feedbackPresenter;
        }

        private void AdvancePendingBossTurn()
        {
            if (Simulation == null || Simulation.State != BattleState.BossTurnPending)
            {
                pendingBossTurnUntil = -1f;
                return;
            }

            if (pendingBossTurnUntil < 0f)
            {
                pendingBossTurnUntil = Time.unscaledTime + BossTurnTransitionDelay;
                return;
            }

            if (Time.unscaledTime < pendingBossTurnUntil)
            {
                return;
            }

            pendingBossTurnUntil = -1f;
            Simulation.AdvancePendingTurn();
        }

        private void ObserveBattleState()
        {
            if (Simulation == null)
            {
                return;
            }

            if (Simulation.State == lastObservedState)
            {
                return;
            }

            BattleState previousState = lastObservedState;
            lastObservedState = Simulation.State;

            if (Simulation.State == BattleState.BossTurnPending)
            {
                ScheduleBossTurnPresentation();
                return;
            }

            if (previousState == BattleState.BossTurnPending && Simulation.State == BattleState.ResolvingBoss)
            {
                PublishPendingBossAction();
            }
        }

        private void ScheduleBossTurnPresentation()
        {
            if (Simulation == null)
            {
                return;
            }

            pendingBossTurnUntil = Time.unscaledTime + BossTurnTransitionDelay;
            PublishTurnBanner("Enemy Turn", Simulation.TurnsElapsed + 1);
            feedbackPresenter?.Show(new BattleFeedbackRequest(
                BattleFeedbackKind.ActionCallout,
                BattleFeedbackTarget.None,
                "Enemy prepares",
                BossIntentDescription,
                duration: BossTurnTransitionDelay));
        }

        private void PublishPendingBossAction()
        {
            if (Simulation == null || Simulation.BossAI == null)
            {
                return;
            }

            BattleBossAction action = Simulation.BossAI.PeekNextAction();
            if (action == null)
            {
                return;
            }

            string title = string.IsNullOrWhiteSpace(action.Description) ? "Enemy acts" : action.Description;
            string subtitle = $"{Boss.DisplayName} • {GetIntentLabel(action.ActionType)}";
            feedbackPresenter?.Show(new BattleFeedbackRequest(
                BattleFeedbackKind.ActionCallout,
                BattleFeedbackTarget.None,
                title,
                subtitle,
                duration: 0.9f));
        }

        private void PublishTurnBanner(string title, int turnNumber)
        {
            feedbackPresenter?.Show(new BattleFeedbackRequest(
                BattleFeedbackKind.TurnBanner,
                BattleFeedbackTarget.None,
                title,
                amount: turnNumber));
        }

        private static string GetIntentLabel(BattleActionType actionType)
        {
            switch (actionType)
            {
                case BattleActionType.Attack:
                    return "Attack Incoming";
                case BattleActionType.Defend:
                    return "Defense";
                case BattleActionType.Heal:
                    return "Recovery";
                case BattleActionType.Special:
                    return "Special";
                default:
                    return "Action";
            }
        }

        private void PublishResolutionFeedback(BattleActionResolution resolution, bool isPlayerAction)
        {
            if (feedbackPresenter == null)
            {
                return;
            }

            if (isPlayerAction && !string.IsNullOrWhiteSpace(Simulation?.Deck?.LastPlayedDefinition?.DisplayName))
            {
                if (Time.unscaledTime - lastCardFeedbackAt > 0.08f)
                {
                    feedbackPresenter.Show(new BattleFeedbackRequest(
                        BattleFeedbackKind.CardPlayed,
                        BattleFeedbackTarget.None,
                        Simulation.Deck.LastPlayedDefinition.DisplayName));
                    lastCardFeedbackAt = Time.unscaledTime;
                }
            }
            else if (!isPlayerAction && !string.IsNullOrWhiteSpace(resolution.PrimaryText))
            {
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.ActionCallout,
                    BattleFeedbackTarget.None,
                    resolution.PrimaryText));
            }

            if (resolution.DamageDealt > 0)
            {
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.Damage,
                    resolution.Target,
                    string.Empty,
                    amount: resolution.DamageDealt,
                    emphasize: resolution.DamageDealt >= 12));
            }

            if (resolution.HealingDone > 0)
            {
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.Heal,
                    resolution.Target,
                    string.Empty,
                    amount: resolution.HealingDone));
            }

            if (resolution.ShieldGained > 0)
            {
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.Shield,
                    resolution.Target,
                    string.Empty,
                    amount: resolution.ShieldGained));
            }

            if (resolution.FeedbackKind == BattleFeedbackKind.StatusApplied)
            {
                string statusText = resolution.StatusDuration > 0
                    ? $"{resolution.StatusId} +{resolution.StatusDuration}T"
                    : resolution.StatusId;
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.StatusApplied,
                    resolution.Target,
                    statusText));
            }
            else if (resolution.FeedbackKind == BattleFeedbackKind.StatusTick)
            {
                feedbackPresenter.Show(new BattleFeedbackRequest(
                    BattleFeedbackKind.StatusTick,
                    resolution.Target,
                    resolution.PrimaryText));
            }
        }

        private void ClearPlayerEncounterState()
        {
            Player.Shield = 0;
            if (Player.StatusEffectController != null)
            {
                Player.StatusEffectController.ClearEffects(Player);
            }
        }

        private void ApplyPreparationBonus()
        {
            int openingShieldBonus = Mathf.Max(0, RunProgressBridge.PendingStartingShieldBonus);
            if (openingShieldBonus <= 0)
            {
                return;
            }

            Player.AddShield(openingShieldBonus);
            AddRecentEvent($"Workshop bonus: +{openingShieldBonus} opening shield.");
            Debug.Log($"BattleScene: applied workshop opening shield bonus of {openingShieldBonus}.");
        }

        private void AccumulateEncounterStats(BattleResult result)
        {
            totalDamageDealt += result.TotalDamageDealt;
            totalHealingDone += result.TotalHealingDone;
            totalShieldGained += result.TotalShieldGained;
            totalCardsPlayed += result.CardsPlayed;
            totalTurnsElapsed += result.TurnsElapsed;
        }

        private BattleResult BuildFinalResult(BattleResult result)
        {
            return new BattleResult
            {
                ResultType = result.ResultType,
                BossId = result.BossId,
                BossDisplayName = result.BossDisplayName,
                EncountersCleared = result.ResultType == BattleResultType.Victory ? currentEncounterIndex + 1 : currentEncounterIndex,
                FinalEncounterId = result.BossId,
                TotalDamageDealt = totalDamageDealt,
                TotalHealingDone = totalHealingDone,
                TotalShieldGained = totalShieldGained,
                CardsPlayed = totalCardsPlayed,
                TurnsElapsed = totalTurnsElapsed,
                DefeatRewardId = result.ResultType == BattleResultType.Victory ? result.DefeatRewardId : string.Empty,
                PlayerFinalHealth = Player != null ? Player.CurrentHealth : 0,
                PlayerMaxHealth = Player != null ? Player.MaxHealth : 0
            };
        }

        private void RecordRunProgress(BattleResult result)
        {
            bool victory = result != null && result.ResultType == BattleResultType.Victory;
            bool finalBossVictory = victory && RunProgressBridge.CurrentEncounter != null && RunProgressBridge.CurrentEncounter.IsBoss;

            string finalOutcomeTitle;
            string finalOutcomeDescription;
            string legacyUnlockName = string.Empty;
            if (finalBossVictory)
            {
                int runTokensEarned = RunProgressBridge.CurrentSummary.TotalTokensEarned + (result != null ? result.TokensEarned : 0);
                int legacySigilsEarned = MetaProgressionStore.RecordFinalBossClear(runTokensEarned);
                finalOutcomeTitle = "Atelier Secured";
                finalOutcomeDescription = $"The core breach is sealed. +{legacySigilsEarned} Legacy Sigils were carved into the archive for future cycles.";
                legacyUnlockName = $"+{legacySigilsEarned} Legacy Sigils";
            }
            else if (victory)
            {
                finalOutcomeTitle = "Encounter Cleared";
                finalOutcomeDescription = "Workshop output recorded. Return to the atelier and prepare the next breach response.";
            }
            else
            {
                finalOutcomeTitle = "Workshop Overrun";
                finalOutcomeDescription = "The breach held. Review the workshop output and battle pacing before the next run.";
            }

            RunProgressBridge.RecordBattleResult(
                result != null ? result.BossDisplayName : Boss != null ? Boss.DisplayName : "Enemy",
                victory,
                result != null ? result.TurnsElapsed : 0,
                result != null ? result.CardsPlayed : 0,
                result != null ? result.TotalDamageDealt : 0,
                result != null ? result.TotalHealingDone : 0,
                result != null ? result.TotalShieldGained : 0,
                result != null ? result.TokensEarned : 0,
                finalOutcomeTitle,
                finalOutcomeDescription,
                legacyUnlockName);
        }

        private void UnsubscribeFromSimulation()
        {
            if (Simulation == null)
            {
                return;
            }

            Simulation.PlayerActionResolved -= OnPlayerActionResolved;
            Simulation.BossActionResolved -= OnBossActionResolved;
            Simulation.PlayerTurnSkipped -= OnPlayerTurnSkipped;
            Simulation.TurnCycleComplete -= OnTurnCycleComplete;
            Simulation.BattleEnded -= OnBattleEnded;
        }
    }
}
