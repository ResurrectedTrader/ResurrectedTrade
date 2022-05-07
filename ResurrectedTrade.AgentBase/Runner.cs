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
    public class RunOutcome
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
            _logger.Log($"Got remote manifests: {remoteManifests.Count}");
            _manifests = remoteManifests;
        }

        public bool IsInitialized()
        {
            return _manifests != null;
        }

        public RunOutcome RunForHandle(Ptr handle, Ptr baseAddress)
        {
            return RunWithAccess(new RemoteMemoryAccess(handle, baseAddress));
        }

        public RunOutcome RunForProcess(Process process)
        {
            using (var access = new RemoteMemoryAccess(process))
            {
                return RunWithAccess(access);
            }
        }

        private RunOutcome RunWithAccess(MemoryAccess access)
        {
            if (_manifests == null) throw new ApplicationException("No manifests found");

            var sessionData = access.Read<D2SessionData>(access.BaseAddress + _offsets.SessionData);
            string battleTag = Encoding.UTF8.GetString(
                access.Read<byte>(sessionData.pBattleTagStr, (uint)sessionData.BattleTagLength)
            );

            // Local chars go under '_' battle tag, which gets replaced service side.
            var isOnline = access.Read<bool>(access.BaseAddress + _offsets.IsOnlineGame);
            if (!isOnline)
            {
                battleTag = "_";
            }

            var capture = new Capture(_offsets, _logger, access);

            _manifests.TryGetValue(battleTag, out var manifest);

            Export accountExport = capture.GetExport(manifest, isOnline: isOnline);
            if (accountExport != null)
            {
                _logger.Log($"Export {accountExport} {battleTag}");
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
                switch (region)
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

                switch (locale)
                {
                    case "enUS":
                        accountExport.Locale = Locale.EnUs;
                        break;
                    case "zhTW":
                        accountExport.Locale = Locale.ZhTw;
                        break;
                    case "deDE":
                        accountExport.Locale = Locale.DeDe;
                        break;
                    case "esES":
                        accountExport.Locale = Locale.EsEs;
                        break;
                    case "frFR":
                        accountExport.Locale = Locale.FrFr;
                        break;
                    case "itIT":
                        accountExport.Locale = Locale.ItIt;
                        break;
                    case "koKR":
                        accountExport.Locale = Locale.KoKr;
                        break;
                    case "plPL":
                        accountExport.Locale = Locale.PlPl;
                        break;
                    case "esMX":
                        accountExport.Locale = Locale.EsMx;
                        break;
                    case "jaJP":
                        accountExport.Locale = Locale.JaJp;
                        break;
                    case "ptBR":
                        accountExport.Locale = Locale.PtBr;
                        break;
                    case "ruRU":
                        accountExport.Locale = Locale.RuRu;
                        break;
                    case "zhCN":
                        accountExport.Locale = Locale.ZhCn;
                        break;
                    default:
                        accountExport.Locale = Locale.Undefined;
                        break;
                }

                var accountExportHash = accountExport.Hash();
                if ((!_previousHash.TryGetValue(accountExport.BattleTag, out int prevHash) ||
                     prevHash != accountExportHash) && _debounce)
                {
                    _logger.Log("Debouncing...");
                    _previousHash[accountExport.BattleTag] = accountExportHash;
                    return new RunOutcome
                    {
                        Attempted = false
                    };
                }

                _logger.Log("Exporting...");
                var response = SendExport(accountExport);
                _logger.Log($"Got manifest: {response.NewManifest}");
                _manifests[battleTag] = response.NewManifest;
                _debounce = response.ShouldDebounce;

                return new RunOutcome
                {
                    Attempted = true,
                    Success = response.Success,
                    ErrorId = response.ErrorId,
                    ErrorMessage = response.ErrorMessage,
                    CooldownMilliseconds = response.CooldownMilliseconds
                };
            }

            return new RunOutcome
            {
                Attempted = false
            };
        }

        private ExportResponse SendExport(Export export)
        {
            var start = Stopwatch.StartNew();
            _logger.Log($"Posting {export.BattleTag} {string.Join(" ", export.Characters.Select(o => o.Name))}");
            var response = _client.SubmitExport(export);
            _logger.Log($"Success in {start.ElapsedMilliseconds}ms");
            return response;
        }
    }
}
