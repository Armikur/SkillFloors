using HarmonyLib;
using UnityEngine;

namespace SkillFloors
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public class SkillFloors_Patch_Player_OnDeath
    {
        static void Postfix(Player __instance)
        {
            var skills = __instance.GetSkills().GetSkillList();
            foreach (var skill in skills)
            {
                var type = skill.m_info.m_skill;
                float floorLevel = SkillFloors.SkillFloors_GetSkillFloor(type);

                if (skill.m_level < floorLevel)
                {
                    Debug.Log($"[SkillFloors] Preventing {type} from dropping below floor level {floorLevel}");
                    skill.m_level = floorLevel;
                }
            }
        }
    }
}
