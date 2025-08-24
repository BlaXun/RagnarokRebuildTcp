using RebuildSharedData.Enum.EntityStats;
using RebuildSharedData.Enum;
using RebuildSharedData.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.Util;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.Logging;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Simulation;
using RoRebuildServer.Data;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Ninja
{
    [SkillHandler(CharacterSkill.BlazeShield, SkillClass.Magic, SkillTarget.Ground)]
    public class BlazeShieldHandler : SkillHandlerBase
    {
        private readonly int requiredCatalystItemId = 7521;
        // Calculates and returns the cast time for the skill
        public override float GetCastTime(CombatEntity source, CombatEntity? target, Position position, int lvl) {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            return 6.5f - (0.5f * lvl);
        }
        
        // Returns the skills identifier
        public CharacterSkill GetSkill() {
            return CharacterSkill.BlazeShield;
        }

        // The element used for this skill
        public AttackElement GetElement() {
            return AttackElement.Fire;
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl,
            bool isIndirect, bool isItemSource) {
            if (lvl < 0 || lvl > 10)
                lvl = 10;

            if (target != null)  {
                position = target.Character.Position;
            }
            
            // Blaze Shield checks for catalyst item only after the casting itself is done
            // Make sure to consume the catalyst item, if it can't be consumed we end here
            if (!isIndirect && !isItemSource) {
                if (ConsumeGemstoneForSkillWithFailMessage(source, requiredCatalystItemId) == false)  {
                    return;
                }
            }
            
            // Remove previous fields
            if (source.Character.CountEventsOfType("BlazeShieldBaseEvent") >= 0)  {
                
                var events = source.Character.ListEventsOfType("BlazeShieldBaseEvent");
                foreach (var ev in events) {
                    ev.EndEvent();
                }
            }
            
            // TODO: Check if cooldown is applied
            if(!isIndirect)
                source.ApplyCooldownForSupportSkillAction(1.0f);
            
            var ch = source.Character;
            var map = ch.Map;

            var e = World.Instance.CreateEvent(source.Entity, map, "BlazeShieldBaseEvent", position, lvl, 0, 0, 0, null);
            ch.AttachEvent(e);

            if (!isIndirect)
                CommandBuilder.SkillExecuteAreaTargetedSkillAutoVis(ch, position, CharacterSkill.BlazeShield, lvl);
        }
    }
}

/**
 * The BlazeShieldBaseEvent is creating the actual fire fields
 */
public class BlazeShieldBaseEvent : NpcBehaviorBase {
    public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)  {
        npc.ValuesInt[0] = param1; // Skill-Level
        npc.ValuesInt[1] = 20; // Maximum alive-duration in seconds
        npc.StartTimer(200);

        if (!npc.Owner.TryGet<WorldObject>(out var owner))  {
            ServerLogger.LogWarning($"Npc {npc.Character} running BlazeShieldBaseEvent init but does not have an owner.");
            return;
        }

        var characterPosition = npc.Character.Position;
        void Wall(int x, int y)  {
            
            byte hitCount = 0;
            switch (param1)
            {
                case 1:
                case 2:
                    hitCount = 5;
                    break;
                
                case 3: 
                case 4:
                    hitCount = 6;
                    break;
                
                case 5:
                case 6:
                    hitCount = 7;
                    break;
                
                case 7:
                case 8:
                    hitCount = 8;
                    break;
                
                case 9:
                case 10:
                    hitCount = 9;
                    break;
            }

            var targetPosition = characterPosition + new Position(x, y);
            // Raise the event to create the actual fire object
            if (npc.Character.Map!.WalkData.IsCellWalkable(targetPosition))
                npc.CreateEvent("BlazeShieldObjectEvent", targetPosition , hitCount); //param is max hit count
        }

        // Create the field
        for (var i = -2; i <= 2; i++) {
            for (var j = -2; j <= 2; j++)  {

                if (i == 0 && j == 0)  {
                    continue;
                }

                Wall(i, j);
            }
        }
    }

    // OnTimer is used to despawn/remove the fire-field once it reached its max. duration
    public override void OnTimer(Npc npc, float lastTime, float newTime)  {
        npc.Character.Events?.ClearInactive();

        if (npc.EventsCount == 0) {
            npc.EndEvent();
            return;
        }

        if (newTime > npc.ValuesInt[1]) {
            npc.EndAllEvents();
        }
    }
}

