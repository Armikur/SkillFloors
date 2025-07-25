using HarmonyLib;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SkillFloors
{
    public static class SkillFloors_SaveLoad_JSDN
    {
        private const string SkillFloors_SaveKey = "SkillFloors_SkillFloorData_JSON";

        public static void SkillFloors_Save(Player player)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                Formatting = Formatting.None
            };

            string json = JsonConvert.SerializeObject(SkillFloors.SkillFloors_SkillFloorDictionary, settings);
            player.m_customData[SkillFloors_SaveKey] = json;
        }

        public static void SkillFloors_Load(Player player)
        {
            if (!player.m_customData.TryGetValue(SkillFloors_SaveKey, out string json))
                return;

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            var deserialized = JsonConvert.DeserializeObject<Dictionary<Skills.SkillType, SkillFloors_SkillFloorData>>(json, settings);
            if (deserialized != null)
            {
                SkillFloors.SkillFloors_SkillFloorDictionary = deserialized;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    public class SkillFloors_Patch_Player_Save
    {
        static void Prefix(Player __instance)
        {
            SkillFloors_SaveLoad_JSDN.SkillFloors_Save(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    public class SkillFloors_Patch_Player_Load
    {
        static void Postfix(Player __instance)
        {
            SkillFloors_SaveLoad_JSDN.SkillFloors_Load(__instance);
        }
    }
}