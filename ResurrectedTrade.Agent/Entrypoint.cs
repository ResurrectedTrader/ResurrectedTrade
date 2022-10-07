using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Win32;
using ResurrectedTrade.Protocol.Agent;
using ResurrectedTrade.Protocol.Profile;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ResurrectedTrade.AgentBase;

namespace ResurrectedTrade.Agent
{
    public enum State
    {
        Ok,
        Down,
        NeedsAuth
    }

    public class Entrypoint : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private Profile _profile;
        private State _state;
        private bool _keepRunning = true;
        private bool _paused;

        private readonly CookieContainer _cookieContainer;
        private readonly AgentService.AgentServiceClient _agentService;
        private readonly ProfileService.ProfileServiceClient _profileService;

        private readonly string _version;
        private CancellationTokenSource _sleepCancellation = new CancellationTokenSource();

        private static readonly RegistryKey RunKey = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true
        );

        private readonly Icon _whiteIcon;
        private readonly Icon _redIcon;
        private readonly Icon _greenIcon;
        private readonly Icon _blueIcon;
        private readonly Icon _blackIcon;

        private readonly Logger _logger;
        private readonly bool _installed;
        private readonly LogViewer _logViewer;
        private readonly string _currentProcessLocation;

        private State State
        {
            get => _state;
            set
            {
                _state = value;
                UpdateTrayIcon();
            }
        }

        private Profile Profile
        {
            get => _profile;
            set
            {
                _profile = value;
                UpdateTrayIcon();
            }
        }

        public Entrypoint(string currentProcessLocation, Logger logger, bool installed)
        {
            _currentProcessLocation = currentProcessLocation;
            _logger = logger;
            _installed = installed;
            var assembly = Assembly.GetAssembly(typeof(Entrypoint));
            _version = assembly?.GetName().Version?.ToString() ?? "Unknown";
            _notifyIcon = new NotifyIcon();
            _whiteIcon = new Icon(
                assembly?.GetManifestResourceStream("ResurrectedTrade.Agent.logo.ico") ??
                throw new InvalidOperationException("Icon not found.")
            );
            _redIcon = _whiteIcon.ScaleColor(1, 0, 0);
            _greenIcon = _whiteIcon.ScaleColor(0, 1, 0);
            _blueIcon = _whiteIcon.ScaleColor(0, 0, 1);
            _blackIcon = _whiteIcon.ScaleColor(0, 0, 0);
            _logViewer = new LogViewer(_blackIcon, _logger);
            _notifyIcon.Icon = _whiteIcon;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (_, args) => { Utils.OpenUrl("https://resurrected.trade/overview"); };
            try
            {
                _cookieContainer = Utils.LoadCookieContainer();
                if (_cookieContainer.Count != 0)
                {
                    _logger.Info("Reusing existing cookie container...");
                }
                else
                {
                    _logger.Info("Using new cookie container...");
                }
            }
            catch (Exception exception)
            {
                _logger.Info($"Failed to load cookie container: {exception}");
            }


            var handler = new HttpClientHandler { UseCookies = true, CookieContainer = _cookieContainer, };
#if DEBUG
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    new ProductHeaderValue(
                        (assembly.GetName().Name ?? "Unknown").Replace(" ", ""),
                        _version
                    )
                )
            );

            var channel = GrpcChannel.ForAddress(
                Utils.ApiAddress,
                new GrpcChannelOptions
                {
                    HttpClient = client,
                    MaxRetryAttempts = 3,
                }
            );
            _agentService = new AgentService.AgentServiceClient(channel);
            _profileService = new ProfileService.ProfileServiceClient(channel);
            UpdateTrayIcon();
        }

        private void UpdateTrayIcon()
        {
            if (State == State.Ok && Profile != null)
            {
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem(Profile.UserId) { Enabled = false },
                        new ToolStripSeparator(),
                        new ToolStripMenuItem(
                            "Pause", null, (sender, args) =>
                            {
                                _paused = !_paused;
                                ((ToolStripMenuItem)sender).Checked = _paused;
                                if (_paused)
                                {
                                    _logger.Info("Pausing");
                                    _notifyIcon.Icon = _blueIcon;
                                }
                                else
                                {
                                    _logger.Info("Unpausing");
                                    _notifyIcon.Icon = _whiteIcon;
                                }
                            }
                        ) { Checked = _paused, },
                        new ToolStripMenuItem("Log out", null, (sender, args) => _ = Logout()),
                    },
                };
            }
            else if (State == State.NeedsAuth)
            {
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem("Log in", null, (sender, args) => _ = Login()),
                    }
                };
            }
            else
            {
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem("Service unavailable") { Enabled = false },
                    }
                };
            }

            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            var versionSubMenu = new ToolStripMenuItem("Version")
            {
                DropDownItems = { new ToolStripMenuItem(_version) { Enabled = false } }
            };
            _notifyIcon.ContextMenuStrip.Items.Add(versionSubMenu);

            if (_installed)
            {
                versionSubMenu.DropDownItems.Add(
                    new ToolStripMenuItem(
                        "Automatic updates", null, (sender, args) =>
                        {
                            var checkbox = sender as ToolStripMenuItem;
                            checkbox.Checked = !checkbox.Checked;

                            Utils.AgentRegistryKey.SetValue(
                                "AUTOMATIC_UPDATES", checkbox.Checked ? 1 : 0, RegistryValueKind.DWord
                            );
                        }
                    ) { Checked = (int)Utils.AgentRegistryKey.GetValue("AUTOMATIC_UPDATES", -1) == 1, }
                );
                _notifyIcon.ContextMenuStrip.Items.Add(
                    new ToolStripMenuItem(
                        "Start with Windows", null, (sender, args) =>
                        {
                            var checkbox = sender as ToolStripMenuItem;
                            checkbox.Checked = !checkbox.Checked;
                            var existing = (string)RunKey.GetValue("Resurrected Trade", null);
                            if (checkbox.Checked)
                            {
                                if (existing == null)
                                {
                                    RunKey.SetValue(
                                        "Resurrected Trade", _currentProcessLocation
                                    );
                                }
                            }
                            else
                            {
                                if (existing != null)
                                {
                                    RunKey.DeleteValue("Resurrected Trade");
                                }
                            }
                        }
                    ) { Checked = (string)RunKey.GetValue("Resurrected Trade", null) != null, }
                );
            }

            _notifyIcon.ContextMenuStrip.Items.Add(
                new ToolStripMenuItem("Show logs", null, (sender, args) => ShowLogs())
            );
            _notifyIcon.ContextMenuStrip.Items.Add(
                new ToolStripMenuItem("Exit", null, (sender, args) => Exit())
            );
        }

        private void ShowLogs()
        {
            if (_logViewer.Visible)
            {
                _logViewer.Activate();
            }
            else
            {
                _logViewer.Show();
            }
        }

        private void Exit()
        {
            _keepRunning = false;
            Application.Exit();
            _sleepCancellation.Cancel();
        }

        private async Task<State> TryFetchProfile()
        {
            if (Utils.HasValidCookie(_cookieContainer))
            {
                try
                {
                    Profile = await _profileService.GetProfileAsync(new Empty());
                    Utils.SaveCookieContainer(_cookieContainer);
                    return State.Ok;
                }
                catch (Exception e)
                {
                    var newState = HandleException(e);
                    if (newState != null)
                    {
                        return newState.Value;
                    }

                    _logger.Info($"Failed to fetch profile: {e}");
                }
            }

            return State.NeedsAuth;
        }

        private async Task WaitForProfile()
        {
            _logger.Info($"Entering profile lookup loop in state: {State}");
            while (_keepRunning && Profile == null)
            {
                var newState = await TryFetchProfile();
                if (newState == State.Ok && Profile != null)
                {
                    _logger.Info("Retreived profile");
                    State = newState;
                    return;
                }

                if (State != newState && newState == State.NeedsAuth)
                {
                    _notifyIcon.ShowBalloonTip(
                        30, "Resurrected Trade", "You need to log in again...", ToolTipIcon.None
                    );
                }

                _logger.Info($"Current state is {newState}");
                State = newState;
                await InterruptableSleep(30000);
            }
        }


        private void ResetSleepCancellation()
        {
            _sleepCancellation.Dispose();
            _sleepCancellation = new CancellationTokenSource();
        }

        public async Task Run()
        {
#if OFFICIAL_BUILD
            var automaticUpdates = (int)Utils.AgentRegistryKey.GetValue("AUTOMATIC_UPDATES", -1);
#else
            var automaticUpdates = 0;
#endif

            if (automaticUpdates == -1)
            {
                DialogResult result = MessageBox.Show(
                    "Would you like to enable automatic updates?",
                    "Automatic Updates", MessageBoxButtons.YesNo
                );
                if (result == DialogResult.Yes)
                {
                    Utils.AgentRegistryKey.SetValue("AUTOMATIC_UPDATES", 1, RegistryValueKind.DWord);
                    automaticUpdates = 1;
                }
                else if (result == DialogResult.No)
                {
                    Utils.AgentRegistryKey.SetValue("AUTOMATIC_UPDATES", 0, RegistryValueKind.DWord);
                    automaticUpdates = 0;
                }
            }

            if (await CheckForUpdates(automaticUpdates == 1))
            {
                return;
            }

            State = State.Ok;
            try
            {
                State = await TryFetchProfile();
                if (State == State.NeedsAuth)
                {
                    var result = await Login();
                    while (result == DialogResult.Retry)
                    {
                        result = await Login();
                    }

                    if (result == DialogResult.OK)
                    {
                        _notifyIcon.Icon = _whiteIcon;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (State == State.Down)
                {
                    UIUtils.ShowError(
                        "Resurrected Trade", "Seems resurrected.trade is down at the moment, we will try later..."
                    );
                    _notifyIcon.Icon = _redIcon;
                }
                else
                {
                    State = State.Ok;
                }
            }
            catch (Exception e)
            {
                _notifyIcon.Icon = _redIcon;
                UIUtils.ShowException("Failed to fetch profile", e);
            }

            while (_keepRunning)
            {
                if (Profile == null)
                {
                    await WaitForProfile();
                    continue;
                }

                var version = (string)Utils.AgentRegistryKey.GetValue("VERSION", null);
                if (version == null)
                {
                    // First login.
                    _notifyIcon.ShowBalloonTip(
                        1000, "Resurrected Trade", "I am here if you need me...", ToolTipIcon.None
                    );
                }

                Utils.AgentRegistryKey.SetValue("VERSION", _version, RegistryValueKind.String);

                try
                {
                    await DoExports();
                }
                catch (Exception e)
                {
                    _logger.Info($"Failed to get exports: {e}");
                    await InterruptableSleep(1000);
                }
            }
        }

        private async Task InterruptableSleep(int duration)
        {
            try
            {
                await Task.Delay(duration, _sleepCancellation.Token);
            }
            catch (TaskCanceledException)
            {
                ResetSleepCancellation();
            }
            catch (OperationCanceledException)
            {
                ResetSleepCancellation();
            }
        }

        internal class GithubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }
        }

        private async Task<bool> CheckForUpdates(bool automaticUpdatesEnabled)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue("ResurrectedTradeAgent", _version)
                    );
                    var result = await
                        client.GetStringAsync(
                            "https://api.github.com/repos/ResurrectedTrader/ResurrectedTrade/releases/latest"
                        );
                    var release = JsonSerializer.Deserialize<GithubRelease>(result);
                    var latestVersion = new Version(release.TagName.TrimStart('v'));
                    var myVersion = GetType().Assembly.GetName().Version;
                    _logger.Info(
                        $"Latest available version: {latestVersion}, our version {myVersion}, newer: {latestVersion > myVersion}"
                    );
                    if (latestVersion > myVersion)
                    {
#if SELF_CONTAINED
                        var uri =
                            $"https://github.com/ResurrectedTrader/ResurrectedTrade/releases/download/v{latestVersion}/ResurrectedTrade-SelfContained.exe";
#else
                        var uri =
                            $"https://github.com/ResurrectedTrader/ResurrectedTrade/releases/download/v{latestVersion}/ResurrectedTrade-FrameworkDependant.exe";
#endif
                        if (automaticUpdatesEnabled)
                        {
                            return await PerformAutomaticUpgrade(client, uri, latestVersion);
                        }

                        NotifyAboutUpdate(uri);

                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Info($"Failed to check for updates: {e}");
            }

            return false;
        }

        private void NotifyAboutUpdate(string uri)
        {
            void ClickHandler(object sender, EventArgs args)
            {
                _logger.Info($"Downloading {uri}");
                Utils.OpenUrl(uri);
                _notifyIcon.BalloonTipClicked -= ClickHandler;
            }

            void ClosedHandler(object sender, EventArgs args)
            {
                _notifyIcon.BalloonTipClicked -= ClickHandler;
                _notifyIcon.BalloonTipClosed -= ClosedHandler;
            }

            _notifyIcon.BalloonTipClicked += ClickHandler;
            _notifyIcon.BalloonTipClosed += ClosedHandler;
#if OFFICIAL_BUILD
            _notifyIcon.ShowBalloonTip(
                30, "Resurrected Trade", "A new version is available, click here to download",
                ToolTipIcon.None
            );
#else
            _logger.Info("Skipping update prompt as debug or unofficial build");
#endif
        }

        private async Task<bool> PerformAutomaticUpgrade(HttpClient client, string uri, Version version)
        {
            _notifyIcon.ShowBalloonTip(
                10, "Resurrected Trade", $"Upgrading to version {version}",
                ToolTipIcon.None
            );
            try
            {
                var newName = _currentProcessLocation.Replace(".exe", ".update.exe");
                File.Delete(newName);
                var stream = await client.GetStreamAsync(uri);
                using (var fileStream = File.Create(newName))
                {
                    await stream.CopyToAsync(fileStream);
                }
                _logger.Info($"Starting {newName}");
                Process.Start(newName);
                return true;
            }
            catch (Exception e)
            {
                _logger.Info($"Failed to perform automatic upgrade: {e}");
                UIUtils.ShowException("Update failed", e);
            }

            return false;
        }

        private async Task<State> DoExports()
        {
            var runner = new Runner(Offsets.Instance, _logger, _agentService);

            try
            {
                _logger.Info("Initializing runner...");
                await runner.Initialize();
            }
            catch (Exception e)
            {
                _logger.Info($"Failed to initialize: {e}");
                var newState = HandleException(e);
                if (newState != null)
                {
                    return newState.Value;
                }

                UIUtils.ShowException("Failed to initialize", e);
                throw;
            }

            _logger.Info($"Entering export loop in state: {State}");
            while (_keepRunning && Profile != null)
            {
                var any = false;
                var failed = false;
                var cooldown = 1000;
                if (_paused)
                {
                    _notifyIcon.Icon = _blueIcon;
                    await InterruptableSleep(cooldown);
                    continue;
                }

                foreach (Process process in Process.GetProcesses())
                {
                    if (!process.ProcessName.StartsWith("D2R")) continue;
                    var buildNumber = process.MainModule?.FileVersionInfo.FileVersion?.Split('.').Last() ?? "-1";
                    if (!int.TryParse(buildNumber, out var version))
                    {
                        continue;
                    }

                    if (version != Offsets.Instance.SupportedVersion)
                    {
                        _logger.Info($"Unsupported version: {version}");
                        continue;
                    }

                    try
                    {
                        var export = runner.GetExport(process);
                        if (export == null)
                        {
                            continue;
                        }

                        var response = await runner.SubmitExport(export);
                        if (response.Attempted)
                        {
                            cooldown = Math.Max(cooldown, response.CooldownMilliseconds);
                            any = true;
                            if (!response.Success)
                            {
                                failed = true;
                            }

                            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                            {
                                _logger.Info($"Failed: {response.ErrorMessage} {response.ErrorId}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!process.HasExited)
                        {
                            failed = true;

                            HandleException(e);

                            _logger.Info(e.ToString());
                        }
                    }
                }

                if (any)
                {
                    Utils.SaveCookieContainer(_cookieContainer);
                }

                if (failed)
                {
                    _notifyIcon.Icon = _redIcon;
                    // Refetch manifests...
                    break;
                }

                if (any)
                {
                    _notifyIcon.Icon = _greenIcon;
                }
                else
                {
                    _notifyIcon.Icon = _whiteIcon;
                }

                await InterruptableSleep(cooldown);
            }

            return State;
        }

        private async Task Logout()
        {
            _logger.Info("Logging out...");
            try
            {
                await _profileService.LogoutAsync(new Empty());
                Profile = null;
                State = State.NeedsAuth;
            }
            catch (Exception e)
            {
                var newState = HandleException(e);
                if (newState == null)
                {
                    UIUtils.ShowException("Logout failed", e);
                }
                else
                {
                    State = newState.Value;
                }
            }

            _sleepCancellation.Cancel();
            Utils.SaveCookieContainer(_cookieContainer);
        }

        private State? HandleException(Exception e)
        {
            if (Utils.IsStatusCodeException(e, StatusCode.Unavailable))
            {
                _notifyIcon.Icon = _redIcon;
                Profile = null;
                return State.Down;
            }

            if (Utils.IsStatusCodeException(e, StatusCode.Unauthenticated))
            {
                _notifyIcon.Icon = _redIcon;
                Profile = null;
                return State.NeedsAuth;
            }

            return null;
        }

        private async Task<DialogResult> Login()
        {
            var prompt = new LoginPrompt(_blackIcon, _profileService);
            var outcome = prompt.ShowDialog();
            if (outcome == DialogResult.Cancel)
            {
                return outcome;
            }

            _logger.Info("Successfully logged in");
            Utils.SaveCookieContainer(_cookieContainer);

            try
            {
                Profile = await _profileService.GetProfileAsync(new Empty());
                _logger.Info("Successfully retrieved profile...");
                State = State.Ok;
                _sleepCancellation.Cancel();
            }
            catch (Exception e)
            {
                UIUtils.ShowException("Failed to retrieve profile", e);
                return DialogResult.Retry;
            }

            return DialogResult.OK;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
