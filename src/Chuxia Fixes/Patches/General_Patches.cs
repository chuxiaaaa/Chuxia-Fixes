using GameNetcodeStuff;

using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TMPro;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.ProBuilder;

namespace Patches
{
    [HarmonyWrapSafe]
    public static class General_Patches
    {
        //[ThreadStatic]
        //private static TMP_Text? currentProcessingText;

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.SetArraySizes))]
        //[HarmonyPatch(typeof(TextMeshPro), nameof(TextMeshPro.SetArraySizes))]
        //public static void SetArraySizesPrefix(TextMeshProUGUI __instance)
        //{
        //    if (Plugin.General_DisableFontWarn.Value)
        //    {
        //        currentProcessingText = __instance;
        //    }
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.SetArraySizes))]
        //[HarmonyPatch(typeof(TextMeshPro), nameof(TextMeshPro.SetArraySizes))]
        //public static void SetArraySizesPostfix(TextMeshProUGUI __instance)
        //{
        //    if (Plugin.General_DisableFontWarn.Value)
        //    {
        //        currentProcessingText = null;
        //    }
        //}


        [HarmonyPostfix]
        [HarmonyPatch(typeof(TMP_Settings), nameof(TMP_Settings.warningsDisabled), MethodType.Getter)]
        public static void warningsDisabled(ref bool __result)
        {
            if (Plugin.General_DisableFontWarn.Value)
            {
                //if (currentProcessingText != null)
                //{
                //    var input = currentProcessingText.text;
                //    StringBuilder stringBuilder = new StringBuilder(input.Length);
                //    foreach (char c in input)
                //    {
                //        if (char.IsControl(c))
                //        {
                //            stringBuilder.Append(c);
                //        }
                //        else if (currentProcessingText.font.HasCharacter(c, false, false))
                //        {
                //            stringBuilder.Append(c);
                //        }
                //        else
                //        {
                //            stringBuilder.Append('□');
                //        }
                //    }
                //    currentProcessingText.text = stringBuilder.ToString();
                //}
                __result = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkObject), "GetCachedParent")]
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
        [HarmonyPatch(typeof(SceneEventData), "AddSpawnedNetworkObjects")]
        public static void AddSpawnedNetworkObjects(SceneEventData __instance)
        {
            if (Plugin.General_FixGiftBox.Value)
            {
                if (__instance?.m_NetworkObjectsSync == null) return;
                for (int i = __instance.m_NetworkObjectsSync.Count - 1; i >= 0; i--)
                {
                    var item = __instance.m_NetworkObjectsSync[i];
                    if (item == null) continue;

                    var grab = item.GetComponentInChildren<GrabbableObject>();
                    if (grab is GiftBoxItem gift && gift.hasUsedGift)
                    {
                        Plugin.Log.LogInfo($"[GiftBox] Remove {item.GetInstanceID()}");
                        __instance.m_NetworkObjectsSync.RemoveAt(i);
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "SetShipReadyToLand")]
        private static void ResetHUDManager()
        {
            if (Plugin.General_FixDeathBoxes.Value)
            {
                var hudManager = HUDManager.Instance;
                if (hudManager == null) return;
                hudManager.spectatingPlayerBoxes = new Dictionary<Animator, PlayerControllerB>();
                hudManager.boxesAdded = 0;
            }
        }


    }
}