public class BlazeShieldObjectEvent : NpcBehaviorBase {
    public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString) {
        npc.ValuesInt[0] = param1; //hitCount
        npc.RevealAsEffect(NpcEffectType.BlazeShield, "BlazeShield");

        if (!npc.Owner.TryGet<Npc>(out var parent) || !parent.Owner.TryGet<CombatEntity>(out var source)) {
            ServerLogger.LogWarning($"Failed to init BlazeShield object event as it has no owner or source entity!");
            npc.EndEvent();
            return;
        }

        var targeting = new TargetingInfo() {
            Faction = source.Character.Type == CharacterType.Player ? 1 : 0,
            Party = 0,
            IsPvp = false,
            SourceEntity = parent.Owner,
            TargetingType = TargetingType.Enemies
        };

        var aoe = World.Instance.GetNewAreaOfEffect();
        // This needs a low tick rate so the individual ticks can be done 
        aoe.Init(npc.Character, Area.CreateAroundPoint(npc.Character.Position, 0), AoeType.DamageAoE, targeting, 20f, 0.0125f, 0, 0);
        aoe.CheckStayTouching = true; // Wont be able to perform hits if set to false
        aoe.SkillSource = CharacterSkill.BlazeShield;
        
        npc.AreaOfEffect = aoe;
        npc.Character.Map!.CreateAreaOfEffect(aoe);
    }

    public override void OnAoEInteraction(Npc npc, CombatEntity target, AreaOfEffect aoe)  {
        if (!aoe.TargetingInfo.SourceEntity.TryGet<CombatEntity>(out var src))
            return;

        if (src.Character.Map != npc.Character.Map)
            return;

        if (!target.IsValidTarget(src) || target.IsInSkillDamageCooldown(CharacterSkill.BlazeShield))
            return;

        void DoAttack(float delay = 0f) {
            var res = src.CalculateCombatResult(target, 0.5f, 1, AttackFlags.Magical , CharacterSkill.BlazeShield, AttackElement.Fire);
            res.KnockBack = 0;
            res.AttackMotionTime = delay;
            res.Time = Time.ElapsedTimeFloat + delay;
            res.IsIndirect = true;
            
            CommandBuilder.SkillExecuteIndirectAutoVisibility(npc.Character, target.Character, res);

            // Decrease remaining hits count
            npc.ValuesInt[0]--;
            if (npc.ValuesInt[0] <= 0)
                npc.EndEvent();

            // The higher the stronger the hitlock. But 0.05f seem to be the sweetspot?
            var hitLock = 0.05f;
            // Doddler claims its 100ms
            //var hitLock = 0.1f;
            target.SetSkillDamageCooldown(CharacterSkill.BlazeShield, hitLock); //make it so they can't get hit by blazeshield again for 100ms
            src.ExecuteCombatResult(res, false);

            // We are forcing a move lock on the target (this should also hinder stalactics with endure from moving)
            // However, we will not force HitLock bosses
            if (target.GetSpecialType() != CharacterSpecialType.Boss) {
                // Prevent enemy from moving for 100ms
                target.Character.AddMoveLockTime(0.1f);
            }
        }

        DoAttack();
        
        // Add a 2nd hit for boss monsters as they will walk through the field like it was nothing
        // This is supposed to help simulate the behavior from official servers
        if ((target.GetSpecialType() == CharacterSpecialType.Boss && npc.ValuesInt[0] > 0))
            DoAttack(0); 
    }
}

// We need a Loader to get our events registered
public class NpcLoaderBlazeShieldEvents : INpcLoader  {
    public void Load()  {
        DataManager.RegisterEvent("BlazeShieldBaseEvent", new BlazeShieldBaseEvent());
        DataManager.RegisterEvent("BlazeShieldObjectEvent", new BlazeShieldObjectEvent());
    }
}
