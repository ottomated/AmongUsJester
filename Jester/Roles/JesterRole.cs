using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jester
{
    public static class JesterRole
    {
        public static byte RoleId = 0;
        public static string Name = "Jester";
        public static Color Color = new Color(0.961f, 0.525f, 0.961f);
        public static string ImpostorText = "Get voted out to win";

        public static Il2CppSystem.Collections.Generic.List<WinningPlayerData> GetWinners(
            Dictionary<byte, bool> customRoles)
        {
            var jesterId = customRoles.FirstOrDefault(x => x.Value).Key;
            var jesterPlayer = GameData.Instance.GetPlayerById(jesterId);
            var list = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>(1);
            list.Add(new WinningPlayerData
            {
                IsYou = jesterId == PlayerControl.LocalPlayer.PlayerId,
                Name = jesterPlayer.LNFMCJAPLBH,
                ColorId = jesterPlayer.ACBLKMFEPKC,
                IsImpostor = false,
                SkinId = jesterPlayer.FHNDEEGICJP,
                PetId = jesterPlayer.HIJJGKGBKOJ,
                HatId = jesterPlayer.KCILOGLJODF,
                IsDead = true
            });
            return list;
        }
    }
}