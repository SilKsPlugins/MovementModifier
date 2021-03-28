using HarmonyLib;
using SDG.Unturned;

namespace MovementModifier
{
    public class UnturnedPatches
    {
        private static bool _modifyingStamina = false;

        [HarmonyPatch(typeof(PlayerLife), "askTire")]
        public static class Stamina
        {
            static void Postfix(PlayerLife __instance, ref byte amount)
            {
                if (_modifyingStamina) return;

                if (MovementModifierPlugin.Instance!.StaminaCosts.TryGetValue(__instance.player.channel.owner.playerID.steamID,
                    out float staminaCost))
                {
                    if (staminaCost == 1) return;

                    _modifyingStamina = true;

                    __instance.serverModifyStamina((1 - staminaCost) * amount);

                    _modifyingStamina = false;
                }
            }
        }
    }
}
