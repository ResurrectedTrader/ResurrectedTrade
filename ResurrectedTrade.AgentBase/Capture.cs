using System.Collections.Generic;
using System.Linq;
using ResurrectedTrade.AgentBase.Enums;
using ResurrectedTrade.AgentBase.Memory;
using ResurrectedTrade.AgentBase.Structs;
using ResurrectedTrade.Common;
using ResurrectedTrade.Common.Enums;
using ResurrectedTrade.Protocol;
using ResurrectedTrade.Protocol.Agent;
using Grid = ResurrectedTrade.AgentBase.Enums.Grid;
using Stat = ResurrectedTrade.Common.Enums.Stat;

namespace ResurrectedTrade.AgentBase
{
    public class Capture
    {
        private const CharFlag HardcoreDied = CharFlag.Hardcore | CharFlag.DiedBefore;

        private static readonly Grid[] SupportedGrids =
        {
            Grid.Equipped,
            //Grid.Belt,
            Grid.Inventory, Grid.Cube, Grid.Stash
        };

        private static readonly HashSet<Stat> CharacterStats =
            new HashSet<Stat> { Stat.Gold, Stat.StashGold, Stat.Level };

        private static readonly HashSet<uint> HirelingClassIds = new HashSet<uint>
        {
            271, // A1
            338, // A2
            359, // A3
            560, // A5
            561 // A5
        };

        private readonly MemoryAccess _access;
        private readonly ILogger _logger;

        private readonly IOffsets _offsets;

        public Capture(IOffsets offsets, ILogger logger, MemoryAccess access)
        {
            _offsets = offsets;
            _logger = logger;
            _access = access;
        }

        private GridExport GetGridExport(
            Structs.Grid grid, GridManifest manifest, bool replace = false, bool checkWeaponSwap = false
        )
        {
            var export = new GridExport();
            Dictionary<uint, int> positionToHash = manifest?.Items.ToDictionary(o => o.Position, o => o.Hash) ??
                                                   new Dictionary<uint, int>();
            var swapWeapons = false;
            if (checkWeaponSwap)
            {
                swapWeapons = GetStateFlag(0xF2D7CF8E9CC08212) == 1;
            }

            if (grid != null)
            {
                foreach (var item in grid)
                {
                    ItemExport itemExport = item.ToProtoExport();
                    if (swapWeapons)
                    {
                        switch (itemExport.Position)
                        {
                            case 40:
                                itemExport.Position = 110;
                                break;
                            case 110:
                                itemExport.Position = 40;
                                break;
                            case 50:
                                itemExport.Position = 120;
                                break;
                            case 120:
                                itemExport.Position = 50;
                                break;
                        }
                    }

                    int exportHash = itemExport.Item.Hash();
                    if (!positionToHash.TryGetValue(itemExport.Position, out int hash))
                    {
                        export.Adds.Add(itemExport);
                    }
                    else if (hash != exportHash || replace)
                    {
                        export.Removes.Add(itemExport.Position);
                        export.Adds.Add(itemExport);
                    }

                    positionToHash.Remove(itemExport.Position);
                }
            }

            foreach (uint position in positionToHash.Keys)
            {
                export.Removes.Add(position);
            }

            return export;
        }

