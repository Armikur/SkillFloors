using HarmonyLib;
using UnityEngine;

namespace SkillFloors
{
    [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
    public class SkillFloors_Patch_Skills_LowerAllSkills
    {
        static void Postfix(Skills __instance)
        {

            foreach (var skill in __instance.m_skills)
            {
                var type = skill.m_info.m_skill;
                float floor = SkillFloors.SkillFloors_GetSkillFloor(type);
                if (skill.m_level < floor)
                {
                    skill.m_level = floor;
                    skill.m_accumulator = 0f; // Optional: reset XP progress
                    Debug.Log($"[SkillFloor] Prevented {type} from dropping below floor level {floor}");
                }
            }
        }
    }
    
}