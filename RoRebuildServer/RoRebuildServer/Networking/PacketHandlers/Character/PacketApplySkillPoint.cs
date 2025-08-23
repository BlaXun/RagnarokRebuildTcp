﻿using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RebuildSharedData.Networking;
using RoRebuildServer.Data;
using RoRebuildServer.Data.CsvDataTypes;
using RoRebuildServer.EntityComponents;

namespace RoRebuildServer.Networking.PacketHandlers.Character
{
    [ClientPacketHandler(PacketType.ApplySkillPoint)]
    public class PacketApplySkillPoint : IClientPacketHandler
    {
        public void Process(NetworkConnection connection, InboundMessage msg)
        {
            if (!connection.IsConnectedAndInGame || connection.Player == null)
                return;

            var player = connection.Player;
            var skillId = (CharacterSkill)msg.ReadByte();
            var skill = DataManager.SkillData[skillId];

            //do we have enough skill points?
            var points = player.GetData(PlayerStat.SkillPoints);
            if (points <= 0)
            {
                CommandBuilder.ErrorMessage(player, "Insufficient skill points.");
                return;
            }
            
            //do we already know the max level of this skill?
            var knownLevel = player.LearnedSkills.GetValueOrDefault(skillId, 0);
            if (knownLevel >= skill.MaxLevel)
            {
                CommandBuilder.ErrorMessage(player, "Cannot apply more skill points to this skill.");
                return;
            }

            //is the skill available for the player to learn?
            if (!DataManager.SkillTree.TryGetValue(player.GetData(PlayerStat.Job), out var tree))
            {
                CommandBuilder.ErrorMessage(player, "Cannot apply skill points to this skill.");
                return;
            }

            //this needs to be way more robust as it only works with novice -> first jobs
            if (player.JobId != 0 && skillId != CharacterSkill.BasicMastery && skillId != CharacterSkill.FirstAid)
            {
                if (player.MaxLearnedLevelOfSkill(CharacterSkill.BasicMastery) < 8 ||
                    player.MaxLearnedLevelOfSkill(CharacterSkill.FirstAid) < 1)
                {
                    CommandBuilder.ErrorMessage(player, "You must apply your 9 novice skill points before assigning points to your first job.");
                    return;
                }
            }
            
            //does the player meet the requirements for this skill?
            var meetsPrereq = CheckPrereqFromTree(tree, skillId, player);
            if (!meetsPrereq)
            {
                while (tree!.Extends != null)
                {
                    var job = DataManager.JobIdLookup[tree.Extends];
                    tree = DataManager.SkillTree.GetValueOrDefault(job);
                    if (tree != null && CheckPrereqFromTree(tree, skillId, player))
                    {
                        meetsPrereq = true;
                        break;
                    }
                }
            }

            if (!meetsPrereq)
            {
                CommandBuilder.ErrorMessage(player, "You do not meet the requirements to level up this skill.");
                return;
            }
            player.AddSkillToCharacter(skillId, knownLevel + 1);
            player.SetData(PlayerStat.SkillPoints, points - 1);

            //CommandBuilder.ApplySkillPoint(player, skillId);
            player.RefreshWeaponMastery();
            player.UpdateStats(true, true);
        }
        
        private bool CheckPrereqFromTree(PlayerSkillTree tree, CharacterSkill skill, Player player)
        {
            if (tree.SkillTree == null || !tree.SkillTree.TryGetValue(skill, out var prereqs))
                return false;

            if(prereqs == null) return true;
            for (var i = 0; i < prereqs.Count; i++)
            {
                var prereq = prereqs[i];
                if (!player.LearnedSkills.TryGetValue(prereq.Skill, out var learned) || learned < prereq.RequiredLevel)
                    return false;
            }

            return true;
        }
    }
}
