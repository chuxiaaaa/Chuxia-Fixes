using GameNetcodeStuff;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using TMPro;

using Unity.Netcode;

using UnityEngine;

namespace Patches
{
    [HarmonyWrapSafe]
    public static class General_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TMP_Settings), nameof(TMP_Settings.warningsDisabled), MethodType.Getter)]
        public static void warningsDisabled(ref bool __result)
        {
            if (Plugin.General_DisableFontWarn.Value)
                __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkObject),"GetCachedParent")]
        public static void GetCachedParent(NetworkObject __instance)
        {
            if (!Plugin.General_FixNetworkObject.Value)
            {
                return;
            }
            if (__instance.m_CachedParent == null)
            {
                __instance.SetCachedParent(__instance.transform.parent);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "SetShipReadyToLand")]
        private static void ResetHUDManager()
        {
            if (!Plugin.General_FixDeathBoxes.Value)
            {
                return;
            }
            var hudManager = HUDManager.Instance;
            if (hudManager == null) return;
            hudManager.spectatingPlayerBoxes = new Dictionary<Animator, PlayerControllerB>();
            hudManager.boxesAdded = 0;
        }
    }
}
