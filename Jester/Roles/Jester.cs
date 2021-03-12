using UnityEngine;
using System.Linq;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;

namespace Jester
{
    public partial class Plugin
    {
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        public static class ExiledText
        {
            public static void Postfix(ExileController __instance, [HarmonyArgument(0)] GameData.Nested_1 exiled)
            {
                CustomRoles.TryGetValue(exiled.FMAAJCIEMEH, out var isJester);
                if (isJester)
                {
                    __instance.ImpostorText.Text = "But they were the Jester.";
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        public static class Exiled
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (JesterWon) return;
                CustomRoles.TryGetValue(__instance.PlayerId, out var isJester);
                if (isJester)
                {
                    JesterWon = true;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class AddFakeTaskText
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (__instance.AmOwner)
                {
                    CustomRoles.TryGetValue(__instance.PlayerId, out var isJester);
                    if (isJester)
                    {
                        var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                        importantTextTask.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
                        importantTextTask.Text = "[F586F5FF]Get voted out to win\r\n[FFFFFFFF]Fake Tasks:";
                        __instance.myTasks.Insert(0, importantTextTask);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        public static class RecomputeTaskCountsPatch
        {
            public static void Prefix(GameData __instance, out List<GameData.Nested_0> __state)
            {
                foreach (var (playerId, isJester) in CustomRoles)
                {
                    if (!isJester) continue;
                    var player = __instance.GetPlayerById(playerId);
                    if (player == null) continue;
                    __state = player.DEPNCDAJFGJ;
                    player.DEPNCDAJFGJ = null;
                    return;
                }

                __state = null;
            }
            public static void Postfix(GameData __instance, List<GameData.Nested_0> __state)
            {
                if (__state is null) return;
                foreach (var (playerId, isJester) in CustomRoles)
                {
                    if (!isJester) continue;
                    __instance.GetPlayerById(playerId).DEPNCDAJFGJ = __state;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
        public static class EndGamePatch
        {
            public static void Prefix()
            {
                if (JesterWon)
                {
                    TempData.winners = JesterRole.GetWinners(CustomRoles);
                }
                else
                {
                    for (var i = 0; i < TempData.winners.Count; i++)
                    {
                        var winner = TempData.winners[i];
                        byte playerId = 255;
                        foreach (var player in GameData.Instance.AllPlayers)
                        {
                            if (player.LNFMCJAPLBH == winner.Name)
                            {
                                playerId = player.FMAAJCIEMEH;
                                break;
                            }
                        }

                        CustomRoles.TryGetValue(playerId, out var isJester);
                        if (isJester)
                        {
                            TempData.winners.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            public static void Postfix(EndGameManager __instance)
            {
                if (JesterWon)
                {
                    __instance.WinText.Color = JesterRole.Color;
                    __instance.BackgroundBar.material.SetColor(Color, JesterRole.Color);
                    JesterWon = false;
                    CustomRoles.Clear();
                }
            }
        }
    }
}