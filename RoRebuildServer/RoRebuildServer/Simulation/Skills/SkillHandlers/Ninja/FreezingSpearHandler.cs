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
        // Returns the skills identifier
        public CharacterSkill GetSkill() {
            return CharacterSkill.FreezingSpear;
        }

        // The element used for this skill
        public AttackElement GetElement() {
            return AttackElement.Water;
        }

        // Calculates and returns the cast time for the skill
        public override float GetCastTime(CombatEntity source, CombatEntity? target, Position position, int lvl) {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            return 0.7f * lvl;
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl,
            bool isIndirect, bool isItemSource) {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            if (target == null || !target.IsValidTarget(source))
                return;
            
            // The skill description states the skill would deal 70% ice magic damage per hit while it actually always was 100%
            var res = source.CalculateCombatResult(target, 0.7f, lvl, AttackFlags.Magical, GetSkill(), GetElement());
            
            // We don't need indirect
            /*  Doddler: You also shouldn't use indirect, that is a flag set if the cast is done
                on behalf of the player (like via auto cast), rather than cast directly.
                It's used to skip playing animation or adding a delay.*/
            
            //res.IsIndirect = isIndirect;
            
            /*if (!isIndirect)
            {
                // Has no aftercast delay <3
                //source.ApplyAfterCastDelay(0.7f, ref res);
                //source.ApplyCooldownForAttackAction(target);
            }*/
            
            //source.ApplyAfterCastDelay(0.0f, ref res);
            //source.ApplyCooldownForAttackAction(target);
            //source.ApplyCooldownForAttackAction(0.0f);

            // Freezing spear deals 3 hits at level 1 and 12 at level 10
            res.HitCount = (byte) (2+lvl);
            res.Time = Time.ElapsedTimeFloat + 0.05f;
            source.ExecuteCombatResult(res, false, true);

            // Start applying damage shortly after the attack animation was executed (0.6s later)
            //res.Time = Time.ElapsedTimeFloat + 0.6f;
            // Apply the damage shortly after the attack is executed
            

            var ch = source.Character;
            CommandBuilder.SkillExecuteTargetedSkillAutoVis(source.Character, target.Character, GetSkill(), lvl, res);
        }
    }
}
