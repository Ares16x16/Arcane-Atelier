using System;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public enum BattleState
    {
        WaitingForPlayer,
        ResolvingBoss,
        BattleEnded
    }

    public sealed class BattleSimulation
    {
        public BattleUnit Player { get; }
        public BattleUnit Boss { get; }
        public BattleDeckController Deck { get; }
        public BattleBossAI BossAI { get; }
        public BattleState State { get; private set; }
        public int TurnsElapsed { get; private set; }

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
            BattleBossAI bossAI)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            BossAI = bossAI ?? throw new ArgumentNullException(nameof(bossAI));
            State = BattleState.WaitingForPlayer;
        }

        public bool TryPlayCard(int handIndex)
        {
            if (State != BattleState.WaitingForPlayer)
            {
                Debug.LogWarning($"BattleSimulation: cannot play card in state '{State}'.");
                return false;
            }

            if (!Deck.TryPlayCard(handIndex, out BattleResolvedEffect effect))
            {
                return false;
            }

            CardsPlayed++;

            BattleActionResolution resolution = BattleActionResolver.ResolvePlayerEffect(effect, Player, Boss);
            AccumulateStats(resolution);
            PlayerActionResolved?.Invoke(resolution);

            if (CheckBattleEnd())
            {
                return true;
            }

            ResolveBossTurn();
            return true;
        }

        public void SkipTurn()
        {
            if (State != BattleState.WaitingForPlayer)
            {
                return;
            }

            Deck.SkipTurn();
            PlayerTurnSkipped?.Invoke();

            if (CheckBattleEnd())
            {
                return;
            }

            ResolveBossTurn();
        }

        private void ResolveBossTurn()
        {
            State = BattleState.ResolvingBoss;

            BattleBossAction action = BossAI.ExecuteNextAction();
            BattleActionResolution resolution = BattleActionResolver.ResolveBossAction(action, Boss, Player);
            AccumulateStats(resolution);
            BossActionResolved?.Invoke(resolution);

            if (!CheckBattleEnd())
            {
                TurnsElapsed++;
                State = BattleState.WaitingForPlayer;
                TurnCycleComplete?.Invoke(TurnsElapsed);
            }
        }

        private void AccumulateStats(BattleActionResolution resolution)
        {
            TotalDamageDealt += resolution.DamageDealt;
            TotalHealingDone += resolution.HealingDone;
            TotalShieldGained += resolution.ShieldGained;
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
