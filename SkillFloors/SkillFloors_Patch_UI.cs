using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkillFloors
{
    [HarmonyPatch(typeof(SkillsDialog), "SetupSkills")]
    public class SkillFloors_Patch_SkillsDialog_SetupSkills
    {
        static void Postfix(SkillsDialog __instance)
        {
            foreach (Transform skillEntry in __instance.m_skillListRoot)
            {
                Text nameText = skillEntry.Find("name")?.GetComponent<Text>();
                if (nameText == null) continue;

                string skillName = nameText.text;
                if (Enum.TryParse(skillName, out Skills.SkillType skillType))
                {
                    float floorLevel = SkillFloors.SkillFloors_GetSkillFloor(skillType);
                    nameText.text = $"{skillName} (Floor {floorLevel:F1})";
                }
            }
        }
    }
}
