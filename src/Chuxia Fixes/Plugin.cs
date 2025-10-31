using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using ChuxiaFixes.Patches;

using HarmonyLib;

using Patches;

using TMPro;


[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log = null!;

    public static ConfigEntry<bool>? FixPlayerName_Enable { get; set; }
    public static ConfigEntry<float>? FixPlayerName_WorkInterval { get; set; }

    public static ConfigEntry<bool>? DisableFontWarn { get; set; }
    public static ConfigEntry<bool>? DisableNetworkAnalyzer { get; set; }

    public static ConfigEntry<bool>? FixDeathBoxes { get; set; }

    private void Awake()
    {

        Log = Logger;
        DisableFontWarn = Config.Bind("General", "DisableFontWarn", true, "Disable unknow font warning messages in the console.");
        DisableNetworkAnalyzer = Config.Bind("General", "DisableNetworkAnalyzer", true, "Disable the built-in network analyzer that cause performance issues.");
        FixDeathBoxes = Config.Bind("General", "FixDeathBoxes", true, "Fixed the issue where names and avatars were displayed incorrectly in spectator mode.");
        FixPlayerName_Enable = Config.Bind("FixPlayerName", "Enable", true, "Fixed incorrect or unknown player names.");
        FixPlayerName_WorkInterval = Config.Bind("FixPlayerName", "WorkInterval", 30f, "Interval (in seconds) between each attempt to fix player names.");
        Harmony.CreateAndPatchAll(typeof(FixPlayerName_Patches));
        Harmony.CreateAndPatchAll(typeof(General_Patches));
        if (DisableNetworkAnalyzer.Value)
        {
            Harmony.CreateAndPatchAll(typeof(NetworkAnalyzer_Patches));
        }
        Log.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_NAME} is loaded!");
    }

}
