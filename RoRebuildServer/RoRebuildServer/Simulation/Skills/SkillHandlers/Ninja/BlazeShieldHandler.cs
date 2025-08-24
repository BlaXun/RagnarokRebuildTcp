using System.Diagnostics;
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
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Ninja
{
    [SkillHandler(CharacterSkill.BlazeShield, SkillClass.Magic, SkillTarget.Ground)]
    public class BlazeShieldHandler : SkillHandlerBase
    {
        /*public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position,
            int lvl, bool isIndirect, bool isItemSource)
        {
            // TODO: Later on it should remove the earlier one... but how?
            if (source.Character.Type == CharacterType.Player && source.Character.CountEventsOfType("BlazeShieldBaseEvent") >= 1)
                return SkillValidationResult.CannotCreateMore;

            return base.ValidateTarget(source, target, position, lvl, false, false);
        }*/
        
        private float attackMultiplier = 0.5f;
        
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

            Debug.Assert(source.Character.Map != null);
            
            if (target != null)
            {
                position = target.Character.Position;
                if (target.Character.IsMoving)
                {
                    //monsters will lead the player 1 tile with firewall if they're moving in order to block them
                    var forwardPosition = position.AddDirectionToPosition(target.Character.FacingDirection);
                    if (source.Character.Map.WalkData.IsCellWalkable(forwardPosition))
                        position = forwardPosition;
                }
            }
            
            if(!isIndirect)
                source.ApplyCooldownForSupportSkillAction();
            
            var ch = source.Character;
            var map = ch.Map;

            var e = World.Instance.CreateEvent(source.Entity, map, "BlazeShieldBaseEvent", position, lvl, 0, 0, 0, null);
            ch.AttachEvent(e);

            if (!isIndirect)
                CommandBuilder.SkillExecuteAreaTargetedSkillAutoVis(ch, position, CharacterSkill.BlazeShield, lvl);
        }
    }
}

public class BlazeShieldBaseEvent : NpcBehaviorBase
{
    public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
    {
        npc.ValuesInt[0] = param1; //level
        npc.ValuesInt[1] = 20; //duration
        npc.StartTimer(200);

        if (!npc.Owner.TryGet<WorldObject>(out var owner))
        {
            ServerLogger.LogWarning($"Npc {npc.Character} running BlazeShieldBaseEvent init but does not have an owner.");
            return;
        }

        var pos = npc.Character.Position;
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
            
            CreateWallPiece(npc, pos + new Position(x, y), hitCount);
        }

        // Create the field
        Wall(-2,-2);
        Wall(-1,-2);
        Wall(0,-2);
        Wall(1,-2);
        Wall(2,-2);
        Wall(-2,-1);
        Wall(-1,-1);
        Wall(0,-1);
        Wall(1,-1);
        Wall(2,-1);
        Wall(-2,0);
        Wall(-1,0);
        Wall(1,0);
        Wall(2,0);
        Wall(-2,1);
        Wall(-1,1);
        Wall(0,1);
        Wall(1,1);
        Wall(2,1);
        Wall(-2,2);
        Wall(-1,2);
        Wall(0,2);
        Wall(1,2);
        Wall(2,2);
    }

    private void CreateWallPiece(Npc npc, Position pos, int hitCount)
    {
        if(npc.Character.Map!.WalkData.IsCellWalkable(pos))
            npc.CreateEvent("BlazeShieldObjectEvent", pos, hitCount); //param is max hit count
    }

    public override void OnTimer(Npc npc, float lastTime, float newTime)  {
        npc.Character.Events?.ClearInactive();

        if (npc.EventsCount == 0)
        {
            npc.EndEvent();
            return;
        }

        if(newTime > npc.ValuesInt[1])
            npc.EndAllEvents();
    }

    public override EventOwnerDeathResult OnOwnerDeath(Npc npc, CombatEntity owner)
    {
        if (owner.Character.Type == CharacterType.Monster)  {
            npc.EndAllEvents();
            return EventOwnerDeathResult.RemoveEvent;
        }

        return EventOwnerDeathResult.NoAction;
    }
}

public class BlazeShieldObjectEvent : NpcBehaviorBase
{
    public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
    {
        npc.ValuesInt[0] = param1; //hitCount

        npc.RevealAsEffect(NpcEffectType.BlazeShield, "BlazeShield");

        if (!npc.Owner.TryGet<Npc>(out var parent) || !parent.Owner.TryGet<CombatEntity>(out var source))
        {
            ServerLogger.LogWarning($"Failed to init BlazeShield object event as it has no owner or source entity!");
            npc.EndEvent();
            return;
        }

        var targeting = new TargetingInfo()
        {
            Faction = source.Character.Type == CharacterType.Player ? 1 : 0,
            Party = 0,
            IsPvp = false,
            SourceEntity = parent.Owner,
            TargetingType = TargetingType.Enemies
        };

        var aoe = World.Instance.GetNewAreaOfEffect();
        aoe.Init(npc.Character, Area.CreateAroundPoint(npc.Character.Position, 0), AoeType.DamageAoE, targeting, 15f, 0.05f, 0, 0);
        aoe.CheckStayTouching = true;
        aoe.SkillSource = CharacterSkill.BlazeShield;
        
        npc.AreaOfEffect = aoe;
        npc.Character.Map!.CreateAreaOfEffect(aoe);
    }

    public override void OnAoEInteraction(Npc npc, CombatEntity target, AreaOfEffect aoe)
    {
        if (!aoe.TargetingInfo.SourceEntity.TryGet<CombatEntity>(out var src))
            return;

        if (src.Character.Map != npc.Character.Map)
            return;

        if (!target.IsValidTarget(src) || target.IsInSkillDamageCooldown(CharacterSkill.BlazeShield))
            return;

        void DoAttack(float delay = 0f)
        {
            var res = src.CalculateCombatResult(target, 0.5f, 1, AttackFlags.Magical, CharacterSkill.BlazeShield, AttackElement.Fire);
            res.KnockBack = 0;
            //res.AttackPosition = target.Character.Position.AddDirectionToPosition(target.Character.FacingDirection);
            res.AttackMotionTime = delay;
            res.Time = Time.ElapsedTimeFloat + delay;
            res.IsIndirect = true;
            
            CommandBuilder.SkillExecuteIndirectAutoVisibility(npc.Character, target.Character, res);

            npc.ValuesInt[0]--;
            if (npc.ValuesInt[0] <= 0)
                npc.EndEvent();

            // The higher the stronger the hitlock. But 0.05f seem to be the sweetspot?
            var hitLock = 0.075f;
            target.SetSkillDamageCooldown(CharacterSkill.BlazeShield, hitLock); //make it so they can't get hit by firewall again this frame
            src.ExecuteCombatResult(res, false);
        }

        DoAttack();
    }
}

public class NpcLoaderBlazeShieldEvents : INpcLoader
{
    public void Load()
    {
        DataManager.RegisterEvent("BlazeShieldBaseEvent", new BlazeShieldBaseEvent());
        DataManager.RegisterEvent("BlazeShieldObjectEvent", new BlazeShieldObjectEvent());
    }
}
