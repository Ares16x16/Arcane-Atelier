using ArcaneAtelier.Workshop;

namespace ArcaneAtelier.Battle
{
    public interface IBattleEffectCommand
    {
        BattleActionResolution Execute(BattleUnit caster, BattleUnit primaryTarget, BattleUnit resolvedTarget, BattleEffectInstruction instruction, WorkshopElementAttribute element);
    }
}
