using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MovementModifier.Configuration
{
    public class ItemModifier : Modifier
    {
        public string Id { get; set; }

        public bool? MustBeEquipped { get; set; }

        public ItemModifier() : this("0", 1, 1, 1, 1, null)
        {
        }

        public ItemModifier(string id, float speed, float jump, float gravity, float staminaCost, bool? mustBeEquipped) : base(speed, jump, gravity, staminaCost)
        {
            Id = id;
            MustBeEquipped = mustBeEquipped;
        }

        public bool GetMustBeEquipped()
        {
            if (MustBeEquipped.HasValue) return MustBeEquipped.Value;

            if (GetAsset() == null) return false;

            switch (GetAsset().type)
            {
                case EItemType.BACKPACK:
                case EItemType.GLASSES:
                case EItemType.HAT:
                case EItemType.MASK:
                case EItemType.PANTS:
                case EItemType.SHIRT:
                case EItemType.VEST:

                case EItemType.CLOUD:
                case EItemType.GUN:
                case EItemType.MELEE:
                case EItemType.TOOL:
                case EItemType.VEHICLE_REPAIR_TOOL:
                    return true;

                default:
                    return false;
            }
        }

        private static int LevenshteinDistance(string s, string t)
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

        private ItemAsset _cachedAsset;

        public ItemAsset GetAsset()
        {
            if (_cachedAsset != null) return _cachedAsset;

            if (string.IsNullOrWhiteSpace(Id)) return null;

            if (ushort.TryParse(Id, out ushort parsed))
            {
                _cachedAsset = Assets.find(EAssetType.ITEM, parsed) as ItemAsset;

                if (_cachedAsset != null) return _cachedAsset;
            }

            List<ItemAsset> possibilities = new List<ItemAsset>();

            string lowered = Id.ToLower();

            foreach (ItemAsset asset in Assets.find(EAssetType.ITEM).OfType<ItemAsset>())
            {
                if (string.IsNullOrWhiteSpace(asset.itemName)) continue;

                if (asset.itemName.ToLower().Contains(lowered))
                {
                    possibilities.Add(asset);
                }
            }

            _cachedAsset = possibilities
                .OrderBy(x => LevenshteinDistance(x.itemName.ToLower(), lowered))
                .FirstOrDefault();

            return _cachedAsset;
        }

        public bool IsEquipped(Player player)
        {
            ItemAsset asset = GetAsset();

            EItemType type = asset.type;

            switch (type)
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

        public bool HasItem(Player player)
        {
            if (IsEquipped(player)) return true;

            ushort id = GetAsset().id;

            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                Items items = player.inventory?.items[page];

                ItemJar jar = items?.items?.FirstOrDefault(x => x.item.id == id);

                if (jar != null) return true;
            }

            return false;
        }

        public override bool Applies(Player player)
        {
            return GetMustBeEquipped()
                ? IsEquipped(player)
                : HasItem(player);
        }
    }
}
