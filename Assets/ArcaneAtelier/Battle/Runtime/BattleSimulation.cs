using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public enum BattleState
    {
        WaitingForPlayer,
        BossTurnPending,
        ResolvingBoss,
        BattleEnded
    }

    public sealed class BattleSimulation
    {
        public BattleUnit Player { get; }
        public BattleUnit Boss { get; }
        public BattleDeckController Deck { get; }
        public BattleBossAI BossAI { get; }
        public BattleStatusEffectController StatusController { get; }
        public BattleState State { get; private set; }
        public int TurnsElapsed { get; private set; }
        public int ActionPoints { get; private set; }
        public int MaxActionPoints { get; private set; }

        public int TotalDamageDealt { get; private set; }
        public int TotalHealingDone { get; private set; }
        public int TotalShieldGained { get; private set; }
        public int CardsPlayed { get; private set; }

        public event Action<BattleActionResolution> PlayerActionResolved;
        public event Action<BattleActionResolution> BossActionResolved;
        public event Action PlayerTurnSkipped;
        public event Action<int> TurnCycleComplete;
        public event Action<BattleResult> BattleEnded;

        public BattleSimulation(
            BattleUnit player,
            BattleUnit boss,
            BattleDeckController deck,
            BattleBossAI bossAI,
            BattleContentDatabase contentDatabase = null)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            BossAI = bossAI ?? throw new ArgumentNullException(nameof(bossAI));
            StatusController = new BattleStatusEffectController(contentDatabase);
            Player.StatusEffectController = StatusController;
            Boss.StatusEffectController = StatusController;
            State = BattleState.WaitingForPlayer;
            MaxActionPoints = 3;
            ActionPoints = MaxActionPoints;
        }

        public bool TryPlayCard(int handIndex)
        {
            if (State != BattleState.WaitingForPlayer)
            {
                Debug.LogWarning($"BattleSimulation: cannot play card in state '{State}'.");
                return false;
            }

            if (!Deck.TryGetActionPointCost(handIndex, out int apCost))
            {
                return false;
            }

            if (ActionPoints < apCost)
            {
                Debug.LogWarning($"BattleSimulation: not enough AP ({ActionPoints}/{apCost}) to play card.");
                return false;
            }

            if (!Deck.TryPlayCard(handIndex, out BattleResolvedEffect effect))
            {
                return false;
            }

            ActionPoints -= apCost;
            CardsPlayed++;

            if (Deck.LastPlayedDefinition != null)
            {
                // Path A/B: new per-card effect executor
                List<BattleActionResolution> resolutions = BattleEffectExecutor.Execute(Deck.LastPlayedDefinition, Player, Boss);
                PublishPlayerResolutions(resolutions);
            }
            else
            {
                // Path C: fallback template resolver
                BattleActionResolution resolution = BattleActionResolver.ResolvePlayerEffect(effect, Player, Boss);
                PublishPlayerResolutions(new List<BattleActionResolution> { resolution });
            }

            if (CheckBattleEnd())
            {
                return true;
            }

            if (ActionPoints <= 0)
            {
                QueueBossTurn();
            }

            return true;
        }

        public void EndTurn()
        {
            if (State != BattleState.WaitingForPlayer)
            {
                return;
            }

            TickStatusEffects(BattleStatusTrigger.OnTurnEnd, Player);

            Deck.EndTurn();
            PlayerTurnSkipped?.Invoke();

            if (CheckBattleEnd())
            {
                return;
            }

            QueueBossTurn();
        }

        public void AdvancePendingTurn()
        {
            if (State != BattleState.BossTurnPending)
            {
                return;
            }

            ResolveBossTurn();
        }

        private void QueueBossTurn()
        {
            State = BattleState.BossTurnPending;
        }

        private void ResolveBossTurn()
        {
            State = BattleState.ResolvingBoss;

            TickStatusEffects(BattleStatusTrigger.OnTurnStart, Boss);
            if (CheckBattleEnd())
            {
                return;
            }

            BattleBossAction action = BossAI.ExecuteNextAction();
            BattleActionResolution resolution = BattleActionResolver.ResolveBossAction(action, Boss, Player);
            AccumulateStats(resolution);
            BossActionResolved?.Invoke(resolution);

            if (!CheckBattleEnd())
            {
                TickStatusEffects(BattleStatusTrigger.OnTurnEnd, Boss);
                TickStatusEffects(BattleStatusTrigger.OnTurnStart, Player);

                if (CheckBattleEnd())
                {
                    return;
                }

                TurnsElapsed++;
                State = BattleState.WaitingForPlayer;
                ActionPoints = MaxActionPoints;
                TurnCycleComplete?.Invoke(TurnsElapsed);
            }
        }

        private void AccumulateStats(BattleActionResolution resolution)
        {
            TotalDamageDealt += resolution.DamageDealt;
            TotalHealingDone += resolution.HealingDone;
            TotalShieldGained += resolution.ShieldGained;
        }

        private void TickStatusEffects(BattleStatusTrigger trigger, BattleUnit unit)
        {
            if (StatusController == null || unit == null)
            {
                return;
            }

            List<BattleActionResolution> resolutions = StatusController.Tick(trigger, unit);
            foreach (BattleActionResolution resolution in resolutions)
            {
                AccumulateStats(resolution);
                if (unit == Player)
                {
                    PlayerActionResolved?.Invoke(resolution);
                }
                else
                {
                    BossActionResolved?.Invoke(resolution);
                }
            }
        }

        private void PublishPlayerResolutions(List<BattleActionResolution> resolutions)
        {
            foreach (BattleActionResolution resolution in resolutions)
            {
                AccumulateStats(resolution);
                PlayerActionResolved?.Invoke(resolution);
            }
        }

        private bool CheckBattleEnd()
        {
            if (!Boss.IsAlive)
            {
                EndBattle(BattleResultType.Victory);
                return true;
            }

            if (!Player.IsAlive)
            {
                EndBattle(BattleResultType.Defeat);
                return true;
            }

            return false;
        }

        private void EndBattle(BattleResultType resultType)
        {
            State = BattleState.BattleEnded;

            BattleResult result = new BattleResult
            {
                ResultType = resultType,
                BossId = BossAI.BossId,
                BossDisplayName = BossAI.BossName,
                TotalDamageDealt = TotalDamageDealt,
                TotalHealingDone = TotalHealingDone,
                TotalShieldGained = TotalShieldGained,
                CardsPlayed = CardsPlayed,
                TurnsElapsed = TurnsElapsed,
                DefeatRewardId = resultType == BattleResultType.Victory ? BossAI.DefeatRewardId : string.Empty
            };

            BattleEnded?.Invoke(result);
        }
    }
}
