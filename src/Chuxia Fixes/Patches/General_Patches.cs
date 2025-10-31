using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Text;

using TMPro;

namespace Patches
{
    [HarmonyWrapSafe]
    public static class General_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TMP_Settings), nameof(TMP_Settings.warningsDisabled), MethodType.Getter)]
        public static void warningsDisabled(ref bool __result)
        {
            if (Plugin.DisableFontWarn.Value)
                __result = true;
        }
    }
}
