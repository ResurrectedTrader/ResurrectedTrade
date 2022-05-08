using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Win32;
using ResurrectedTrade.AgentBase.Memory;
using ResurrectedTrade.AgentBase.Structs;
using ResurrectedTrade.Common;
using ResurrectedTrade.Protocol;
using ResurrectedTrade.Protocol.Agent;

namespace ResurrectedTrade.AgentBase
{
    public class SubmitOutcome
    {
        public bool Attempted;
        public int CooldownMilliseconds;
        public string ErrorId;
        public string ErrorMessage;
        public bool Success;
    }

    public class Runner
    {
        private readonly AgentService.AgentServiceClient _client;
        private readonly ILogger _logger;
        private readonly IOffsets _offsets;
        private readonly Dictionary<string, int> _previousHash = new Dictionary<string, int>();
        private bool _debounce = true;
        private Dictionary<string, Manifest> _manifests;

        public Runner(IOffsets offsets, ILogger logger, AgentService.AgentServiceClient client)
        {
            _offsets = offsets;
            _logger = logger;
            _client = client;
        }

        public void Initialize()
        {
            var remoteManifests = _client
                .GetManifests(new Empty())
                .ResponseStream
                .ToListAsync()
                .Result
                .ToDictionary(o => o.BattleTag, o => o);
            _logger.Info($"Got remote manifests: {remoteManifests.Count}");
            _manifests = remoteManifests;
            _previousHash.Clear();
        }

        public bool IsInitialized()
        {
            return _manifests != null;
        }

        public Export GetExport(Process process)
        {
            return GetExport(process.Handle, process.MainModule.BaseAddress);
        }

        public Export GetExport(Ptr handle, Ptr baseAddress, bool replace = false)
        {
            if (_manifests == null) throw new ApplicationException("No manifests found");
            var access = new RemoteMemoryAccess(handle, baseAddress);

            var sessionData = access.Read<D2SessionData>(access.BaseAddress + _offsets.SessionData);
            string battleTag = Encoding.UTF8.GetString(
                access.Read<byte>(sessionData.pBattleTagStr, (uint)sessionData.BattleTagLength)
            );

            // Local chars go under '_' battle tag.
            var isOnline = access.Read<bool>(access.BaseAddress + _offsets.IsOnlineGame);
            if (!isOnline)
            {
                battleTag = "_";
            }

            var capture = new Capture(_offsets, _logger, access);

            _manifests.TryGetValue(battleTag, out var manifest);

            var accountExport = capture.GetExport(manifest, replace: replace, isOnline: isOnline);
            if (accountExport != null)
            {
                _logger.Info($"Export {battleTag} {string.Join(", ", accountExport.Characters.Select(o => o.Name))}");
                _logger.Debug($"Export: {accountExport}");
                accountExport.BattleTag = battleTag;

                var region = (string)Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Blizzard Entertainment\Battle.net\Launch Options\OSI",
                    "REGION",
                    ""
                );

                var locale = (string)Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Blizzard Entertainment\Battle.net\Launch Options\OSI",
                    "LOCALE",
                    ""
                );

                // In theory we could use these from the game client, but given there is only one value per account
                // Using the "latest one that had changes" makes most sense, atleast in terms of region.
                // Locale probably does not matter much.
                switch (region?.ToUpper())
                {
                    case "US":
                        accountExport.Region = Region.Americas;
                        break;
                    case "EU":
                        accountExport.Region = Region.Europe;
                        break;
                    case "KR":
                        accountExport.Region = Region.Asia;
                        break;
                    default:
                        accountExport.Region = Region.Undefined;
                        break;
                }

                switch (locale?.ToUpper())
                {
                    case "ENUS":
                        accountExport.Locale = Locale.EnUs;
                        break;
                    case "ZHTW":
                        accountExport.Locale = Locale.ZhTw;
                        break;
                    case "DEDE":
                        accountExport.Locale = Locale.DeDe;
                        break;
                    case "ESES":
                        accountExport.Locale = Locale.EsEs;
                        break;
                    case "FRFR":
                        accountExport.Locale = Locale.FrFr;
                        break;
                    case "ITIT":
                        accountExport.Locale = Locale.ItIt;
                        break;
                    case "KOKR":
                        accountExport.Locale = Locale.KoKr;
                        break;
                    case "PLPL":
                        accountExport.Locale = Locale.PlPl;
                        break;
                    case "ESMX":
                        accountExport.Locale = Locale.EsMx;
                        break;
                    case "JAJP":
                        accountExport.Locale = Locale.JaJp;
                        break;
                    case "PTBR":
                        accountExport.Locale = Locale.PtBr;
                        break;
                    case "RURU":
                        accountExport.Locale = Locale.RuRu;
                        break;
                    case "ZHCN":
                        accountExport.Locale = Locale.ZhCn;
                        break;
                    default:
                        accountExport.Locale = Locale.Undefined;
                        break;
                }
            }

            return accountExport;
        }

        public SubmitOutcome SubmitExport(Export accountExport, bool? debounce = null)
        {
            var accountExportHash = accountExport.Hash();
            if ((!_previousHash.TryGetValue(accountExport.BattleTag, out int prevHash) ||
                 prevHash != accountExportHash) && (debounce ?? _debounce))
            {
                _logger.Debug("Debouncing...");
                _previousHash[accountExport.BattleTag] = accountExportHash;
                return new SubmitOutcome
                {
                    Attempted = false
                };
            }

            var response = SendExport(accountExport);
            _logger.Debug($"Got new manifest: {response.NewManifest}");
            _manifests[accountExport.BattleTag] = response.NewManifest;
            _debounce = response.ShouldDebounce;

            return new SubmitOutcome
            {
                Attempted = true,
                Success = response.Success,
                ErrorId = response.ErrorId,
                ErrorMessage = response.ErrorMessage,
                CooldownMilliseconds = response.CooldownMilliseconds
            };
        }

        private ExportResponse SendExport(Export export)
        {
            var start = Stopwatch.StartNew();
            _logger.Info($"Posting {export.BattleTag} {string.Join(", ", export.Characters.Select(o => o.Name))}");
            var response = _client.SubmitExport(export);
            _logger.Info($"Finished in {start.ElapsedMilliseconds}ms, Success: {response.Success}, ErrorId: {response.ErrorId}, Error: {response.ErrorMessage}, Cooldown: {response.CooldownMilliseconds}, Debounce: {response.ShouldDebounce}");
            return response;
        }
    }
}
