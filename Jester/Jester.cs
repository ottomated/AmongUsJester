using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Reactor;
using System.Collections.Generic;
using UnityEngine;
using UnhollowerBaseLib;
using Hazel;
using System.Linq;

namespace Jester
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    [ReactorPluginSide(PluginSide.ClientOnly)]
    public partial class Plugin : BasePlugin
    {
        public const string Id = "net.ottomated.Jester";

        public static readonly Dictionary<byte, bool> CustomRoles = new Dictionary<byte, bool>();

        public static bool JesterWon = false;
        private static readonly int Color = Shader.PropertyToID("_Color");

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            Harmony.PatchAll();
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class RpcSetInfected
        {
            public static void Postfix(PlayerControl __instance,
                [HarmonyArgument(0)] Il2CppReferenceArray<GameData.Nested_1> infected)
            {
                var playersToGiveRoles = new List<GameData.Nested_1>();
                foreach (var p in GameData.Instance.AllPlayers)
                {
                    if (infected.All(imposter => imposter.FMAAJCIEMEH != p.FMAAJCIEMEH))
                    {
                        playersToGiveRoles.Add(p);
                    }
                }

                var giveRole = false; ;

                switch (GameOptionsPatches.JesterMode)
                {
                    case GameOptionsPatches.JesterModes.Always:
                        giveRole = true;
                        break;
                    case GameOptionsPatches.JesterModes.Maybe:
                        if (HashRandom.Method_1(10000) % 2 == 0)
                        {
                            giveRole = true;
                        }
                        break;
                }

                var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId,
                    (byte) CustomRpcMessage.SetCustomRoles, SendOption.Reliable);

                CustomRoles.Clear();

                messageWriter.Write(giveRole);
                if (giveRole)
                {
                    int index = HashRandom.Method_1(playersToGiveRoles.Count);
                    CustomRoles.Add(playersToGiveRoles[index].ACBLKMFEPKC, true);
                    messageWriter.Write(playersToGiveRoles[index].ACBLKMFEPKC);
                    if (CustomRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                    {
                        var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                        if (role)
                        {
                            __instance.nameText.Color = JesterRole.Color;
                        }
                    }
                }

                messageWriter.EndMessage();

                // foreach (var (key, value) in CustomRoles)
                // {
                //     System.Console.WriteLine(key + ": " + value.Join(null, ", "));
                // }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class PlayerHandleRpc
        {
            public static void Prefix([HarmonyArgument(1)] MessageReader writer, [HarmonyArgument(0)] int callId)
            {
                switch ((CustomRpcMessage) callId)
                {
                    case CustomRpcMessage.SetCustomRoles: // Set Custom Roles
                        CustomRoles.Clear();
                        var giveRole = writer.ReadBoolean();
                        if (!giveRole) return;
                        var playerId = writer.ReadByte();
                        var player = GameData.Instance.GetPlayerById(playerId).CBEJMNMADDB;
                        CustomRoles.Add(player.PlayerId, true);

                        if (CustomRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                        {
                            var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                            if (role)
                            {
                                PlayerControl.LocalPlayer.nameText.Color = JesterRole.Color;
                            }
                        }

                        return;
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        public static class CrewmateIntro
        {
            public static void Postfix(IntroCutscene __instance)
            {
                if (CustomRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                    if (role)
                    {
                        __instance.ImpostorText.Text = JesterRole.ImpostorText;
                        __instance.Title.Text = JesterRole.Name;
                        __instance.Title.Color = JesterRole.Color;
                        __instance.BackgroundBar.material.SetColor(Color, JesterRole.Color);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_70))]
        public static class OverwriteMeetingColor
        {
            public static void Postfix([HarmonyArgument(0)] GameData.Nested_1 player, ref PlayerVoteArea __result)
            {
                if (player.IBJBIALCEKB.AmOwner && CustomRoles.ContainsKey(player.FMAAJCIEMEH))
                {
                    var role = CustomRoles[PlayerControl.LocalPlayer.PlayerId];
                    if (role)
                    {
                        __result.NameText.Color = JesterRole.Color;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static bool Prefix()
            {
                if (!JesterWon) return true;
                ShipStatus.RpcEndGame(GameOverReason.HumansByTask, false);
                return false;

            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
        public static class IsGameOverDueToDeathPatch
        {
            public static void Postfix(ref bool __result)
            {
                if (JesterWon) __result = true;
            }
        }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTrackerPlug
        {
            public static bool Prefix(PingTracker __instance)
            {
                if (AmongUsClient.Instance)
                {
                    if (AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                    {
                        __instance.gameObject.SetActive(false);
                    }

                    __instance.text.Text =
                        $"Ping: {AmongUsClient.Instance.Ping} ms\n\nMods by\nOttomated\n([C36EFFFF]Twitch[FFFFFFFF], [FE4550FF]Patreon[FFFFFFFF])";
                }

                return false;
            }
        }
    }
}