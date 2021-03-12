using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Il2CppSystem.IO;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jester
{
    internal static class GameOptionsPatches
    {
        public enum JesterModes
        {
            Never,
            Maybe,
            Always
        }

        public static JesterModes JesterMode = JesterModes.Never;

        [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
        public static class OnEnablePatchString
        {
            public static bool Prefix(OptionBehaviour __instance)
            {
                var name = __instance.gameObject.name;
                return !(name.EndsWith("(Clone)") || name == "JesterOption");
            }
        }

        [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
        public static class FixedUpdateString
        {
            public static bool Prefix(StringOption __instance)
            {
                return __instance.gameObject.name != "JesterOption";
            }

            public static void Postfix(StringOption __instance)
            {
                var name = __instance.gameObject.name;
                if (name == "JesterOption")
                {
                    if (__instance.oldValue != __instance.Value)
                    {
                        __instance.oldValue = __instance.Value;
                    }
                    __instance.ValueText.Text = ((JesterModes) __instance.Value).ToString();
                }
            }
        }

        private static void StringPostfix(StringOption __instance, int increment)
        {
            if (__instance.gameObject.name != "JesterOption") return;
            __instance.Value = Mathf.Clamp(__instance.Value + increment, 0, __instance.Values.Count - 1);
            JesterMode = (JesterModes) __instance.Value;

            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            localPlayer.RpcSyncSettings(PlayerControl.GameOptions);
        }

        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
        public static class IncreaseString
        {
            public static bool Prefix(StringOption __instance)
            {
                return __instance.gameObject.name != "JesterOption";
            }

            public static void Postfix(StringOption __instance)
            {
                StringPostfix(__instance, 1);
            }
        }

        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
        public static class DecreaseString
        {
            public static bool Prefix(StringOption __instance)
            {
                return __instance.gameObject.name != "JesterOption";
            }

            public static void Postfix(StringOption __instance)
            {
                StringPostfix(__instance, -1);
            }
        }

        [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.OnEnable))]
        public static class AllowMapChanges
        {
            public static void Prefix(GameSettingMenu __instance)
            {
                __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
            }
        }

        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        public static class GameOptionsMenuPatch
        {
            private static void InitializeString(GameOptionsMenu __instance, string name, string title,
                int initialSelection, float y)
            {
                var cloneSource = Object.FindObjectOfType<StringOption>();
                var option = Object.Instantiate(cloneSource.gameObject, __instance.gameObject.transform);
                option.name = name;
                var localPosition = option.transform.localPosition;
                var kv = option.GetComponent<StringOption>();

                kv.TitleText.Text = title;
                kv.Value = initialSelection;
                option.transform.localPosition = new Vector3(localPosition.x, y, localPosition.z);
            }

            public static void Prefix(GameOptionsMenu __instance)
            {
                // Initialize jester
                InitializeString(__instance, "JesterOption", "Jester Enabled", (int) JesterMode, -8.5f);
            }
        }


        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
        public static class ExtendOptionsMenu {
            public static void Postfix(ref GameOptionsMenu __instance) {
                __instance.GetComponentInParent<Scroller>().YBounds.max = 10f;
            }
        }
        
        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_53))]
        public static class Serialize
        {
            public static void Postfix([HarmonyArgument(0)] BinaryWriter writer)
            {
                writer.Write((byte) JesterMode);
            }
        }

        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_13))]
        public static class Deserialize
        {
            public static void Postfix([HarmonyArgument(0)] Il2CppStructArray<byte> bytes)
            {
                JesterMode = (JesterModes) bytes[^1];
            }
        }

        [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Method_26))]
        private static class GameOptionsDataPatch
        {
            public static void Postfix(ref string __result)
            {
                var stringBuilder = new StringBuilder(__result);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"Jester Enabled: {JesterMode}");
                __result = stringBuilder.ToString();
            }
        }
    }
}