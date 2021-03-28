using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovementModifier.Configuration;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Core.Helpers;
using OpenMod.Core.Users;
using OpenMod.Unturned.Plugins;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

// For more, visit https://openmod.github.io/openmod-docs/development-guide/making-your-first-plugin/

[assembly: PluginMetadata("MovementModifierPlugin", Author = "SilK", DisplayName = "Movement Modifier")]
namespace MovementModifier
{
    public class MovementModifierPlugin : OpenModUnturnedPlugin
    {
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<MovementModifierPlugin> m_Logger;
        private readonly IUserManager m_UserManager;
        private readonly List<Modifier> m_ActiveModifiers;
        private readonly Dictionary<CSteamID, float> m_StaminaCosts;

        public MovementModifierPlugin(
            IConfiguration configuration,
            ILogger<MovementModifierPlugin> logger,
            IUserManager userManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_UserManager = userManager;
            m_ActiveModifiers = new List<Modifier>();
            m_StaminaCosts = new Dictionary<CSteamID, float>();
        }

        protected override async UniTask OnLoadAsync()
        {
            await UniTask.SwitchToMainThread();

            Level.onPostLevelLoaded += level => OnPostLevelLoaded();
            if (Level.isLoaded)
            {
                OnPostLevelLoaded();
            }

            UnturnedPatches.OnTire += OnTire;
        }

        protected override async UniTask OnUnloadAsync()
        {
            await UniTask.SwitchToMainThread();

            // ReSharper disable once DelegateSubtraction
            Level.onPostLevelLoaded -= level => OnPostLevelLoaded();

            var users =
                (await m_UserManager.GetUsersAsync(KnownActorTypes.Player))
                .OfType<UnturnedUser>();

            foreach (UnturnedUser user in users)
            {
                UpdatePlayer(user.Player.Player);
            }

            UnturnedPatches.OnTire -= OnTire;
        }

        private void OnPostLevelLoaded()
        {
            m_ActiveModifiers.Clear();

            m_ActiveModifiers.Add(m_Configuration.GetSection("Global").Get<Modifier>());

            ItemModifier[] itemModifiers = m_Configuration.GetSection("ItemModifiers").Get<ItemModifier[]>();

            m_Logger.LogInformation($"Loading {itemModifiers.Length} item modifiers...");

            int errors = 0;

            foreach (ItemModifier modifier in itemModifiers)
            {
                if (modifier.GetAsset() == null)
                {
                    Logger.LogError($"Item asset for modifier with Id '{modifier.Id}' could not be found and will be ignored.");
                    errors++;
                    continue;
                }

                if (!modifier.ModifiesAnything())
                {
                    Logger.LogWarning($"Modifier with Id '{modifier.Id}' does not have any non-default modifiers set and will be ignored.");
                    errors++;
                    continue;
                }

                m_ActiveModifiers.Add(modifier);
            }

            if (errors == 0)
            {
                m_Logger.LogInformation("Loaded modifiers successfully.");
            }
            else
            {
                m_Logger.LogError($"{errors} errors/warnings occurred while loading modifiers.");
            }

            var users = AsyncHelper.RunSync(() => m_UserManager.GetUsersAsync(KnownActorTypes.Player))
                .OfType<UnturnedUser>();

            foreach (UnturnedUser user in users)
            {
                UpdatePlayer(user.Player.Player);
            }
        }

        public Modifier GetTotalModifier(Player player)
        {
            Modifier totalModifier = new Modifier();

            foreach (Modifier modifier in m_ActiveModifiers)
            {
                if (modifier.Applies(player))
                {
                    totalModifier *= modifier;
                }
            }

            return totalModifier;
        }

        public void UpdatePlayer(Player player)
        {
            Modifier totalModifier = GetTotalModifier(player);

            player.movement.sendPluginGravityMultiplier(totalModifier.Gravity);
            player.movement.sendPluginJumpMultiplier(totalModifier.Jump);
            player.movement.sendPluginSpeedMultiplier(totalModifier.Speed);

            CSteamID steamId = player.channel.owner.playerID.steamID;

            if (m_StaminaCosts.ContainsKey(steamId))
            {
                m_StaminaCosts[steamId] = totalModifier.StaminaCost;
            }
            else
            {
                m_StaminaCosts.Add(steamId, totalModifier.StaminaCost);
            }
        }

        private bool m_ModifyingStamina;

        private void OnTire(Player player, byte amount)
        {
            if (m_ModifyingStamina) return;

            if (m_StaminaCosts.TryGetValue(player.channel.owner.playerID.steamID, out float staminaCost))
            {
                if (staminaCost == 1) return;

                m_ModifyingStamina = true;

                player.life.serverModifyStamina((1 - staminaCost) * amount);

                m_ModifyingStamina = false;
            }
        }
    }
}
