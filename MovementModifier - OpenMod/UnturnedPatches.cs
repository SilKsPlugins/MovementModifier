using HarmonyLib;
using SDG.Unturned;

namespace MovementModifier
{
    public class UnturnedPatches
    {
        public delegate void Tire(Player player, byte amount);
        public static event Tire OnTire;

        [HarmonyPatch(typeof(PlayerLife), "askTire")]
        private static class Stamina
        {
            static void Postfix(PlayerLife __instance, byte amount)
            {
                OnTire?.Invoke(__instance.player, amount);
            }
        }
    }
}
