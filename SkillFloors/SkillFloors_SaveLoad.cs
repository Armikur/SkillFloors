using HarmonyLib;
using System.Collections.Generic;
using Jotunn.Utils;
using SimpleJSON;

namespace SkillFloors
{
    public static class SkillFloors_SaveLoad
    {
        private const string SkillFloors_SaveKey = "SkillFloors_SkillFloorData_JSON";

        public static void SkillFloors_Save(Player player)
        {
            var root = new JSONObject();
            foreach (var kvp in SkillFloors.SkillFloors_SkillFloorDictionary)
            {
                var skillType = kvp.Key.ToString();
                var data = kvp.Value;

                var skillJson = new JSONObject();
                skillJson["FloorLevel"] = data.SkillFloors_Floor_Level;
                skillJson["FloorXPProgress"] = data.SkillFloors_Floor_XPProgress;

                root[skillType] = skillJson;
            }

            player.m_customData[SkillFloors_SaveKey] = root.ToString();
        }

        public static void SkillFloors_Load(Player player)
        {
            if (!player.m_customData.TryGetValue(SkillFloors_SaveKey, out string json))
                return;

            var root = JSON.Parse(json);
            if (root == null || !root.IsObject)
                return;

            var dict = new Dictionary<Skills.SkillType, SkillFloors_SkillFloorData>();

            foreach (var kvp in root.AsObject)
            {
                if (!System.Enum.TryParse(kvp.Key, out Skills.SkillType skillType))
                    continue;

                var skillJson = kvp.Value.AsObject;
                var data = new SkillFloors_SkillFloorData
                {
                    SkillFloors_Floor_Level = skillJson["FloorLevel"].AsFloat,
                    SkillFloors_Floor_XPProgress = skillJson["FloorXPProgress"].AsFloat
                };

                dict[skillType] = data;
            }

            SkillFloors.SkillFloors_SkillFloorDictionary = dict;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    public class SkillFloors_Patch_Player_Save
    {
        static void Prefix(Player __instance)
        {
            SkillFloors_SaveLoad.SkillFloors_Save(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    public class SkillFloors_Patch_Player_Load
    {
        static void Postfix(Player __instance)
        {
            SkillFloors_SaveLoad.SkillFloors_Load(__instance);
        }
    }
}
