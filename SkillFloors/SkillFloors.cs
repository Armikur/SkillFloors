using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using static Skills;

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
        public const string PluginVersion = "1.1.3";

        // Harmony
        private readonly Harmony HarmonyInstance = new Harmony(PluginGUID);

        // Configurable Values
        internal static ConfigEntry<float> Config_Rate;
        internal static ConfigEntry<bool> Config_Debug;

        private void Awake()
        {
            // load configs
            CreateConfigValues();
            Jotunn.Logger.LogInfo(PluginAuthor + "'s " + PluginName + " mod " + PluginVersion + " has loaded!");

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);
        }

        // Configurations ----
        private void CreateConfigValues()
        {
            Config_Rate = Config.Bind("Server Config", "Floor XP Gain Rate", 0.15f, new ConfigDescription("Multiplier for how much XP floor skills gain relative to regular skills (e.g., 0.15 = 15%). Values over 1 will give more XP than normal; helpful for catching up.", new AcceptableValueRange<float>(0f, 1.5f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config_Debug = Config.Bind("Client Config", "Enable Debug Logging", false, new ConfigDescription("Shows each skill floor increase in the BepInEx window."));
        }

        // ------------------------------- MAIN CODE -------------------------------
        // NOTE: Skill Floors are prefixed "floor" and skills are prefixed "skill"
        internal static bool IsReady = false; // is SkillFloors ready?
        public static Dictionary<Skills.SkillType, FloorValues> Floors_Book = new(); // Dictionary<Skills.SkillType, FloorValues>(); // this Dictionary stores SkillFloors' data (skill, (skill level, xp progress) )

        public static void ResetFloorsBook() // reset skillFloor data, prevents bleed when swapping characters
        {
            Floors_Book = new Dictionary<Skills.SkillType, FloorValues>();
        }

        public static void UpdateFloor(Skills.Skill skill, float skillXPGain)
        {
            var skillType = skill.m_info.m_skill; // type of skill (run, jump, etc)

            // if Floor doesn't exist, create it
            if (!Floors_Book.TryGetValue(skillType, out var floorVals))
            {
                floorVals = new FloorValues();
                Floors_Book[skillType] = floorVals;
            }

            // used either way
            float floorReqXP = CalcFloorReqXP(floorVals.Level);
            float skillReqXP = skill.GetNextLevelRequirement();

            // if floor >= skill, clamp and return
            float skillLevelInt = Mathf.Floor(skill.m_level); // skill level, as integer
            if (floorVals.Level >= skillLevelInt)
            {
                floorVals.Level = skillLevelInt; // restrict floor level
                floorVals.XP = 0f; // no xp gain when floor = skill
                if (Config_Debug.Value) Jotunn.Logger.LogInfo($"[SkillFloors] {skillType} floor clamped at {floorVals.Level} ({floorVals.XP}/{floorReqXP}) | Skill: {skill.m_level} ({skill.m_accumulator}/{skillReqXP})");
                return;
            }

            floorVals.XP += skillXPGain * Config_Rate.Value; // increase floor XP (skill rate * configed floor rate)

            // increase floor level if needed
            if (floorVals.XP >= floorReqXP)
            {
                floorVals.Level += 1f;
                floorVals.XP = 0f;
                FloorGainNotify(skillType, floorVals.Level);
                Jotunn.Logger.LogInfo($"[SkillFloors] {skillType} floor increased to {floorVals.Level}!! | Skill: {skill.m_level} ({skill.m_accumulator}/{skillReqXP} )"); // always log
            }
            if (Config_Debug.Value) Jotunn.Logger.LogInfo($"[SkillFloors] {skillType} Floor: {floorVals.Level} ({floorVals.XP}/{floorReqXP}) | Skill: {skill.m_level} ({skill.m_accumulator}/{skillReqXP})");
        }

        public static float GetFloorLevel(Skills.SkillType type)
        {
            return Floors_Book.TryGetValue(type, out var data) ? data.Level : 0f;
        }

        public static float CalcFloorReqXP(float curFloorLevel)
        {
            return Mathf.Pow(Mathf.Floor(curFloorLevel + 1f), 1.5f) * 0.5f + 0.5f; // copied from Valheim calc but using Floor level
        }

        private static void FloorGainNotify(Skills.SkillType skillType, float floorLevel)
        {
            if (MessageHud.instance == null) return;

            string skillName = Localization.instance.Localize("$skill_" + skillType.ToString().ToLower()); // localize the name
            string msg = $"<color=#7D9FB8>{skillName} floor increased to {floorLevel}</color>";
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
            // MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, msg);
        }
    }

    public class FloorValues
    {
        public float Level = 0f;
        public float XP = 0f;
    }

    /* --------------------------- ON SKILL RAISED ----------------------------------- */
    [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
    public class SkillFloors_Patch_SkillFloor_Raise
    {
        static void Postfix(Skills.Skill __instance, float factor)
        {
            float baseStep = __instance.m_info.m_increseStep;
            float globalRate = Game.m_skillGainRate;
            float actualXPGained = baseStep * factor * globalRate;

            SkillFloors.UpdateFloor(__instance, actualXPGained);
        }
    }

    /* --------------------------- ON PLAYER DEATH ----------------------------------- */
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public class Patch_Player_OnDeath
    {
        /* static void Prefix(Player __instance)
        {
            SaveData.Save_Floors(__instance);
        }*/
        static void Postfix(Player __instance)
        {
            var skills = __instance.GetSkills().GetSkillList();
            foreach (var skill in skills)
            {
                var type = skill.m_info.m_skill;
                float floorLevel = SkillFloors.GetFloorLevel(type);

                if (skill.m_level < floorLevel)
                {
                    skill.m_level = floorLevel;
                    Jotunn.Logger.LogInfo($"[SkillFloors] {type} hit its SkillFloor. Holding the line (I mean floor!) at level {floorLevel}!.");
                }
            }
        }
    }

    /* --------------------------- SKILLS PANEL GUI ----------------------------------- */
    [HarmonyPatch(typeof(SkillsDialog), "Setup")]
    public class Patch_SkillsDialog
    {
        static void Postfix(SkillsDialog __instance, Player player)
        {
            var skills = player.GetSkills().GetSkillList(); // get list of player skills
            // loop through each skill and add its level and progress to the skills panel
            for (int i = 0; i < skills.Count && i < __instance.m_elements.Count; i++)
            {
                var skillType = skills[i].m_info.m_skill;
                var floorLevel = SkillFloors.GetFloorLevel(skillType);
                var element = __instance.m_elements[i];

                // Update name text with floor level
                var nameText = Utils.FindChild(element.transform, "name").GetComponent<TMP_Text>();
                if (nameText != null && floorLevel > 0)
                {
                    string localizedName = Localization.instance.Localize("$skill_" + skillType.ToString().ToLower());
                    nameText.text = $"{localizedName} <color=#7D9FB8><size=85%>{Mathf.FloorToInt(floorLevel)}</size></color>";
                }

                // Add or update floor progress bar
                if (SkillFloors.Floors_Book.TryGetValue(skillType, out var floorData))
                {
                    var originalBar = Utils.FindChild(element.transform, "currentlevel").GetComponent<GuiBar>();
                    if (originalBar != null)
                    {
                        var existingFloorBar = Utils.FindChild(element.transform, "floorlevel")?.GetComponent<GuiBar>();
                        GuiBar floorBar;
                        if (existingFloorBar != null)
                        {
                            floorBar = existingFloorBar;
                        }
                        else
                        {
                            var floorBarGO = GameObject.Instantiate(originalBar.gameObject, originalBar.transform.parent);
                            floorBarGO.name = "floorlevel";
                            floorBar = floorBarGO.GetComponent<GuiBar>();

                            var rt = floorBarGO.GetComponent<RectTransform>();
                            rt.anchoredPosition -= new Vector2(0f, 2f); // move x, y
                            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 2f); // set height
                        }

                        float requiredXP = SkillFloors.CalcFloorReqXP(floorData.Level);
                        float progress = Mathf.Clamp01(floorData.XP / requiredXP);
                        floorBar.SetValue(progress);

                        floorBar.SetColor(new Color(0.66f, 0.76f, 0.84f, 1f)); // #A9C3D6
                    }
                }
            }
        }
    }

    /* --------------------------- SAVE AND LOAD LOGIC ----------------------------------- */
    /*
    public static class SaveData_Old
    {
        private const string SaveDataKey = "SkillFloors_SkillFloorData_JSON";

        public static void Save_Floors(Player player)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                Formatting = Formatting.None
            };

            string json = JsonConvert.SerializeObject(SkillFloors.Floors_Book, settings);
            player.m_customData[SaveDataKey] = json;
        }

        public static void Load_Floors(Player player)
        {
            if (!player.m_customData.TryGetValue(SaveDataKey, out string json)) return;

            var settings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new StringEnumConverter() } };

            var deserialized = JsonConvert.DeserializeObject<Dictionary<Skills.SkillType, FloorValues>>(json, settings);
            if (deserialized != null)
            {
                SkillFloors.Floors_Book = deserialized;
            }
        }
    }
    */
    public static class SaveData
    {
        private const string SaveDataKey = "SkillFloors_Data"; // now a ZPKG
        private const string SaveDataKey_OLDJSON = "SkillFloors_SkillFloorData_JSON"; // old version. remove JSON support in next major release.
        private const int SaveVersion = 1;

        public static void Save_Floors(Player player)
        {
            if (player == null) return;

            ZPackage pkg = new ZPackage();

            pkg.Write(SaveVersion);
            pkg.Write(SkillFloors.Floors_Book.Count); // number of entries

            foreach (var kvp in SkillFloors.Floors_Book)
            {
                pkg.Write((int)kvp.Key);        // SkillType
                pkg.Write(kvp.Value.Level);     // Floor level
                pkg.Write(kvp.Value.XP);        // Floor XP
            }

            player.m_customData[SaveDataKey] = pkg.GetBase64();
            Jotunn.Logger.LogInfo("[SkillFloors] Saved floors data.");
        }

        public static void Load_Floors(Player player)
        {
            if (player == null) return;

            // Use Z Package (new system) if there...
            if (player.m_customData.TryGetValue(SaveDataKey, out string base64))
            {
                ZPackage pkg = new ZPackage(base64);

                int version = pkg.ReadInt();
                if (version != SaveVersion)
                {
                    Jotunn.Logger.LogWarning($"[SkillFloors] Unsupported save version {version}, ignoring data");
                    return;
                }

                int count = pkg.ReadInt();

                SkillFloors.Floors_Book.Clear();

                for (int i = 0; i < count; i++)
                {
                    Skills.SkillType type = (Skills.SkillType)pkg.ReadInt();
                    float level = pkg.ReadSingle();
                    float xp = pkg.ReadSingle();

                    SkillFloors.Floors_Book[type] = new FloorValues
                    {
                        Level = level,
                        XP = xp
                    };
                }
                Jotunn.Logger.LogInfo("[SkillFloors] Loaded floors data");
                return;
            }

            // No? Try JSON (migrate)
            if (MigrateFromJsonSave(player)) return;

            // Still no?
            Jotunn.Logger.LogInfo("[SkillFloors] No floors data found");
            SkillFloors.Floors_Book.Clear();
        }

        private static bool MigrateFromJsonSave(Player player)
        {
            if (!player.m_customData.TryGetValue(SaveDataKey_OLDJSON, out string json)) return false;

            try
            {
                var settings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new StringEnumConverter() } };

                var oldJsonData = JsonConvert.DeserializeObject<Dictionary<Skills.SkillType, FloorValues>>(json, settings);

                if (oldJsonData == null || oldJsonData.Count == 0) return false;

                SkillFloors.Floors_Book.Clear();

                foreach (var kvp in oldJsonData)
                {
                    SkillFloors.Floors_Book[kvp.Key] = new FloorValues
                    {
                        Level = kvp.Value.Level,
                        XP = kvp.Value.XP
                    };
                }

                Save_Floors(player); // save in new format
                player.m_customData.Remove(SaveDataKey_OLDJSON); // kill old json data
                Jotunn.Logger.LogInfo("[SkillFloors] Data migrated from JSON to ZPackage");
                return true;
            }
            catch (Exception ex)
            { 
                Jotunn.Logger.LogError($"[SkillFloors] JSON migration failed:\n {ex}");
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    public class Patch_Player_Save
    {
        static void Prefix(Player __instance)
        {
            SaveData.Save_Floors(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Load))]
    public class Patch_Player_Load
    {
        static void Prefix() // reset book before loading
        { 
            SkillFloors.ResetFloorsBook();
            SkillFloors.IsReady = false;
        }
        static void Postfix(Player __instance)
        {
            SaveData.Load_Floors(__instance);
            // SkillFloors.InitFloors(__instance);
            SkillFloors.IsReady = true;
        }
    }
}