        private CharacterExport GetCharacterExport(
            Unit player, CharacterManifest manifest, CharFlag charFlags, bool replace = false
        )
        {
            var playerName = player.PlayerData.Name;
            List<GridExport> charGrids = new List<GridExport>();
            foreach (var gridType in SupportedGrids)
            {
                GridManifest gridManifest = manifest?.Grids.FirstOrDefault(o => o.Grid == gridType.ToProtocol());
                var grid = player.Inventory.GetGrid(gridType);
                bool isEquipped = gridType == Grid.Equipped;
                var export = new GridExport();

                // Don't update equipped if character died, to keep equipped items before death.
                if (!isEquipped || (charFlags & HardcoreDied) != HardcoreDied)
                {
                    export = GetGridExport(grid, gridManifest, replace, isEquipped);
                }

                if (export.Adds.Any() || export.Removes.Any())
                {
                    _logger.Info($"{playerName} {gridType} changed");
                    export.Grid = gridType.ToProtocol();
                    charGrids.Add(export);
                }
            }

            var merc = GetMerc(player);
            if (merc != null)
            {
                GridManifest gridManifest = manifest?.Grids.FirstOrDefault(o => o.Grid == Protocol.Grid.Mercenary);
                var grid = merc.Inventory.GetGrid(Grid.Equipped);
                GridExport export = GetGridExport(grid, gridManifest, replace);
                if (export.Adds.Any() || export.Removes.Any())
                {
                    _logger.Info($"Something changed in mercenery of {playerName}");
                    export.Grid = Protocol.Grid.Mercenary;
                    charGrids.Add(export);
                }
            }

            var charExport = new CharacterExport
            {
                Class = player.ClassId,
                Name = playerName,
                Flags = (uint)charFlags,
                Stats =
                {
                    player.StatList.BaseStats
                        .Where(o => CharacterStats.Contains(o.Stat))
                        .Select(o => o.ToProto())
                },
                Grids = { charGrids }
            };


            bool hashMismatch = manifest == null || charExport.Hash() != manifest.Hash;
            if (hashMismatch || charExport.Grids.Any())
            {
                _logger.Info(
                    $"Something in char {charExport.Name}: hash mismatch: {hashMismatch} grids: {charExport.Grids.Any()} manifest: {manifest != null}"
                );
                return charExport;
            }

            return null;
        }

        private CharacterExport GetSharedStashExport(
            string stashName, Unit sharedStash, CharacterManifest manifest, uint charFlags, bool replace = false
        )
        {
            var charExport = new CharacterExport
            {
                Class = 0,
                Name = stashName,
                // Copy char flags from player, to signify what game mode the stash tabs are in.
                Flags = charFlags & (uint)(CharFlag.Hardcore | CharFlag.Expansion | CharFlag.Ladder),
                Stats =
                {
                    sharedStash.StatList.BaseStats
                        .Where(o => o.Stat == Stat.StashGold)
                        .Select(o => o.ToProto())
                }
            };

            GridManifest gridManifest = manifest?.Grids.FirstOrDefault(o => o.Grid == Protocol.Grid.Stash);
            var grid = sharedStash.Inventory.GetGrid(Grid.Stash);
            GridExport export = GetGridExport(grid, gridManifest, replace);
            if (export.Adds.Any() || export.Removes.Any())
            {
                _logger.Info($"Shared stash {stashName} changed");
                export.Grid = Protocol.Grid.Stash;
                charExport.Grids.Add(export);
            }

            bool hashMismatch = manifest == null || charExport.Hash() != manifest.Hash;
            if (hashMismatch || charExport.Grids.Any())
            {
                _logger.Info(
                    $"Something changed in stash {stashName}: hash mismatch: {hashMismatch} grids: {charExport.Grids.Any()} manifest: {manifest != null}"
                );
                return charExport;
            }

            return null;
        }

