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

    public static ConfigEntry<bool> FixPlayerName_Enable { get; set; } = null!;
    public static ConfigEntry<float> FixPlayerName_WorkInterval { get; set; } = null!;

    public static ConfigEntry<bool> General_DisableFontWarn { get; set; } = null!;
    public static ConfigEntry<bool> General_DisableNetworkAnalyzer { get; set; } = null!;

    public static ConfigEntry<bool> General_FixDeathBoxes { get; set; } = null!;

    public static ConfigEntry<bool> General_FixNetworkObject { get; set; } = null!;

    private void Awake()
    {
        Log = Logger;
        General_DisableFontWarn = Config.Bind("General", "DisableFontWarn", true, "Disable unknow font warning messages in the console.");
        General_DisableNetworkAnalyzer = Config.Bind("General", "DisableNetworkAnalyzer", true, "Disable the built-in network analyzer that cause performance issues.");
        General_FixNetworkObject = Config.Bind("General", "FixNetworkObject", true, "Fixed the issue where NetworkObject's cached parent was not set correctly.");
        General_FixDeathBoxes = Config.Bind("General", "FixDeathBoxes", true, "Fixed the issue where names and avatars were displayed incorrectly in spectator mode.");
        FixPlayerName_Enable = Config.Bind("FixPlayerName", "Enable", true, "Fixed incorrect or unknown player names.");
        FixPlayerName_WorkInterval = Config.Bind("FixPlayerName", "WorkInterval", 30f, "Interval (in seconds) between each attempt to fix player names.");
        Harmony.CreateAndPatchAll(typeof(FixPlayerName_Patches));
        Harmony.CreateAndPatchAll(typeof(General_Patches));
        if (General_DisableNetworkAnalyzer.Value)
        {
            Harmony.CreateAndPatchAll(typeof(NetworkAnalyzer_Patches));
        }
        Log.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_NAME} is loaded!");
    }

}
