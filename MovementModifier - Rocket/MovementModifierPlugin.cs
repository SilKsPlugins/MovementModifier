using HarmonyLib;
using MovementModifier.Configuration;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace MovementModifier
{
    public class MovementModifierPlugin : RocketPlugin<MovementModifierConfiguration>
    {
        public static MovementModifierPlugin? Instance { get; private set; } = null;

        public const string HarmonyId = "com.silk.movementmodifier";

        public Harmony? HarmonyInstance { get; private set; } = null;

        public List<ItemModifier> ActiveModifiers = new List<ItemModifier>();

        internal Dictionary<CSteamID, float> StaminaCosts = new Dictionary<CSteamID, float>();

        protected override void Load()
        {
            Level.onPostLevelLoaded += OnPostLevelLoaded;
            if (Level.isLoaded)
            {
                OnPostLevelLoaded(0);
            }
        }

        private void OnPostLevelLoaded(int level)
        {
            if (Instance != null) return;

            Instance = this;

            int errors = 0;

            Logger.Log($"Loading {Configuration.Instance.ItemModifiers?.Length} modifiers...");

            ActiveModifiers.Clear();

            foreach (ItemModifier modifier in Configuration.Instance.ItemModifiers!)
            {
                modifier.Asset = GetAsset(modifier.Id.Trim());

                if (modifier.Asset == null)
                {
                    Logger.LogError($"Item asset for modifier with Id '{modifier.Id}' could not be found and will be ignored.");
                    errors++;
                    continue;
                }

                if (!modifier.ModifiesAnything)
                {
                    Logger.LogError($"Modifier with Id '{modifier.Id}' does not have any non-default modifiers set and will be ignored.");
                    modifier.Asset = null;
                    errors++;
                    continue;
                }

                ActiveModifiers.Add(modifier);
            }

            if (errors == 0)
            {
                Logger.Log("Loaded modifiers successfully.");
            }
            else
            {
                Logger.LogError($"{errors} errors occurred while loading modifiers.");
            }

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            PlayerEquipment.OnUseableChanged_Global += OnEquippedChanged;

            foreach (Player player in GetOnlinePlayers())
            {
                InitPlayer(player);
            }

            HarmonyInstance = new Harmony(HarmonyId);
            HarmonyInstance.PatchAll(GetType().Assembly);
        }

        protected override void Unload()
        {
            Level.onPostLevelLoaded -= OnPostLevelLoaded;

            if (HarmonyInstance != null)
            {
                HarmonyInstance.UnpatchAll(HarmonyId);
                HarmonyInstance = null;
            }

            StopAllCoroutines();

            foreach (Player player in GetOnlinePlayers())
            {
                Release(player);
            }

            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

            Instance = null;
        }

        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            for (int i = 0; i <= n; d[i, 0] = i++)
                ;
            for (int j = 0; j <= m; d[0, j] = j++)
                ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static ItemAsset? GetAsset(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            if (ushort.TryParse(id, out ushort parsed))
            {
                if (Assets.find(EAssetType.ITEM, parsed) is ItemAsset result) return result;
            }

            List<ItemAsset> possibilities = new List<ItemAsset>();

            string lowered = id.ToLower();

            foreach (ItemAsset asset in Assets.find(EAssetType.ITEM).OfType<ItemAsset>())
            {
                if (string.IsNullOrWhiteSpace(asset.itemName)) continue;

                if (asset.itemName.ToLower().Contains(lowered))
                {
                    possibilities.Add(asset);
                }
            }

            return possibilities.OrderBy(x => LevenshteinDistance(x.itemName.ToLower(), lowered))
                .FirstOrDefault();
        }

        #region Events
        
        private void OnPlayerConnected(UnturnedPlayer player)
        {
            StartCoroutine(nameof(DelayedInit), player.Player);
        }

        private void OnPlayerDisconnected(UnturnedPlayer player) => Release(player.Player);

        private IEnumerator DelayedInit(Player player)
        {
            yield return new WaitForSeconds(3);

            InitPlayer(player);
        }

        private void InitPlayer(Player player)
        {
            player.clothing.onBackpackUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onGlassesUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onHatUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onMaskUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onPantsUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onShirtUpdated += (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onVestUpdated += (a, b, c) => UpdatePlayerMultipliers(player);

            player.inventory.onInventoryAdded += (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryRemoved += (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryResized += (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryStateUpdated += () => UpdatePlayerMultipliers(player);

            UpdatePlayerMultipliers(player);
        }

        private void Release(Player player)
        {
            player.clothing.onBackpackUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onGlassesUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onHatUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onMaskUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onPantsUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onShirtUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.clothing.onVestUpdated -= (a, b, c) => UpdatePlayerMultipliers(player);

            player.inventory.onInventoryAdded -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryRemoved -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryResized -= (a, b, c) => UpdatePlayerMultipliers(player);
            player.inventory.onInventoryStateUpdated -= () => UpdatePlayerMultipliers(player);

            UpdatePlayerMultipliers(player, true);
        }

        private void OnEquippedChanged(PlayerEquipment equipment)
        {
            UpdatePlayerMultipliers(equipment.player);
        }

        #endregion

        public static IEnumerable<Player> GetOnlinePlayers() => Provider.clients.Where(x => x != null && x.player != null).Select(x => x.player);

        public bool IsEquipped(Player player, ItemAsset asset)
        {
            switch (asset.type)
            {
                case EItemType.BACKPACK:
                    return player.clothing.backpack == asset.id;
                case EItemType.GLASSES:
                    return player.clothing.glasses == asset.id;
                case EItemType.HAT:
                    return player.clothing.hat == asset.id;
                case EItemType.MASK:
                    return player.clothing.mask == asset.id;
                case EItemType.PANTS:
                    return player.clothing.pants == asset.id;
                case EItemType.SHIRT:
                    return player.clothing.shirt == asset.id;
                case EItemType.VEST:
                    return player.clothing.vest == asset.id;
                default:
                    return player.equipment.itemID == asset.id;
            }
        }

        public bool HasItem(Player player, ushort id)
        {
            if (player.clothing.backpack == id) return true;
            if (player.clothing.glasses == id) return true;
            if (player.clothing.hat == id) return true;
            if (player.clothing.mask == id) return true;
            if (player.clothing.pants == id) return true;
            if (player.clothing.shirt == id) return true;
            if (player.clothing.vest == id) return true;

            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                var items = player.inventory.items[page];

                var jar = items?.items.FirstOrDefault(x => x.item.id == id);

                if (jar != null) return true;
            }

            return false;
        }

        public Multipliers GetPlayerMultipliers(Player player)
        {
            var result = Configuration.Instance.GlobalMultipliers!.Clone();

            foreach (ItemModifier modifier in ActiveModifiers)
            {
                bool applies = modifier.GetMustBeEquipped()
                    ? IsEquipped(player, modifier.Asset!)
                    : HasItem(player, modifier.Asset!.id);

                if (applies)
                {
                    result.Gravity *= modifier.Gravity;
                    result.Jump *= modifier.Jump;
                    result.Speed *= modifier.Speed;
                    result.StaminaCost *= modifier.StaminaCost;
                }
            }

            return result;
        }

        public void UpdatePlayerMultipliers(Player player, bool defaults = false)
        {
            Multipliers multipliers = defaults ? new Multipliers()
            {
                Gravity = 1,
                Jump = 1,
                Speed = 1,
                StaminaCost = 1
            } : GetPlayerMultipliers(player);

            player.movement.sendPluginGravityMultiplier(multipliers.Gravity);
            player.movement.sendPluginJumpMultiplier(multipliers.Jump);
            player.movement.sendPluginSpeedMultiplier(multipliers.Speed);

            CSteamID steamId = player.channel.owner.playerID.steamID;

            StaminaCosts.Set(steamId, multipliers.StaminaCost);
        }
    }
}
