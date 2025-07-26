using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SkillFloors
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    
    internal class SkillFloors : BaseUnityPlugin
    {
        public const string PluginName = "SkillFloors";
        internal const string PluginAuthor = "Armikur";
        public const string PluginGUID = $"{PluginAuthor}.mod.Valheim.{PluginName}";
        public const string PluginVersion = "0.0.5";

        private readonly Harmony HarmonyInstance = new Harmony(PluginGUID);

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Armikur's SkillFloors mod " + PluginVersion + " has loaded!");

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }

        // MAIN CODE ----
        public static Dictionary<Skills.SkillType, SkillFloors_SkillFloorData> SkillFloors_SkillFloorDictionary = new Dictionary<Skills.SkillType, SkillFloors_SkillFloorData>();

        public static void SkillFloors_GainFloorXP(Skills.Skill skill, float xpGained)
        {
            var type = skill.m_info.m_skill;

            if (!SkillFloors_SkillFloorDictionary.ContainsKey(type))
                SkillFloors_SkillFloorDictionary[type] = new SkillFloors_SkillFloorData();

            SkillFloors_SkillFloorData floorData = SkillFloors_SkillFloorDictionary[type];

            // reduce floor skill XP gain by % of regular skill XP gain
            float floorXPGainRate = 0.15f;
            float floorXPGained = xpGained * floorXPGainRate;
            floorData.SkillFloors_Floor_XPProgress += floorXPGained;

            float requiredXP = SkillFloors_CalculateRequiredFloorXP(floorData.SkillFloors_Floor_Level);

            if (floorData.SkillFloors_Floor_XPProgress >= requiredXP)
            {
                floorData.SkillFloors_Floor_Level += 1f;
                floorData.SkillFloors_Floor_XPProgress = 0f;
                Jotunn.Logger.LogInfo($"[SkillFloor] ---- {type} floor level increased to {floorData.SkillFloors_Floor_Level} ----");
            }

            float skillLevel = skill.m_level;
            float skillProgress = skill.m_accumulator;
            float skillRequiredXP = skill.GetNextLevelRequirement();

            Jotunn.Logger.LogInfo($"[SkillFloor] {type} Floor: {floorData.SkillFloors_Floor_Level} ({floorData.SkillFloors_Floor_XPProgress}/{requiredXP} | Skill: {skillLevel} ({skillProgress}/{skillRequiredXP})");
        }

        public static float SkillFloors_GetSkillFloor(Skills.SkillType type)
        {
            return SkillFloors_SkillFloorDictionary.TryGetValue(type, out var data) ? data.SkillFloors_Floor_Level : 0f;
        }

        private static float SkillFloors_CalculateRequiredFloorXP(float currentFloorLevel)
        {
            return Mathf.Pow(Mathf.Floor(currentFloorLevel + 1f), 1.5f) * 0.5f + 0.5f; // copied from Valheim calc but using Floor level
        }
    }

    public class SkillFloors_SkillFloorData
    {
        public float SkillFloors_Floor_Level = 0f;
        public float SkillFloors_Floor_XPProgress = 0f;
    }

    [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
    public class SkillFloors_Patch_SkillFloor_Raise
    {
        static void Postfix(Skills.Skill __instance, float factor)
        {
            float baseStep = __instance.m_info.m_increseStep;
            float globalRate = Game.m_skillGainRate;
            float actualXPGained = baseStep * factor * globalRate;

            SkillFloors.SkillFloors_GainFloorXP(__instance, actualXPGained);
        }
    }
}