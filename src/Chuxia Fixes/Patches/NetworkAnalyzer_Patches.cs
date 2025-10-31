using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ChuxiaFixes.Patches
{

    [HarmonyPatch]
    [HarmonyWrapSafe]
    public static class NetworkAnalyzer_Patches
    {
        public static bool hasPatch { get; set; }

        public static MethodBase TargetMethod()
        {
            var type = Type.GetType("Unity.Netcode.NetworkMetrics, Unity.Netcode.Runtime");
            foreach (var item in type.GetConstructors())
            {
                return item;
            }
            return null;
        }

        static void Postfix(object __instance)
        {
            if (hasPatch)
            {
                return;
            }
            hasPatch = true;
            var harmony = new Harmony("chuxia.NetworkMetricsFix");
            foreach (var item in AccessTools.GetDeclaredMethods(__instance.GetType()))
            {
                if (item.ReturnType == typeof(void))
                {
                    harmony.Patch(item, transpiler: new HarmonyMethod(typeof(NetworkAnalyzer_Patches), nameof(EmptyMethodTranspiler)));
                }
            }
        }

        public static IEnumerable<CodeInstruction> EmptyMethodTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