        public Export GetExport(Manifest manifest, bool replace = false, bool isOnline = false)
        {
            // Don't do much while not in game or in loading screen
            var inGame = _access.Read<bool>(_access.BaseAddress + _offsets.InGame);
            if (!inGame)
            {
                _logger.Debug($"In game = {inGame}");
                return null;
            }

            var loadComplete = _access.Read<bool>(_access.BaseAddress + _offsets.LoadGameComplete);
            if (!loadComplete)
            {
                _logger.Debug($"Load complete = {loadComplete}");
                return null;
            }

            var player = GetPlayerUnit();
            if (player == null)
            {
                _logger.Debug("No player...");
                return null;
            }

            if (!player.IsFullyLoaded())
            {
                _logger.Debug("Player not fully loaded...");
                return null;
            }

            // Don't submit while items are on cursor, otherwise we can't detect moves.
            if (player.Inventory.Struct.pCursorItem != Ptr.Zero)
            {
                _logger.Debug("Item in cursor...");
                return null;
            }

            var charFlags = (CharFlag)_access.Read<uint>(_access.BaseAddress + _offsets.CharFlags);

            var export = new Export();

            CharacterManifest charManifest = manifest?.Characters.FirstOrDefault(o => o.Name == player.PlayerData.Name);

            CharacterExport charExport = GetCharacterExport(player, charManifest, charFlags, replace);
            if (charExport != null)
            {
                export.Characters.Add(charExport);
            }

            var stashSuffix = "";
            if (charFlags.HasFlag(CharFlag.Hardcore))
            {
                stashSuffix = "h";
            }

            if (isOnline)
            {
                if (charFlags.HasFlag(CharFlag.Ladder))
                {
                    stashSuffix += "l";
                }
            }
            else
            {
                // Shared stashes seem to be shared between laader and non-ladder offline
                // (as there is no ladder, but char might have the flag) 
                // Unset the ladder bit.
                charFlags &= ~CharFlag.Ladder;
            }

            uint idx = 0;
            foreach (var sharedStashUnit in GetSharedStashUnits())
            {
                var stashName = $"_{idx}{stashSuffix}";

                CharacterExport sharedStashExport = GetSharedStashExport(
                    stashName,
                    sharedStashUnit,
                    manifest?.Characters.FirstOrDefault(o => o.Name == stashName),
                    (uint)charFlags,
                    replace
                );
                if (sharedStashExport != null)
                {
                    export.Characters.Add(sharedStashExport);
                }

                idx++;
            }

            export = IdentifyMoves(export, manifest);
            Validate(export, manifest);

            if (export.Characters.Any() || export.Moves.Any())
            {
                return export;
            }

            return null;
        }

        private void Validate(Export export, Manifest manifest)
        {
            if (manifest == null) return;

            var taken = new HashSet<Location>();
            foreach (CharacterManifest manifestCharacter in manifest.Characters)
            {
                foreach (GridManifest manifestCharacterGrid in manifestCharacter.Grids)
                {
                    foreach (ItemManifest itemManifest in manifestCharacterGrid.Items)
                    {
                        var location = new Location
                        {
                            Character = manifestCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = manifestCharacterGrid.Grid, Position = itemManifest.Position
                            }
                        };
                        taken.Add(location);
                    }
                }
            }

            foreach (CharacterExport exportCharacter in export.Characters)
            {
                foreach (GridExport exportCharacterGrid in exportCharacter.Grids)
                {
                    foreach (var removePosition in exportCharacterGrid.Removes)
                    {
                        var location = new Location
                        {
                            Character = exportCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = exportCharacterGrid.Grid, Position = removePosition
                            }
                        };
                        taken.Remove(location);
                    }
                }
            }

