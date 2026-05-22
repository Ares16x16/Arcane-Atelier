using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleBossAI
    {
        private readonly BattleBossDefinition definition;
        private readonly float externalDamageMultiplier;
        private int currentActionIndex;
        private readonly List<BattleBossAction> attackActions = new List<BattleBossAction>();
        private readonly List<BattleBossAction> defendActions = new List<BattleBossAction>();
        private readonly List<BattleBossAction> healActions = new List<BattleBossAction>();
        private readonly List<BattleBossAction> specialActions = new List<BattleBossAction>();
        private BattleUnit selfUnit;
        private int sustainTurnCounter = 0;
        private int sustainAttackCounter = 0;
        private bool isPhase2 = false;
        private float damageMultiplier = 1.0f;
        private readonly System.Random rng = new System.Random();
        private BattleBossAction plannedEnemyAction;
        private bool hasPlannedEnemyAction;

        public BattleBossAI(BattleBossDefinition bossDefinition, float externalDamageMultiplier = 1f)
        {
            definition = bossDefinition ?? throw new ArgumentNullException(nameof(bossDefinition));
            this.externalDamageMultiplier = Mathf.Max(1f, externalDamageMultiplier);
            currentActionIndex = 0;
            CategorizeActions();
        }

        public string BossId => definition?.BossId ?? "unknown.boss";
        public string BossName => definition?.DisplayName ?? "Unknown Boss";
        public string DefeatRewardId => definition?.DefeatRewardId ?? string.Empty;

        public int CurrentActionIndex => currentActionIndex;

        public int ActionPatternLength => definition?.ActionPattern?.Count ?? 0;

        public void BindUnits(BattleUnit self, BattleUnit opponent)
        {
            selfUnit = self;
        }

        public BattleBossAction PeekNextAction()
        {
            if (definition != null && definition.IsEnemy)
            {
                return EnsurePlannedEnemyAction();
            }

            IReadOnlyList<BattleBossAction> pattern = GetCurrentPhasePattern();
            if (pattern == null || pattern.Count == 0)
            {
                return CreateNoAction();
            }

            return pattern[currentActionIndex % pattern.Count];
        }

        public BattleBossAction ExecuteNextAction()
        {
            if (selfUnit != null && selfUnit.StatusEffectController != null)
            {
                if (selfUnit.StatusEffectController.ConsumeStatus(selfUnit, "Freeze") ||
                    selfUnit.StatusEffectController.ConsumeStatus(selfUnit, "Stun"))
                {
                    ClearPlannedEnemyAction();
                    return CreateNoAction();
                }
            }

            if (definition != null && definition.IsEnemy)
            {
                return ApplyDamageMultiplier(ConsumePlannedEnemyAction());
            }

            CheckPhaseTransition();

            IReadOnlyList<BattleBossAction> pattern = GetCurrentPhasePattern();
            if (pattern == null || pattern.Count == 0)
            {
                return CreateNoAction();
            }

            BattleBossAction rawAction = pattern[currentActionIndex % pattern.Count];
            currentActionIndex = (currentActionIndex + 1) % pattern.Count;
            return ApplyDamageMultiplier(rawAction);
        }

        public void Reset()
        {
            currentActionIndex = 0;
            sustainTurnCounter = 0;
            sustainAttackCounter = 0;
            isPhase2 = false;
            damageMultiplier = 1.0f;
            ClearPlannedEnemyAction();
        }

        private void CategorizeActions()
        {
            attackActions.Clear();
            defendActions.Clear();
            healActions.Clear();
            specialActions.Clear();

            if (definition == null || definition.ActionPattern == null)
            {
                return;
            }

            foreach (BattleBossAction action in definition.ActionPattern)
            {
                if (action == null)
                {
                    continue;
                }

                switch (action.ActionType)
                {
                    case BattleActionType.Attack:
                        attackActions.Add(action);
                        break;
                    case BattleActionType.Defend:
                        defendActions.Add(action);
                        break;
                    case BattleActionType.Heal:
                        healActions.Add(action);
                        break;
                    case BattleActionType.Special:
                        specialActions.Add(action);
                        break;
                }
            }
        }

        private BattleBossAction EnsurePlannedEnemyAction()
        {
            if (!hasPlannedEnemyAction)
            {
                plannedEnemyAction = SelectEnemyAction();
                hasPlannedEnemyAction = true;
            }

            return plannedEnemyAction ?? CreateNoAction();
        }

        private BattleBossAction ConsumePlannedEnemyAction()
        {
            BattleBossAction action = EnsurePlannedEnemyAction();
            ClearPlannedEnemyAction();

            if (action != null && definition.ActionPattern != null && definition.ActionPattern.Count > 0)
            {
                currentActionIndex = (currentActionIndex + 1) % definition.ActionPattern.Count;
                if (definition.EnemyArchetype == BattleEnemyArchetype.Sustain)
                {
                    sustainTurnCounter++;
                }
            }

            return action ?? CreateNoAction();
        }

        private void ClearPlannedEnemyAction()
        {
            plannedEnemyAction = null;
            hasPlannedEnemyAction = false;
        }

        private BattleBossAction SelectEnemyAction()
        {
            BattleBossAction action = definition.EnemyArchetype switch
            {
                BattleEnemyArchetype.Aggressive => SelectAggressiveEnemyAction(),
                BattleEnemyArchetype.Sustain => SelectSustainEnemyAction(),
                BattleEnemyArchetype.Defensive => SelectDefensiveEnemyAction(),
                _ => PeekFallbackAction()
            };

            return action ?? CreateNoAction();
        }

        private BattleBossAction SelectAggressiveEnemyAction()
        {
            if (specialActions.Count > 0 && rng.Next(100) < 20)
            {
                return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
            }

            if (attackActions.Count > 0)
            {
                return GetScaledAction(attackActions, rng.Next(attackActions.Count), 0.85f);
            }

            return PeekFallbackAction();
        }

        private BattleBossAction SelectSustainEnemyAction()
        {
            if (IsLowHealth() && healActions.Count > 0 && rng.Next(100) < 80)
            {
                return CloneAction(healActions[rng.Next(healActions.Count)], 1f);
            }

            if (specialActions.Count > 0 && rng.Next(100) < 20)
            {
                return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
            }

            if (attackActions.Count > 0)
            {
                return GetScaledAction(attackActions, rng.Next(attackActions.Count), 1f);
            }

            return PeekFallbackAction();
        }

        private BattleBossAction SelectDefensiveEnemyAction()
        {
            if (ShouldAddShield() && defendActions.Count > 0)
            {
                return CloneAction(defendActions[rng.Next(defendActions.Count)], 1f);
            }

            if (specialActions.Count > 0 && rng.Next(100) < 20)
            {
                return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
            }

            if (attackActions.Count > 0)
            {
                return GetScaledAction(attackActions, rng.Next(attackActions.Count), 0.95f);
            }

            if (defendActions.Count > 0)
            {
                return CloneAction(defendActions[rng.Next(defendActions.Count)], 1f);
            }

            return PeekFallbackAction();
        }

        private BattleBossAction GetScaledAction(IReadOnlyList<BattleBossAction> actions, int indexSeed, float multiplier)
        {
            if (actions == null || actions.Count == 0)
            {
                return null;
            }

            BattleBossAction template = actions[indexSeed % actions.Count];
            if (template == null)
            {
                return null;
            }

            return CloneAction(template, multiplier);
        }

        private BattleBossAction PeekFallbackAction()
        {
            if (definition == null || definition.ActionPattern == null || definition.ActionPattern.Count == 0)
            {
                return CreateNoAction();
            }

            return definition.ActionPattern[currentActionIndex];
        }

        private static BattleBossAction CloneAction(BattleBossAction source, float valueMultiplier)
        {
            return new BattleBossAction
            {
                ActionType = source.ActionType,
                Value = Mathf.Max(0, Mathf.RoundToInt(source.Value * valueMultiplier)),
                SecondaryValue = source.SecondaryValue,
                Description = source.Description
            };
        }

        private BattleBossAction CreateNoAction()
        {
            return new BattleBossAction
            {
                ActionType = BattleActionType.None,
                Value = 0,
                SecondaryValue = 0f,
                Description = "No action available."
            };
        }

        private bool IsLowHealth()
        {
            if (selfUnit == null || selfUnit.MaxHealth <= 0)
            {
                return false;
            }

            float ratio = selfUnit.CurrentHealth / (float)selfUnit.MaxHealth;
            return ratio <= definition.LowHealthThresholdNormalized;
        }

        private bool ShouldAddShield()
        {
            if (selfUnit == null)
            {
                return defendActions.Count > 0;
            }

            return selfUnit.Shield < definition.DefensiveShieldThreshold;
        }

        private bool ShouldUseBurstTurn()
        {
            int interval = Mathf.Max(1, definition.PreferredBurstTurnInterval);
            return (currentActionIndex + 1) % interval == 0;
        }

        private IReadOnlyList<BattleBossAction> GetCurrentPhasePattern()
        {
            if (isPhase2 && definition?.Phase2ActionPattern != null && definition.Phase2ActionPattern.Count > 0)
            {
                return definition.Phase2ActionPattern;
            }
            return definition?.ActionPattern;
        }

        private void CheckPhaseTransition()
        {
            if (isPhase2 || selfUnit == null || definition == null)
            {
                return;
            }

            if (!definition.IsBoss)
            {
                return;
            }

            float healthPercent = selfUnit.CurrentHealth / (float)selfUnit.MaxHealth;
            if (healthPercent <= definition.PhaseTransitionHealthPercent)
            {
                isPhase2 = true;
                damageMultiplier = 1.5f;
                Debug.Log($"=== {definition.DisplayName} enters rage! All damage increased by 50%. ===");
            }
        }

        private BattleBossAction ApplyDamageMultiplier(BattleBossAction action)
        {
            if (action == null)
            {
                return action;
            }

            float combinedMultiplier = damageMultiplier * externalDamageMultiplier;
            if (Mathf.Approximately(combinedMultiplier, 1f))
            {
                return action;
            }

            if (action.ActionType == BattleActionType.Attack || action.ActionType == BattleActionType.Special)
            {
                return new BattleBossAction
                {
                    ActionType = action.ActionType,
                    Value = Mathf.RoundToInt(action.Value * combinedMultiplier),
                    SecondaryValue = action.SecondaryValue,
                    Description = damageMultiplier > 1f ? action.Description + " (Enraged!)" : action.Description
                };
            }

            return action;
        }
    }
}
