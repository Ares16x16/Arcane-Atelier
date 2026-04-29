using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        private const string WorkshopSceneName = "WorkshopScene";

        [SerializeField] private BattleContentDatabase contentDatabase;
        [SerializeField] private string startingBossId = "boss.earth.golem";
        [SerializeField] private int playerMaxHealth = 100;
        [SerializeField] private int playerEnergyPerTurn = 3;
        [SerializeField] private bool useEmergencyDeckWhenEmpty = true;
        [SerializeField] private bool showFallbackGui = true;

        public BattleUnit Player { get; private set; }
        public BattleUnit Boss { get; private set; }
        public BattleBossDefinition CurrentBossDefinition { get; private set; }
        public List<WorkshopBattleCardEntry> Cards { get; private set; } = new List<WorkshopBattleCardEntry>();
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

        private int bossActionIndex;

        private void Awake()
        {
            InitializePlayer();
            LoadWorkshopPayload();
            InitializeBoss();
            EnsurePlayableDeck();
            BeginPlayerTurn();
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
                Cards = payload.Cards ?? new List<WorkshopBattleCardEntry>();

                Debug.Log($"BattleScene: loaded {payload.Cards.Count} card type(s) from workshop.");
                foreach (WorkshopBattleCardEntry card in payload.Cards)
                {
                    Debug.Log($"  - {card.DisplayName} x{card.Amount} ({card.Role}, {card.Element})");
                }
            }
            else
            {
                Debug.LogWarning("BattleScene: no workshop payload found. Running with empty deck.");
            }
        }

        private void InitializeBoss()
        {
            CurrentBossDefinition = contentDatabase != null ? contentDatabase.FindBoss(startingBossId) : null;
            if (CurrentBossDefinition == null)
            {
                Debug.LogWarning($"BattleSceneController: boss '{startingBossId}' not found. Using runtime fallback boss.");
                CurrentBossDefinition = CreateFallbackBoss();
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
                && CurrentEnergy > 0
                && cardIndex >= 0
                && cardIndex < Cards.Count
                && Cards[cardIndex].Amount > 0;
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
            CurrentEnergy--;
            CardsPlayed++;

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
            SceneManager.LoadScene(WorkshopSceneName);
        }

        public string GetCardSummary(WorkshopBattleCardEntry card)
        {
            return DescribeCard(card);
        }

        private void BeginPlayerTurn()
        {
            CurrentEnergy = Mathf.Max(1, playerEnergyPerTurn);
            if (!BattleEnded)
            {
                LastActionMessage = $"Turn {TurnNumber}. Enemy intent: {CurrentBossIntent}";
            }
        }

        private void EnsurePlayableDeck()
        {
            if (!useEmergencyDeckWhenEmpty || Cards.Count > 0)
            {
                return;
            }

            Cards.Add(CreateEmergencyCard("combat.emergency.arcane_bolt", "Arcane Bolt", WorkshopSpellRole.Attack, 8, 1, WorkshopElementAttribute.None));
            Cards.Add(CreateEmergencyCard("combat.emergency.guard_thread", "Guard Thread", WorkshopSpellRole.Defense, 8, 1, WorkshopElementAttribute.None));
            HandVersion++;
            LastActionMessage = "No workshop payload found. Emergency cards supplied.";
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
            int totalDamage = 0;
            for (int hit = 0; hit < effect.HitCount; hit++)
            {
                BattleElementRelation relation = BattleElementUtility.GetRelation(effect.Element, Boss.Element);
                int damage = Mathf.Max(1, Mathf.RoundToInt(BattleElementUtility.ApplyMultiplier(effect.PrimaryValue, relation)));
                Boss.TakeDamage(damage);
                totalDamage += damage;
            }

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
        }

        private static BattleBossDefinition CreateFallbackBoss()
        {
            BattleBossDefinition fallback = ScriptableObject.CreateInstance<BattleBossDefinition>();
            fallback.hideFlags = HideFlags.HideAndDontSave;
            fallback.Configure(
                "boss.earth.golem",
                "Earth Golem",
                80,
                WorkshopElementAttribute.Earth,
                new[]
                {
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Attack,
                        Value = 12,
                        SecondaryValue = 0f,
                        Description = "Stone Slam"
                    },
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Defend,
                        Value = 8,
                        SecondaryValue = 0f,
                        Description = "Rock Guard"
                    }
                },
                "reward_unlock_crystal");
            return fallback;
        }

        private void OnGUI()
        {
            if (!showFallbackGui || Player == null || Boss == null)
            {
                return;
            }

            const float width = 360f;
            float height = BattleEnded ? 280f : 340f;
            Rect rect = new Rect(16f, 16f, width, height);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Battle");
            GUILayout.Label($"Player: {Player.CurrentHealth}/{Player.MaxHealth} HP  Shield {Player.Shield}");
            GUILayout.Label($"Enemy: {Boss.DisplayName}  {Boss.CurrentHealth}/{Boss.MaxHealth} HP  Shield {Boss.Shield}");
            GUILayout.Label($"Turn {TurnNumber}  Energy {CurrentEnergy}/{playerEnergyPerTurn}");
            GUILayout.Label($"Intent: {CurrentBossIntent}");
            GUILayout.Space(6f);
            GUILayout.Label(LastActionMessage);
            GUILayout.Space(8f);

            if (!BattleEnded)
            {
                for (int index = 0; index < Cards.Count; index++)
                {
                    WorkshopBattleCardEntry card = Cards[index];
                    string label = $"{card.DisplayName} x{card.Amount} - {DescribeCard(card)}";
                    GUI.enabled = CanPlayCard(index);
                    if (GUILayout.Button(label, GUILayout.Height(30f)))
                    {
                        PlayCard(index);
                    }
                }

                GUI.enabled = !BattleEnded;
                if (GUILayout.Button("End Turn", GUILayout.Height(30f)))
                {
                    EndPlayerTurn();
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label($"Cards played: {CardsPlayed}");
                GUILayout.Label($"Damage dealt: {TotalDamageDealt}");
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
            switch (effect.Role)
            {
                case WorkshopSpellRole.Defense:
                    return $"Gain {effect.PrimaryValue * effect.HitCount} shield";
                case WorkshopSpellRole.Healing:
                    return $"Heal {effect.PrimaryValue * effect.HitCount}";
                default:
                    return $"Deal {effect.PrimaryValue * effect.HitCount} damage";
            }
        }

    }
}
