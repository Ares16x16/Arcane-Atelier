using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        private const string WorkshopSceneName = "WorkshopScene";
        private const int DefaultOpeningHandSize = 5;
        private const int DefaultMaxHandSize = 10;

        [SerializeField] private BattleContentDatabase contentDatabase;
        [SerializeField] private string startingBossId = "boss.earth.golem";
        [SerializeField] private int playerMaxHealth = 100;
        [SerializeField] private int playerEnergyPerTurn = 3;
        [SerializeField] private bool useEmergencyDeckWhenEmpty = true;
        [SerializeField] private bool showFallbackGui = true;

        private readonly List<WorkshopBattleCardEntry> preparedDeck = new List<WorkshopBattleCardEntry>();
        private readonly List<WorkshopBattleCardEntry> drawPile = new List<WorkshopBattleCardEntry>();
        private readonly List<WorkshopBattleCardEntry> discardPile = new List<WorkshopBattleCardEntry>();

        public BattleUnit Player { get; private set; }
        public BattleUnit Boss { get; private set; }
        public BattleBossDefinition CurrentBossDefinition { get; private set; }
        public List<WorkshopBattleCardEntry> Cards { get; } = new List<WorkshopBattleCardEntry>();
        public int CurrentEnergy { get; private set; }
        public int TurnNumber { get; private set; } = 1;
        public int CardsPlayed { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int TotalHealingDone { get; private set; }
        public int TotalShieldGained { get; private set; }
        public int HandVersion { get; private set; }
        public bool BattleEnded { get; private set; }
        public string LastActionMessage { get; private set; } = "Battle started.";
        public string CurrentBossIntent => FormatBossIntent(PeekBossAction());
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public bool WillShuffleOnNextDraw => drawPile.Count == 0 && discardPile.Count > 0;
        public int TotalDeckCardCount => preparedDeck.Sum(entry => Mathf.Max(1, entry.Amount));
        public int CardsDrawPerTurn => Mathf.Clamp(Mathf.CeilToInt(TotalDeckCardCount * 0.4f), 1, 4);
        public int OpeningHandSize => CardsDrawPerTurn;
        public int MaxHandSize => DefaultMaxHandSize;
        public int EnergyPerTurn => Mathf.Max(1, playerEnergyPerTurn);
        public string EncounterLabel => RunProgressBridge.CurrentEncounter?.EncounterLabel ?? "Breach";
        public string EncounterDescription => RunProgressBridge.CurrentEncounter?.EncounterDescription ?? string.Empty;
        public bool CurrentEncounterIsBoss => RunProgressBridge.CurrentEncounter?.IsBoss ?? false;

        private int bossActionIndex;

        private void Awake()
        {
            showFallbackGui = showFallbackGui && GetComponent<BattleUIPresenter>() == null;
            InitializePlayer();
            LoadWorkshopPayload();
            InitializeBoss();
            EnsurePlayableDeck();
            BuildDrawPile();
            BeginPlayerTurn();
        }

        private void InitializePlayer()
        {
            Player = new BattleUnit
            {
                DisplayName = string.Empty,
                MaxHealth = playerMaxHealth,
                CurrentHealth = playerMaxHealth,
                Shield = 0,
                Element = WorkshopElementAttribute.None
            };

            if (RunProgressBridge.PendingStartingShieldBonus > 0)
            {
                Player.AddShield(RunProgressBridge.PendingStartingShieldBonus);
            }
        }

        private void LoadWorkshopPayload()
        {
            preparedDeck.Clear();

            if (!WorkshopBattlePayloadBridge.TryConsume(out WorkshopBattlePayload payload))
            {
                Debug.LogWarning("BattleScene: no workshop payload found. Running with fallback behavior.");
                return;
            }

            if (payload.Cards == null)
            {
                return;
            }

            foreach (WorkshopBattleCardEntry card in payload.Cards)
            {
                if (string.IsNullOrWhiteSpace(card.CardId) || card.Amount <= 0)
                {
                    continue;
                }

                preparedDeck.Add(NormalizeCardEntry(card));
                Debug.Log($"BattleScene: loaded {card.DisplayName} x{card.Amount} ({card.Role}, {card.Element}).");
            }
        }

        private void InitializeBoss()
        {
            string encounterBossId = string.IsNullOrWhiteSpace(RunProgressBridge.CurrentEncounter?.BossId)
                ? startingBossId
                : RunProgressBridge.CurrentEncounter.BossId;

            CurrentBossDefinition = contentDatabase != null ? contentDatabase.FindBoss(encounterBossId) : null;
            if (CurrentBossDefinition == null)
            {
                Debug.LogWarning($"BattleSceneController: boss '{encounterBossId}' not found. Using runtime fallback boss.");
                CurrentBossDefinition = CreateFallbackBoss(encounterBossId);
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

        public bool CanPlayCard(int cardIndex)
        {
            return !BattleEnded
                && Player != null
                && Player.IsAlive
                && Boss != null
                && Boss.IsAlive
                && cardIndex >= 0
                && cardIndex < Cards.Count
                && CurrentEnergy >= GetCardEnergyCost(Cards[cardIndex]);
        }

        public int GetCardEnergyCost(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= Cards.Count)
            {
                return 1;
            }

            return GetCardEnergyCost(Cards[cardIndex]);
        }

        public void PlayCard(int cardIndex)
        {
            if (!CanPlayCard(cardIndex))
            {
                LastActionMessage = BattleEnded ? "Battle is already over." : "That card cannot be played right now.";
                return;
            }

            WorkshopBattleCardEntry card = Cards[cardIndex];
            BattleResolvedEffect effect = ResolveCard(card);
            CurrentEnergy -= GetCardEnergyCost(card);
            CardsPlayed++;

            Cards.RemoveAt(cardIndex);
            discardPile.Add(card);
            MarkHandDirty();

            switch (effect.Role)
            {
                case WorkshopSpellRole.Attack:
                    ResolveAttack(card, effect);
                    break;
                case WorkshopSpellRole.Healing:
                    ResolveHealing(card, effect);
                    break;
                case WorkshopSpellRole.Defense:
                    ResolveDefense(card, effect);
                    break;
                default:
                    ResolveAttack(card, effect);
                    break;
            }

            CheckBattleEnd();
        }

        public void EndPlayerTurn()
        {
            if (BattleEnded)
            {
                return;
            }

            DiscardHand();
            ResolveBossAction();
            CheckBattleEnd();

            if (!BattleEnded)
            {
                TurnNumber++;
                BeginPlayerTurn();
            }
        }

        public void ReturnToWorkshop()
        {
            SceneManager.LoadScene(RunProgressBridge.CurrentSummary.RunEnded ? "MainMenuScene" : WorkshopSceneName);
        }

        public string GetCardSummary(WorkshopBattleCardEntry card)
        {
            return DescribeCard(card);
        }

        private void BeginPlayerTurn()
        {
            if (BattleEnded)
            {
                return;
            }

            Player.Shield = 0;
            CurrentEnergy = EnergyPerTurn;
            int cardsDrawn = DrawCards(OpeningHandSize);
            LastActionMessage = cardsDrawn > 0
                ? $"Turn {TurnNumber}. Drew {cardsDrawn} card(s). Enemy intent: {CurrentBossIntent}"
                : $"Turn {TurnNumber}. No cards left to draw. Enemy intent: {CurrentBossIntent}";
        }

        private void EnsurePlayableDeck()
        {
            if (!useEmergencyDeckWhenEmpty || preparedDeck.Count > 0)
            {
                return;
            }

            preparedDeck.Add(CreateEmergencyCard("combat.emergency.arcane_bolt", "Arcane Bolt", WorkshopSpellRole.Attack, 8, 2, WorkshopElementAttribute.None));
            preparedDeck.Add(CreateEmergencyCard("combat.emergency.guard_thread", "Guard Thread", WorkshopSpellRole.Defense, 8, 2, WorkshopElementAttribute.None));
            LastActionMessage = "No workshop payload found. Emergency deck supplied.";
        }

        private void BuildDrawPile()
        {
            drawPile.Clear();
            discardPile.Clear();
            Cards.Clear();

            foreach (WorkshopBattleCardEntry entry in preparedDeck)
            {
                int copies = Mathf.Max(1, entry.Amount);
                for (int index = 0; index < copies; index++)
                {
                    drawPile.Add(CreateBattleCopy(entry));
                }
            }

            Shuffle(drawPile);
            MarkHandDirty();
        }

        private int DrawCards(int amount)
        {
            int drawn = 0;
            while (drawn < amount && Cards.Count < MaxHandSize)
            {
                if (drawPile.Count == 0)
                {
                    ReshuffleDiscardIntoDrawPile();
                }

                if (drawPile.Count == 0)
                {
                    break;
                }

                int lastIndex = drawPile.Count - 1;
                Cards.Add(drawPile[lastIndex]);
                drawPile.RemoveAt(lastIndex);
                drawn++;
            }

            if (drawn > 0)
            {
                MarkHandDirty();
            }

            return drawn;
        }

        private void DiscardHand()
        {
            if (Cards.Count == 0)
            {
                return;
            }

            discardPile.AddRange(Cards);
            Cards.Clear();
            MarkHandDirty();
        }

        private void ReshuffleDiscardIntoDrawPile()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        private static void Shuffle(List<WorkshopBattleCardEntry> cards)
        {
            for (int index = cards.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }
        }

        private static WorkshopBattleCardEntry CreateBattleCopy(WorkshopBattleCardEntry entry)
        {
            return new WorkshopBattleCardEntry
            {
                CardId = entry.CardId,
                DisplayName = entry.DisplayName,
                Amount = 1,
                Element = entry.Element,
                Tier = entry.Tier,
                Role = entry.Role,
                Rarity = entry.Rarity,
                PrimaryValue = entry.PrimaryValue,
                HitCount = entry.HitCount,
                SecondaryValue = entry.SecondaryValue,
                EffectKeyword = entry.EffectKeyword
            };
        }

        private static WorkshopBattleCardEntry NormalizeCardEntry(WorkshopBattleCardEntry card)
        {
            card.Amount = Mathf.Max(1, card.Amount);
            card.HitCount = Mathf.Max(1, card.HitCount);
            return card;
        }

        private static WorkshopBattleCardEntry CreateEmergencyCard(
            string cardId,
            string displayName,
            WorkshopSpellRole role,
            int primaryValue,
            int amount,
            WorkshopElementAttribute element)
        {
            return new WorkshopBattleCardEntry
            {
                CardId = cardId,
                DisplayName = displayName,
                Amount = amount,
                Element = element,
                Tier = WorkshopSpellTier.Basic,
                Role = role,
                Rarity = WorkshopSpellRarity.Common,
                PrimaryValue = primaryValue,
                HitCount = 1,
                SecondaryValue = 0f,
                EffectKeyword = string.Empty
            };
        }

        private int GetCardEnergyCost(WorkshopBattleCardEntry card)
        {
            return 1;
        }

        private BattleResolvedEffect ResolveCard(WorkshopBattleCardEntry card)
        {
            BattleCardEffectTemplate template = contentDatabase != null ? contentDatabase.FindTemplate(card.CardId) : null;
            if (template != null)
            {
                return template.Resolve(card);
            }

            WorkshopSpellRole role = card.Role;
            int primaryValue = card.PrimaryValue;
            int hitCount = Mathf.Max(1, card.HitCount);

            if (role == WorkshopSpellRole.None)
            {
                role = InferRole(card);
            }

            if (primaryValue <= 0)
            {
                primaryValue = GetFallbackPrimaryValue(card, role);
            }

            return new BattleResolvedEffect
            {
                Role = role,
                PrimaryValue = primaryValue,
                HitCount = hitCount,
                SecondaryValue = card.SecondaryValue,
                Element = card.Element
            };
        }

        private static WorkshopSpellRole InferRole(WorkshopBattleCardEntry card)
        {
            string source = ((card.CardId ?? string.Empty) + " " + (card.DisplayName ?? string.Empty)).ToLowerInvariant();
            if (source.Contains("ward") || source.Contains("guard") || source.Contains("sigil"))
            {
                return WorkshopSpellRole.Defense;
            }

            if (source.Contains("mend") || source.Contains("heal") || source.Contains("prayer"))
            {
                return WorkshopSpellRole.Healing;
            }

            return WorkshopSpellRole.Attack;
        }

        private static int GetFallbackPrimaryValue(WorkshopBattleCardEntry card, WorkshopSpellRole role)
        {
            if (role == WorkshopSpellRole.Defense)
            {
                return 10;
            }

            if (role == WorkshopSpellRole.Healing)
            {
                return 8;
            }

            if (!string.IsNullOrEmpty(card.CardId) && card.CardId.ToLowerInvariant().Contains("frost"))
            {
                return 9;
            }

            return 12;
        }

        private void ResolveAttack(WorkshopBattleCardEntry card, BattleResolvedEffect effect)
        {
            int totalDamage = Mathf.Max(1, effect.PrimaryValue) * Mathf.Max(1, effect.HitCount);
            Boss.TakeDamage(totalDamage);

            TotalDamageDealt += totalDamage;
            LastActionMessage = $"{card.DisplayName} dealt {totalDamage} damage.";
        }

        private void ResolveHealing(WorkshopBattleCardEntry card, BattleResolvedEffect effect)
        {
            int totalHealing = effect.PrimaryValue * effect.HitCount;
            int before = Player.CurrentHealth;
            Player.Heal(totalHealing);
            int healed = Player.CurrentHealth - before;
            TotalHealingDone += healed;
            LastActionMessage = $"{card.DisplayName} healed {healed} HP.";
        }

        private void ResolveDefense(WorkshopBattleCardEntry card, BattleResolvedEffect effect)
        {
            int shield = Mathf.Max(1, effect.PrimaryValue * effect.HitCount);
            Player.AddShield(shield);
            TotalShieldGained += shield;
            LastActionMessage = $"{card.DisplayName} granted {shield} shield.";
        }

        private void ResolveBossAction()
        {
            Boss.Shield = 0;
            BattleBossAction action = PeekBossAction();
            bossActionIndex++;

            if (action == null)
            {
                Player.TakeDamage(10);
                LastActionMessage = $"{Boss.DisplayName} attacked for 10 damage.";
                return;
            }

            switch (action.ActionType)
            {
                case BattleActionType.Attack:
                    Player.TakeDamage(action.Value);
                    LastActionMessage = $"{Boss.DisplayName}: {DescribeAction(action, "Attack")} for {action.Value} damage.";
                    break;
                case BattleActionType.Heal:
                    Boss.Heal(action.Value);
                    LastActionMessage = $"{Boss.DisplayName}: {DescribeAction(action, "Heal")} for {action.Value} HP.";
                    break;
                case BattleActionType.Defend:
                    Boss.AddShield(action.Value);
                    LastActionMessage = $"{Boss.DisplayName}: {DescribeAction(action, "Defend")} for {action.Value} shield.";
                    break;
                default:
                    Player.TakeDamage(Mathf.Max(1, action.Value));
                    LastActionMessage = $"{Boss.DisplayName}: {DescribeAction(action, "Special")}.";
                    break;
            }
        }

        private BattleBossAction PeekBossAction()
        {
            if (CurrentBossDefinition == null || CurrentBossDefinition.ActionPattern.Count == 0)
            {
                return null;
            }

            int index = bossActionIndex % CurrentBossDefinition.ActionPattern.Count;
            return CurrentBossDefinition.ActionPattern[index];
        }

        private static string FormatBossIntent(BattleBossAction action)
        {
            if (action == null)
            {
                return "Attack 10";
            }

            string label = string.IsNullOrWhiteSpace(action.Description) ? action.ActionType.ToString() : action.Description;
            return $"{label} ({action.ActionType} {action.Value})";
        }

        private static string DescribeAction(BattleBossAction action, string fallback)
        {
            return string.IsNullOrWhiteSpace(action.Description) ? fallback : action.Description;
        }

        private void CheckBattleEnd()
        {
            if (BattleEnded || Boss == null || Player == null)
            {
                return;
            }

            if (!Boss.IsAlive)
            {
                CommitResult(BattleResultType.Victory);
                return;
            }

            if (!Player.IsAlive)
            {
                CommitResult(BattleResultType.Defeat);
            }
        }

        private void CommitResult(BattleResultType resultType)
        {
            BattleEnded = true;
            CurrentEnergy = 0;
            DiscardHand();
            LastActionMessage = resultType == BattleResultType.Victory
                ? $"Victory over {Boss.DisplayName}."
                : $"Defeated by {Boss.DisplayName}.";

            BattleResultBridge.Commit(new BattleResult
            {
                ResultType = resultType,
                BossId = CurrentBossDefinition != null ? CurrentBossDefinition.BossId : startingBossId,
                BossDisplayName = Boss != null ? Boss.DisplayName : "Enemy",
                TotalDamageDealt = TotalDamageDealt,
                TotalHealingDone = TotalHealingDone,
                TotalShieldGained = TotalShieldGained,
                CardsPlayed = CardsPlayed,
                TurnsElapsed = TurnNumber,
                DefeatRewardId = resultType == BattleResultType.Victory && CurrentBossDefinition != null
                    ? CurrentBossDefinition.DefeatRewardId
                    : string.Empty
            });

            RunProgressBridge.RecordBattleResult(
                Boss != null ? Boss.DisplayName : "Enemy",
                resultType == BattleResultType.Victory,
                TurnNumber,
                CardsPlayed,
                TotalDamageDealt,
                TotalHealingDone,
                TotalShieldGained,
                resultType == BattleResultType.Victory
                    ? "Run Complete"
                    : "Run Failed",
                resultType == BattleResultType.Victory
                    ? CurrentEncounterIsBoss
                        ? "The breach has been sealed. The atelier survives this siege and its legacy grows stronger."
                        : $"Victory at {EncounterLabel}. Return to the atelier and refine the next forged loadout."
                    : $"The projection collapsed against {Boss?.DisplayName ?? "the enemy"}.",
                resultType == BattleResultType.Victory && CurrentEncounterIsBoss ? "Kindled Start" : string.Empty);
        }

        private void MarkHandDirty()
        {
            HandVersion++;
        }

        private static BattleBossDefinition CreateFallbackBoss(string bossId)
        {
            BattleBossDefinition fallback = ScriptableObject.CreateInstance<BattleBossDefinition>();
            fallback.hideFlags = HideFlags.HideAndDontSave;
            switch (bossId)
            {
                case "enemy.ember.wisp":
                    fallback.Configure(
                        bossId,
                        "Ember Wisp",
                        44,
                        WorkshopElementAttribute.Fire,
                        new[]
                        {
                            CreateBossAction(BattleActionType.Attack, 8, "Spark Jab"),
                            CreateBossAction(BattleActionType.Attack, 10, "Ash Burst"),
                            CreateBossAction(BattleActionType.Defend, 6, "Ember Veil")
                        },
                        "reward.unlock.spell_fusion_basic");
                    break;
                case "enemy.hollow.cleric":
                    fallback.Configure(
                        bossId,
                        "Hollow Cleric",
                        56,
                        WorkshopElementAttribute.Water,
                        new[]
                        {
                            CreateBossAction(BattleActionType.Heal, 8, "Tide Benediction"),
                            CreateBossAction(BattleActionType.Attack, 11, "Mist Lance"),
                            CreateBossAction(BattleActionType.Defend, 8, "Prayer Ward")
                        },
                        "reward.boost.shaping");
                    break;
                case "enemy.glass.knight":
                    fallback.Configure(
                        bossId,
                        "Glass Knight",
                        68,
                        WorkshopElementAttribute.Earth,
                        new[]
                        {
                            CreateBossAction(BattleActionType.Defend, 10, "Mirror Guard"),
                            CreateBossAction(BattleActionType.Attack, 12, "Shard Thrust"),
                            CreateBossAction(BattleActionType.Attack, 14, "Refraction Cleave")
                        },
                        "reward.unlock.spell_fusion_intermediate");
                    break;
                default:
                    fallback.Configure(
                        "boss.earth.golem",
                        "Corrupted Earth Golem",
                        132,
                        WorkshopElementAttribute.Earth,
                        new[]
                        {
                            CreateBossAction(BattleActionType.Attack, 14, "Stone Slam"),
                            CreateBossAction(BattleActionType.Defend, 10, "Rock Guard"),
                            CreateBossAction(BattleActionType.Attack, 18, "Faultline Crush"),
                            CreateBossAction(BattleActionType.Heal, 10, "Earthbound Renewal")
                        },
                        "legacy.kindled_start");
                    break;
            }

            return fallback;
        }

        private static BattleBossAction CreateBossAction(BattleActionType type, int value, string description)
        {
            return new BattleBossAction
            {
                ActionType = type,
                Value = value,
                SecondaryValue = 0f,
                Description = description
            };
        }

        private void OnGUI()
        {
            if (!showFallbackGui || Player == null || Boss == null)
            {
                return;
            }

            const float width = 400f;
            float height = BattleEnded ? 320f : 420f;
            Rect rect = new Rect(16f, 16f, width, height);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Battle");
            GUILayout.Label($"Player: {Player.CurrentHealth}/{Player.MaxHealth} HP  Shield {Player.Shield}");
            GUILayout.Label($"Enemy: {Boss.DisplayName}  {Boss.CurrentHealth}/{Boss.MaxHealth} HP  Shield {Boss.Shield}");
            GUILayout.Label($"Turn {TurnNumber}  Energy {CurrentEnergy}/{EnergyPerTurn}");
            GUILayout.Label($"Draw {DrawPileCount}  Discard {DiscardPileCount}");
            GUILayout.Label($"Intent: {CurrentBossIntent}");
            GUILayout.Space(6f);
            GUILayout.Label(LastActionMessage);
            GUILayout.Space(8f);

            if (!BattleEnded)
            {
                for (int index = 0; index < Cards.Count; index++)
                {
                    WorkshopBattleCardEntry card = Cards[index];
                    string label = $"[{GetCardEnergyCost(card)}] {card.DisplayName} - {DescribeCard(card)}";
                    GUI.enabled = CanPlayCard(index);
                    if (GUILayout.Button(label, GUILayout.Height(36f)))
                    {
                        PlayCard(index);
                    }
                }

                GUI.enabled = !BattleEnded;
                if (GUILayout.Button("End Turn", GUILayout.Height(34f)))
                {
                    EndPlayerTurn();
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label($"Cards played: {CardsPlayed}");
                GUILayout.Label($"Damage dealt: {TotalDamageDealt}");
                GUILayout.Label($"Healing done: {TotalHealingDone}");
                GUILayout.Label($"Shield gained: {TotalShieldGained}");
                if (GUILayout.Button("Return To Workshop", GUILayout.Height(34f)))
                {
                    ReturnToWorkshop();
                }
            }

            GUILayout.EndArea();
            GUI.enabled = true;
        }

        private string DescribeCard(WorkshopBattleCardEntry card)
        {
            BattleResolvedEffect effect = ResolveCard(card);
            int totalValue = effect.PrimaryValue * effect.HitCount;
            switch (effect.Role)
            {
                case WorkshopSpellRole.Defense:
                    return $"Gain {totalValue} shield";
                case WorkshopSpellRole.Healing:
                    return $"Heal {totalValue}";
                default:
                    return $"Deal {totalValue} damage";
            }
        }

    }
}
