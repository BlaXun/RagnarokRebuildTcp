using RebuildSharedData.Enum.EntityStats;
using RebuildSharedData.Enum;
using RebuildSharedData.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Ninja
{
    [SkillHandler(CharacterSkill.FreezingSpear, SkillClass.Magic)]
    public class FreezingSpearHandler : SkillHandlerBase
    {
        public CharacterSkill GetSkill() {
            return CharacterSkill.FreezingSpear;
        }

        public AttackElement GetElement() {
            return AttackElement.Water;
        }

        public override float GetCastTime(CombatEntity source, CombatEntity? target, Position position, int lvl)
        {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            return 0.7f * lvl;
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl,
            bool isIndirect, bool isItemSource)
        {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            if (target == null || !target.IsValidTarget(source))
                return;

            var res = source.CalculateCombatResult(target, 1, lvl, AttackFlags.Magical, GetSkill(), GetElement());
            res.IsIndirect = isIndirect;

            if (!isIndirect)
            {
                // Has no aftercast delay <3
                //source.ApplyAfterCastDelay(0.7f, ref res);
                //source.ApplyCooldownForAttackAction(target);
            }

            source.ExecuteCombatResult(res, false);

            // Start applying damage shortly after the attack animation was executed (0.6s later)
            res.Time = Time.ElapsedTimeFloat + 0.6f;

            var ch = source.Character;
            CommandBuilder.SkillExecuteTargetedSkillAutoVis(source.Character, target.Character, GetSkill(), lvl, res);
        }
    }
}
