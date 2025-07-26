using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SkillFloors
{
    [HarmonyPatch(typeof(SkillsDialog), "Setup")]
    public class SkillFloors_Patch_SkillsDialog_Names
    {
        static void Postfix(SkillsDialog __instance, Player player)
        {
            var skills = player.GetSkills().GetSkillList();

            for (int i = 0; i < skills.Count && i < __instance.m_elements.Count; i++)
            {
                var skill = skills[i];
                var skillType = skill.m_info.m_skill;
                var floorLevel = SkillFloors.SkillFloors_GetSkillFloor(skillType);

                if (floorLevel > 0)
                {
                    var element = __instance.m_elements[i];
                    var nameText = Utils.FindChild(element.transform, "name").GetComponent<TMP_Text>();
                    if (nameText != null)
                    {
                        string localizedName = Localization.instance.Localize("$skill_" + skillType.ToString().ToLower());
                        nameText.text = $"{localizedName} <color=#7D9FB8><size=85%>{Mathf.FloorToInt(floorLevel)}</size></color>";
                    }
                }
            }
        }
    }
}
