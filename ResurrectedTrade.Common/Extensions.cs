using System.Collections.Generic;
using System.IO;
using System.Linq;
using Force.Crc32;
using ResurrectedTrade.Protocol;
using ResurrectedTrade.Protocol.Agent;

namespace ResurrectedTrade.Common
{
    public static class Extensions
    {
        public static int Hash(this IEnumerable<Stat> stats)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                foreach (Stat stat in stats.OrderBy(o => (o.Id, o.Layer)))
                {
                    writer.Write(stat.Id);
                    writer.Write(stat.Layer);
                    writer.Write(stat.Value);
                }

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this Item item)
        {
            // From protobuf release notes:
            //
            //    The deterministic serialization is, however, NOT canonical across languages;
            //
            // Hence, we roll our own.
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(item.FileIndex);
                writer.Write(item.ItemDataFileIndex);
                writer.Write(item.Quality);
                writer.Write(item.Flags);
                writer.Write(item.GraphicsIndex);
                writer.Write(item.Prefix1);
                writer.Write(item.Prefix2);
                writer.Write(item.Prefix3);
                writer.Write(item.Suffix1);
                writer.Write(item.Suffix2);
                writer.Write(item.Suffix3);
                writer.Write(item.AutoAffix);
                writer.Write(item.RarePrefix);
                writer.Write(item.RareSuffix);
                writer.Write(item.BaseStats.Hash());
                writer.Write(item.Stats.Hash());
                foreach (Item socketedItem in item.Sockets)
                {
                    writer.Write(socketedItem.Hash());
                }

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this CharacterExport character)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(character.Name);
                writer.Write(character.Class);
                writer.Write(character.Flags);
                writer.Write(character.Stats.Hash());

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this Move move)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(move.From.Hash());
                writer.Write(move.To.Hash());

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this Location location)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(location.Character);
                writer.Write(location.GridPosition.Hash());

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this GridPosition gridPosition)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((int)gridPosition.Grid);
                writer.Write(gridPosition.Position);

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }

        public static int Hash(this Export export)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(export.BattleTag);
                foreach (CharacterExport exportCharacter in export.Characters)
                {
                    writer.Write(exportCharacter.Hash());
                }

                foreach (Move exportMove in export.Moves)
                {
                    writer.Write(exportMove.Hash());
                }

                return unchecked((int)Crc32Algorithm.Compute(ms.ToArray()));
            }
        }
    }
}
