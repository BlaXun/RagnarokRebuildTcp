using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectHandlers;
using Assets.Scripts.Effects.EffectHandlers.Skills;
using Assets.Scripts.Network;
using JetBrains.Annotations;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;

/*
 * TODO: Ninja is having a wrong graphic when using the skill?
 * Should be kneeling with hand on ground
 * (According to doddler this can not be solved right now and needs adjustments in the engine)
 *
 * TODO: Make damage be applied immediatly (right now it seems there is a delay)
 * TODO: The 3d effect should fade-out instead of just disappear
 */
namespace Assets.Scripts.SkillHandlers.Handlers
{
    [SkillHandler(CharacterSkill.FreezingSpear, true)]
    public class FreezingSpearHandler : SkillHandlerBase
    {
        public override void OnHitEffect(ServerControllable target, ref AttackResultData attack)
        {
            // Do not show the hit effect that earth spike would usually show
            //target.Messages.SendElementalHitEffect(attack.Src, attack.MotionTime, AttackElement.Water, attack.HitCount);
        }

        public override void StartSkillCasting(ServerControllable src, ServerControllable target, int lvl, float castTime)  {
            
            // Show the casting vfx around the character
            src.AttachEffect(CastEffect.Create(castTime, src.gameObject, AttackElement.Water));

            // SHow the target vfx around the target
            if (target != null)
                target.AttachEffect(CastLockOnEffect.Create(castTime, target.gameObject));
        }

        public override void InterruptSkillCasting(ServerControllable src) {
            src.EndEffectOfType(EffectType.CastEffect);
        }

        public override void ExecuteSkillTargeted([CanBeNull] ServerControllable src, ref AttackResultData attack)  {
            // We are playing the skill motion in fast-mode, but its still not spammable
            src?.PerformSkillMotion(true);
            if (attack.Target != null)
                //IceArrow.Create(src, attack.Target, attack.SkillLevel); //don't attach to the entity so the effect stays if they get removed
                FreezingSpearHitEffect.LaunchFreezingSpearEffect(attack.Target, attack.SkillLevel);
        }
    }
}