using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Skills;

namespace SkillFloors
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]

    internal class SkillFloors : BaseUnityPlugin
    {
        public const string PluginName = "SkillFloors";
        public const string PluginGUID = $"Armikur.mod.Valheim.{PluginName}";
        public const string PluginVersion = "1.2.2";

        // Harmony
        private readonly Harmony HarmonyInstance = new Harmony(PluginGUID);

        // Configurable Values
        internal static ConfigEntry<float> Config_Rate;
        internal static ConfigEntry<bool> Config_Debug;

        private void Awake()
        {
            // load configuration
            CreateConfigValues();
            SFLog.Info("Armikur's " + PluginName + " mod " + PluginVersion + " has loaded!"); // always logged

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
        internal static bool BookIsLoaded = false; // have we alreawdy loaded scene?)
        public static Dictionary<Skills.SkillType, FloorValues> Floors_Book = new(); // Dictionary<Skills.SkillType, FloorValues>(); // this Dictionary stores SkillFloors' data (skill, (skill level, xp progress) )

        public static void FreshFloorsBook() // replace Floors_Book with fresh book
        {
            Floors_Book = new Dictionary<Skills.SkillType, FloorValues>();
            if(Config_Debug.Value) SFLog.Warn("Fresh Floors Book created");
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

            floorVals.XP += skillXPGain * Config_Rate.Value; // increase floor XP (skill rate * configed floor rate)

            // increase floor level if needed. intentionally only increases by 1 level and sets the progress back to 0 instead of using a while loop to "spend" off the XP for levels. If something grants a ton of XP, this will clamp the floor to only increase by 1 regardless.
            if (floorVals.XP >= floorReqXP)
            {
                floorVals.Level += 1f;
                floorVals.XP = 0f;
                FloorGainNotify(skillType, floorVals.Level);
                SFLog.Info($"{skillType} floor increased to {floorVals.Level}!! | Skill: {skill.m_level} ({skill.m_accumulator}/{skillReqXP} )"); // always logged
            }
            if (Config_Debug.Value) SFLog.Info($"{skillType} Floor: {floorVals.Level} ({floorVals.XP}/{floorReqXP}) | Skill: {skill.m_level} ({skill.m_accumulator}/{skillReqXP})");
        }

        public static float GetFloorLevel(Skills.SkillType type) => Floors_Book.TryGetValue(type, out var data) ? data.Level : 0f; // return level or 0
        public static float CalcFloorReqXP(float curFloorLevel) => Mathf.Pow(Mathf.Floor(curFloorLevel + 1f), 1.5f) * 0.5f + 0.5f; // Valheim calc used on Floor level

        private static void FloorGainNotify(Skills.SkillType skillType, float floorLevel) // ingame message on Floor increase
        {
            if (MessageHud.instance == null) return;
            string skillName = Localization.instance.Localize("$skill_" + skillType.ToString().ToLower()); // localize the name
            string msg = $"<color=#7D9FB8>{skillName} floor increased to {floorLevel}</color>";
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
        }
    }

    internal static class SFLog // logging helper
    {
        private const string prefix = "[SkillFloors] ";
        public static void Info(string msg) => Jotunn.Logger.LogInfo(prefix + msg);
        public static void Warn(string msg) => Jotunn.Logger.LogWarning(prefix + msg);
        public static void Err(string msg) => Jotunn.Logger.LogError(prefix + msg);
    }

    public class FloorValues // initial floor level and xp
    {
        public float Level = 0f;
        public float XP = 0f;
    }

    /* --------------------------- ON SKILL RAISED ----------------------------------- */
    [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
    public class Patch_SkillFloor_Raise // raise skill floors
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
    public class Patch_Player_OnDeath // prevent skills from falling below their floors
    {
        static void Postfix(Player __instance)
        {
            var skills = __instance.GetSkills().GetSkillList(); // get current skills list
            foreach (var skill in skills) // check each one against the floor level, clamp if needed
            {
                var type = skill.m_info.m_skill;
                float floorLevel = SkillFloors.GetFloorLevel(type);

                if (skill.m_level < floorLevel)
                {
                    skill.m_level = floorLevel;
                    SFLog.Info($"{type} hit its SkillFloor. Holding the line (I mean floor!) at level {floorLevel}!");
                }
            }
        }
    }

    /* --------------------------- SKILLS PANEL GUI ----------------------------------- */
    [HarmonyPatch(typeof(SkillsDialog), "Setup")]
    public class Patch_SkillsDialog // update GUI
    {
        static void Postfix(SkillsDialog __instance, Player player)
        {
            var skills = player.GetSkills().GetSkillList(); // get current skills list
            // for each skill, add floor level & progress to skills panel
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
    public static class SaveData
        {
        private const string SaveDataKey = "SkillFloors_Data"; // now a ZPkg
        private const int SaveVersion = 1; // increment this if the save format changes in a way that is not backwards compatible
        public static bool IsMainScene() => SceneManager.GetActiveScene().name.Equals("main"); // in world?

        public static void Save_Floors(Player player) // save Floors_Book to ZPackage
        {
            // NOTE: This method is currently only called from Patch_Player_Save, meaning it's already been checked for null/local/in-world player
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
            Log_Book("Floors data saved"); // log info if debug on
        }

        public static void Load_Floors(Player player) // load Floors_Book from ZPackage or old JSON if present. Clear book if neither exist.
        {
            // NOTE: This method is currently only called from Patch_Player_Load, meaning it always starts with a new empty Floors_Book.
            // NOTE: This method is currently only called from Patch_Player_Load, meaning it's already been checked for null/local/in-world player


            // Use Z Package if exists...
            if (player.m_customData.TryGetValue(SaveDataKey, out string base64))
            {
                ZPackage pkg = new ZPackage(base64);

                int version = pkg.ReadInt();
                if (version != SaveVersion)
                {
                    SFLog.Warn($"ZPackage found, unsupported save version {version}, aborting");
                    return;
                }

                int count = pkg.ReadInt();

                // SkillFloors.Floors_Book.Clear(); -- currently redundant
                // if (SkillFloors.Config_Debug.Value) SFLog.Warn("Book cleared, rebuild from ZPackage...");
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

                Log_Book("ZPackage found, Floors data loaded"); // log info if debug on
                return;
            }

            // No floors data?
            // SkillFloors.Floors_Book.Clear(); -- currently redundant
            SFLog.Warn("No Floors data found (normal for first use / new characters)."); // always log
        }

        private static void Log_Book(string headerMsg = null) // log current book entries if debugging enabled.
        {
            if (!SkillFloors.Config_Debug.Value) return; // only continue if debugging is on
            if (!string.IsNullOrWhiteSpace(headerMsg)) SFLog.Warn(headerMsg); // show header message if exists
            var currentBook = SkillFloors.Floors_Book;
            if (currentBook.Count == 0)
            {
                SFLog.Warn("xxxxxx Floors Book Is Empty xxxxxx");
                return;
            }
            SFLog.Warn("****** Current Floors Book ******");
            foreach (var kvp in currentBook)
            {
                SFLog.Info($"** {kvp.Key}: Floor: {kvp.Value.Level}, Floor XP: {kvp.Value.XP}");
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Save))]
    public class Patch_Player_Save
    {
        static void Prefix(Player __instance)
        {
            if (__instance == null || Player.m_localPlayer != __instance)
            {
                if (SkillFloors.Config_Debug.Value) SFLog.Warn("skip save: non-local or null player");
                return;
            }
            if (!SaveData.IsMainScene())
            {
                if (SkillFloors.Config_Debug.Value) SFLog.Warn("skip save: not in-world");
                return;
            }
            SaveData.Save_Floors(__instance);
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class Patch_Player_OnSpawned
    {
        // We previously used Player.Load for this. Not totally convinced this is preferred but it seems to attempt fewer loads outside of play.
        static void Postfix(Player __instance)
        {
            if (__instance == null || Player.m_localPlayer != __instance)
            {
                if (SkillFloors.Config_Debug.Value) SFLog.Warn("skip load: non-local or null player");
                return;
            }
            if (!SaveData.IsMainScene())
            {
                if (SkillFloors.Config_Debug.Value) SFLog.Warn("skip load: not in-world");
                return;
            }
            if (SkillFloors.BookIsLoaded)
            {
                if (SkillFloors.Config_Debug.Value) SFLog.Warn("skip load: only load once per login");
                return;
            }
            if (SkillFloors.Config_Debug.Value) SFLog.Warn("Refreshing Book & loading Floors data");
            SkillFloors.FreshFloorsBook();
            SaveData.Load_Floors(__instance);
            SkillFloors.BookIsLoaded = true;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    public static class Patch_Game_Logout
    {
        static void Prefix()
        {
            SkillFloors.BookIsLoaded = false; // reset loaded state so it's ready for next spawn
            if (SkillFloors.Config_Debug.Value) SFLog.Warn("\"Loaded\" state reset");
        }
    }
}