            foreach (CharacterExport exportCharacter in export.Characters)
            {
                foreach (GridExport exportCharacterGrid in exportCharacter.Grids)
                {
                    foreach (var itemExport in exportCharacterGrid.Adds)
                    {
                        var location = new Location
                        {
                            Character = exportCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = exportCharacterGrid.Grid,
                                Position = itemExport.Position
                            }
                        };

                        if (taken.Contains(location))
                        {
                            _logger.Debug($"Trying to add to a taken position: {location}");
                        }
                    }
                }
            }
        }

        private Export IdentifyMoves(Export export, Manifest manifest)
        {
            if (manifest == null) return export;

            Dictionary<Location, int> locationToHash = new Dictionary<Location, int>();
            foreach (CharacterManifest manifestCharacter in manifest.Characters)
            {
                foreach (GridManifest manifestCharacterGrid in manifestCharacter.Grids)
                {
                    foreach (ItemManifest itemManifest in manifestCharacterGrid.Items)
                    {
                        var location = new Location
                        {
                            Character = manifestCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = manifestCharacterGrid.Grid, Position = itemManifest.Position
                            }
                        };
                        locationToHash[location] = itemManifest.Hash;
                    }
                }
            }

            // Collection of genuinely removed or added items.
            List<(Location, int)> removes = new List<(Location, int)>();
            List<(Location, int)> adds = new List<(Location, int)>();

            foreach (CharacterExport exportCharacter in export.Characters)
            {
                foreach (GridExport exportCharacterGrid in exportCharacter.Grids)
                {
                    foreach (var removePosition in exportCharacterGrid.Removes)
                    {
                        var location = new Location
                        {
                            Character = exportCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = exportCharacterGrid.Grid, Position = removePosition
                            }
                        };
                        removes.Add((location, locationToHash[location]));
                        _logger.Debug($"Removed {removes.Last()}");
                    }

                    foreach (ItemExport itemExport in exportCharacterGrid.Adds)
                    {
                        var location = new Location
                        {
                            Character = exportCharacter.Name,
                            GridPosition = new GridPosition
                            {
                                Grid = exportCharacterGrid.Grid, Position = itemExport.Position
                            }
                        };
                        adds.Add((location, itemExport.Item.Hash()));
                        _logger.Debug($"Add {adds.Last()}");
                    }
                }
            }

            // For each remove, find an add.
            foreach (var (removeLocation, removeHash) in removes)
            {
                Location addLocation = adds.FirstOrDefault(o => o.Item2 == removeHash).Item1;
                if (addLocation == null)
                {
                    // Can't find the remove with the same hash, probably genuinely new.
                    _logger.Debug($"Cannot find add for {removeHash}");
                    continue;
                }

                adds.Remove((addLocation, removeHash));
                // Not needed in theory.
                // removes.Remove((removeLocation, removeHash));

                // Remove the remove
                if (!export.Characters
                        .First(o => o.Name == removeLocation.Character)
                        .Grids
                        .First(o => o.Grid == removeLocation.GridPosition.Grid)
                        .Removes
                        .Remove(removeLocation.GridPosition.Position))
                {
                    _logger.Debug($"Failed to remove the remove {removeLocation}");
                }
                else
                {
                    _logger.Debug($"Removed the remove {removeLocation}");
                }

                // Remove the add
                var grid = export.Characters
                    .First(o => o.Name == addLocation.Character)
                    .Grids
                    .First(o => o.Grid == addLocation.GridPosition.Grid);

                // There should be only one in theory
                foreach (ItemExport itemExport in grid.Adds
                             .Where(o => o.Position == addLocation.GridPosition.Position).ToList())
                {
                    _logger.Debug($"Removing add {addLocation}");
                    grid.Adds.Remove(itemExport);
                }

                // Replace with move
                _logger.Debug(
                    $"Move from {removeLocation.Character} {removeLocation.GridPosition.Grid} {removeLocation.GridPosition.Position} to {addLocation.Character} {addLocation.GridPosition.Grid} {addLocation.GridPosition.Position}"
                );

                export.Moves.Add(
                    new Move { From = removeLocation, To = addLocation }
                );
            }

            // Clean up things that have been translated into moves.
            foreach (CharacterExport exportCharacter in export.Characters.ToList())
            {
                foreach (GridExport exportCharacterGrid in exportCharacter.Grids.ToList())
                {
                    if (!exportCharacterGrid.Adds.Any() && !exportCharacterGrid.Removes.Any())
                    {
                        exportCharacter.Grids.Remove(exportCharacterGrid);
                    }
                }

                // Do not remove characters, as flags might have changed.
            }

            return export;
        }

        public Unit GetPlayerUnit()
        {
            var charFlags = (CharFlag)_access.Read<uint>(_access.BaseAddress + _offsets.CharFlags);
            var expansionCharacter = charFlags.HasFlag(CharFlag.Expansion);
            foreach (var unit in GetPlayerUnits())
            {
                if (unit.Type != UnitType.Player) continue;
                if (unit.Struct.pUnitData == Ptr.Zero) continue;
                if (string.IsNullOrEmpty(unit.PlayerData?.Name)) continue;
                if (unit.Struct.pPath == Ptr.Zero) continue;
                // 186 - shared stash state
                if (unit.StatList.HasState(186)) continue;
                if (unit.Mode == 0) continue;

                // MAGIXXX
                var userBaseOffset = expansionCharacter ? 0x70 : 0x30;
                var checkUser = expansionCharacter ? 0 : 1;

                var userBaseCheck = _access.Read<int>(unit.Struct.pInventory + userBaseOffset);
                if (userBaseCheck != checkUser)
                {
                    return unit;
                }
            }

            return null;
        }

        private IEnumerable<Unit> GetSharedStashUnits()
        {
            foreach (uint unitId in GetPlayerUnit().Inventory.GetSharedStashUnitIDs())
            {
                yield return GetPlayerUnits().First(o => o.UnitId == unitId);
            }
        }

        private IEnumerable<Unit> GetPlayerUnits()
        {
            Ptr[] unitPtrs = _access.Read<Ptr>(_access.BaseAddress + _offsets.UnitHashTable, 128);
            IEnumerable<Unit> units = unitPtrs
                .Where(o => o != Ptr.Zero)
                .Select(o => new Unit(_access, o));
            foreach (var unit in units)
            {
                var u = unit;
                while (u != null)
                {
                    yield return u;
                    u = u.ListNext;
                }
            }
        }

        private Unit GetMerc(Unit player)
        {
            var firstPetAddress = _access.Read<Ptr>(_access.BaseAddress + _offsets.Pets);
            if (firstPetAddress == Ptr.Zero)
            {
                return null;
            }

            var pet = new Pet(_access, firstPetAddress);
            while (pet != null)
            {
                if (pet.OwnerId == player.UnitId && HirelingClassIds.Contains(pet.ClassId))
                {
                    break;
                }

                pet = pet.GetNext();
            }

            if (pet == null)
            {
                return null;
            }

            Ptr[] npcs = _access.Read<Ptr>(_access.BaseAddress + _offsets.UnitHashTable + 128 * Ptr.Size, 128);
            IEnumerable<Unit> units = npcs
                .Where(o => o != Ptr.Zero)
                .Select(o => new Unit(_access, o));
            foreach (var unit in units)
            {
                var u = unit;
                while (u != null)
                {
                    if (HirelingClassIds.Contains(u.ClassId) && u.UnitId == pet.UnitId)
                    {
                        return u;
                    }

                    u = u.ListNext;
                }
            }

            return null;
        }

        private byte GetStateFlag(ulong flag)
        {
            var stateFlags = _access.Read<Ptr>(_access.BaseAddress + _offsets.WidgetStates);
            var v2 = _access.Read<ulong>(stateFlags + 8);
            if (v2 == 0)
            {
                return 0;
            }

            ulong v4 = 0xC4CEB9FE1A85EC53ul
                       * ((0xFF51AFD7ED558CCDul * (flag ^ (flag >> 33))) ^
                          ((0xFF51AFD7ED558CCDul * (flag ^ (flag >> 33))) >> 33));
            ulong v5 = (_access.Read<ulong>(stateFlags) - 1) & (v4 ^ (v4 >> 33));
            var v6 = _access.Read<ulong>(v2 + 8 * v5);
            ulong i = v2 + 8 * v5;
            for (; v6 != 0; v6 = _access.Read<ulong>(v6))
            {
                if (flag == _access.Read<ulong>(v6 + 8))
                {
                    break;
                }

                i = v6;
            }

            var ir = _access.Read<ulong>(i);
            if (ir != 0)
            {
                return _access.Read<byte>(_access.Read<ulong>(_access.Read<ulong>(ir + 16) + 16));
            }

            return 0;
        }
    }
}
