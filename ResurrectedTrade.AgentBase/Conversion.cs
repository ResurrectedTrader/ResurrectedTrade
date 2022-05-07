using System;
using System.Collections.Generic;
using System.Linq;
using ResurrectedTrade.AgentBase.Structs;
using ResurrectedTrade.Common.Enums;
using ResurrectedTrade.Protocol;
using ResurrectedTrade.Protocol.Agent;
using Grid = ResurrectedTrade.Protocol.Grid;
using Stat = ResurrectedTrade.Protocol.Stat;

namespace ResurrectedTrade.AgentBase
{
    public static class Conversion
    {
        public static Stat ToProto(this D2StatStrc stat)
        {
            return new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value };
        }

        public static ItemExport ToProtoExport(this Unit item)
        {
            var pos = item.Position;
            return new ItemExport { Position = checked((ushort)(pos.X * 10 + pos.Y)), Item = item.ToProtoItem() };
        }

        public static Grid ToProtocol(this Enums.Grid grid)
        {
            switch (grid)
            {
                case Enums.Grid.Equipped: return Grid.Equipped;
                case Enums.Grid.Belt: return Grid.Belt;
                case Enums.Grid.Inventory: return Grid.Inventory;
                case Enums.Grid.Cube: return Grid.Cube;
                case Enums.Grid.Stash: return Grid.Stash;
                default: throw new ApplicationException("Unhandled grid type");
            }
        }

        private static Item ToProtoItem(this Unit item)
        {
            List<Stat> baseStats = new List<Stat>();

            foreach (var stat in item.StatList.BaseStats)
            {
                baseStats.Add(
                    new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value }
                );
            }

            List<Stat> fullStats = new List<Stat>();

            foreach (var stat in item.StatList.FullStats)
            {
                fullStats.Add(
                    new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value }
                );
            }

            var modifierStats = item.StatList.GetAddedStatsList();
            if (modifierStats != null)
            {
                // Statlist that contains ED etc stats that are not visible in the full stat list, but used for computation
                foreach (var stat in modifierStats.BaseStats)
                {
                    if (fullStats.Exists(o => (int)stat.Stat == o.Id && stat.Layer == o.Layer))
                    {
                        continue;
                    }

                    fullStats.Add(
                        new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value }
                    );
                }

                // Statlist that contains ED etc stats for base item (superior item)
                // These do not reflect in the final stat list, hence for things like ED, we should add them.
                // You can always work out the ED from base item/runeword by looking at the base stat list.
                // However, these do contain stuff like skills granted by white items, so we can't just blindly add.
                // Only add if they are different (or might need to check stat type)
                foreach (var stat in modifierStats.PrevLink?.BaseStats ?? Array.Empty<D2StatStrc>())
                {
                    var fullStat = fullStats.Find(o => (int)stat.Stat == o.Id && stat.Layer == o.Layer);
                    if (fullStat == null)
                    {
                        fullStats.Add(
                            new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value }
                        );
                    }
                    else if (fullStat.Value != stat.Value)
                    {
                        fullStat.Value += stat.Value;
                    }

                    if (baseStats.Exists(o => (int)stat.Stat == o.Id && stat.Layer == o.Layer))
                    {
                        continue;
                    }

                    baseStats.Add(
                        new Stat { Layer = stat.Layer, Id = (uint)stat.Stat, Value = stat.Value }
                    );
                }
            }

            List<Unit> sockets = new List<Unit>();
            if (item.Inventory != null)
            {
                for (var socketed = item.Inventory.FirstItem; socketed != null; socketed = socketed.ItemData.NextItem)
                {
                    sockets.Add(socketed);
                }
            }


            return new Item
            {
                FileIndex = item.ClassId,
                ItemDataFileIndex = item.ItemData.FileIndex,
                Quality = (uint)item.ItemData.Quality,
                Flags = (uint)(item.ItemData.Flags &
                               (ItemFlags.Ethereal | ItemFlags.Identified | ItemFlags.Personalized |
                                ItemFlags.Runeword)),
                GraphicsIndex = item.ItemData.InvGfxIdx,
                Prefix1 = item.ItemData.Prefix1,
                Prefix2 = item.ItemData.Prefix2,
                Prefix3 = item.ItemData.Prefix3,
                Suffix1 = item.ItemData.Suffix1,
                Suffix2 = item.ItemData.Suffix2,
                Suffix3 = item.ItemData.Suffix3,
                AutoAffix = item.ItemData.AutoAffix,
                RarePrefix = item.ItemData.RarePrefix,
                RareSuffix = item.ItemData.RareSuffix,
                BaseStats =
                {
                    MaskOutStatsThatChangeValues(baseStats.OrderBy(o => (o.Id, o.Layer)), item.ItemData.Flags)
                },
                Stats = { MaskOutStatsThatChangeValues(fullStats.OrderBy(o => (o.Id, o.Layer)), item.ItemData.Flags) },
                Sockets = { sockets.OrderBy(o => (o.Position.X, o.Position.Y)).Select(o => o.ToProtoItem()) }
            };
        }

        private static IEnumerable<Stat> MaskOutStatsThatChangeValues(
            this IEnumerable<Stat> statsEnumerable, ItemFlags flags
        )
        {
            var stats = statsEnumerable.ToList();
            bool selfRepair = stats.Any(o => o.Id == 252);
            bool indestructible = stats.Any(o => o.Id == 152);
            if (!flags.HasFlag(ItemFlags.Ethereal) || selfRepair || indestructible)
            {
                var durability = stats.Find(o => o.Id == 72);
                var maxDurability = stats.Find(o => o.Id == 73);
                if (maxDurability != null && durability != null)
                {
                    durability.Value = maxDurability.Value;
                }
            }

            // ItemChargedSkill = 204
            if (!flags.HasFlag(ItemFlags.Ethereal))
            {
                foreach (Stat stat in stats.Where(o => o.Id == 204))
                {
                    int maxCharges = stat.Value >> 8;
                    stat.Value = (maxCharges << 8) | maxCharges;
                }
            }

            // Quantity = 70
            // ReplenishQuantity = 253,
            if (!flags.HasFlag(ItemFlags.Ethereal) || stats.Any(o => o.Id == 253))
            {
                var quantity = stats.Find(o => o.Id == 70);
                if (quantity != null)
                {
                    quantity.Value = 1;
                }
            }

            return stats;
        }
    }
}
