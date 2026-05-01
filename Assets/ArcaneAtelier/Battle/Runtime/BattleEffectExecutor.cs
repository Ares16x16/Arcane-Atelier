using System.Collections.Generic;

namespace ArcaneAtelier.Battle
{
    public static class BattleEffectExecutor
    {
        private static readonly Dictionary<BattleEffectType, IBattleEffectCommand> Commands = new Dictionary<BattleEffectType, IBattleEffectCommand>
        {
            [BattleEffectType.Damage] = new DamageEffectCommand(),
            [BattleEffectType.Heal] = new HealEffectCommand(),
            [BattleEffectType.Shield] = new ShieldEffectCommand(),
            [BattleEffectType.ApplyStatus] = new ApplyStatusEffectCommand()
        };

        public static List<BattleActionResolution> Execute(BattleCardDefinition definition, BattleUnit caster, BattleUnit target)
        {
            List<BattleActionResolution> results = new List<BattleActionResolution>();

            if (definition == null || caster == null || target == null)
            {
                results.Add(new BattleActionResolution(0, 0, 0, "Invalid effect execution context."));
                return results;
            }

            foreach (BattleEffectInstruction instruction in definition.Instructions)
            {
                BattleUnit resolvedTarget = ResolveTarget(caster, target, instruction.Target, instruction.Type);
                if (Commands.TryGetValue(instruction.Type, out IBattleEffectCommand command))
                {
                    results.Add(command.Execute(caster, target, resolvedTarget, instruction, definition.Element));
                }
                else
                {
                    results.Add(new BattleActionResolution(0, 0, 0, $"Unknown effect type: {instruction.Type}"));
                }
            }

            return results;
        }

        private static BattleUnit ResolveTarget(BattleUnit caster, BattleUnit primaryTarget, BattleEffectTarget target, BattleEffectType type)
        {
            BattleEffectTarget resolvedTarget = target;
            if (resolvedTarget == BattleEffectTarget.Auto)
            {
                resolvedTarget = type switch
                {
                    BattleEffectType.Heal => BattleEffectTarget.Self,
                    BattleEffectType.Shield => BattleEffectTarget.Self,
                    BattleEffectType.ApplyStatus => BattleEffectTarget.Opponent,
                    _ => BattleEffectTarget.Opponent
                };
            }

            return resolvedTarget == BattleEffectTarget.Self ? caster : primaryTarget;
        }
    }
}
