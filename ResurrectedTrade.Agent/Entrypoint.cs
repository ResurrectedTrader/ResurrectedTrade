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
using System.Linq;
using System.Net.Http;
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
        private readonly ManualResetEvent _event = new ManualResetEvent(false);

        private readonly Icon _whiteIcon;
        private readonly Icon _redIcon;
        private readonly Icon _greenIcon;
        private readonly Icon _blueIcon;

        private readonly Logger _logger;

        private void SpinSleep(int millis)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(millis);
            while (!_event.WaitOne(0) && deadline > DateTime.UtcNow)
            {
                Application.DoEvents();
            }

            _event.Reset();
        }

        public Entrypoint()
        {
            _logger = new Logger(50000);
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
            _notifyIcon.Icon = _whiteIcon;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (_, args) => { Utils.OpenUrl("https://resurrected.trade/overview"); };
            _notifyIcon.MouseMove += (_, args) => { UpdateTrayIcon(); };
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
            if (_state == State.Ok && _profile != null)
            {
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem(_profile.UserId) { Enabled = false },
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
                        new ToolStripMenuItem("Log out", null, (sender, args) => Logout()),
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Version")
                        {
                            DropDownItems = { new ToolStripMenuItem(_version) { Enabled = false } }
                        },
                        new ToolStripMenuItem("Show logs", null, (sender, args) => ShowLogs()),
                        new ToolStripMenuItem("Exit", null, (sender, args) => Exit()),
                    },
                };
            }
            else if (_state == State.NeedsAuth)
            {
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip
                {
                    Items =
                    {
                        new ToolStripMenuItem("Log in", null, (sender, args) => Login()),
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Version")
                        {
                            DropDownItems = { new ToolStripMenuItem(_version) { Enabled = false } }
                        },
                        new ToolStripMenuItem("Show logs", null, (sender, args) => ShowLogs()),
                        new ToolStripMenuItem("Exit", null, (sender, args) => Exit()),
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
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Version")
                        {
                            DropDownItems = { new ToolStripMenuItem(_version) { Enabled = false } }
                        },
                        new ToolStripMenuItem("Show logs", null, (sender, args) => ShowLogs()),
                        new ToolStripMenuItem("Exit", null, (sender, args) => Exit()),
                    }
                };
            }
        }

        private void ShowLogs()
        {
            new LogViewer(_whiteIcon, _logger).Show();
        }

        private void Exit()
        {
            _keepRunning = false;
            Application.Exit();
            _event.Set();
        }

        private void Spin(Task task)
        {
            while (!task.IsCompleted)
            {
                Application.DoEvents();
            }
        }

        private T Spin<T>(Task<T> task)
        {
            while (!task.IsCompleted)
            {
                Application.DoEvents();
            }

            return task.Result;
        }

        private State TryFetchProfile()
        {
            if (Utils.HasValidCookie(_cookieContainer))
            {
                try
                {
                    _profile = Spin(_profileService.GetProfileAsync(new Empty()).ResponseAsync);
                    Utils.SaveCookieContainer(_cookieContainer);
                    return State.Ok;
                }
                catch (Exception e)
                {
                    if (Utils.IsStatusCodeException(e, StatusCode.Unavailable))
                    {
                        return State.Down;
                    }

                    if (Utils.IsStatusCodeException(e, StatusCode.Unauthenticated))
                    {
                        return State.NeedsAuth;
                    }

                    throw;
                }
            }

            return State.NeedsAuth;
        }

        private void WaitForProfile()
        {
            _logger.Info($"Entering profile lookup loop in state: {_state}");
            while (_keepRunning && _profile == null)
            {
                var newState = TryFetchProfile();
                if (newState == State.Ok && _profile != null)
                {
                    _logger.Info("Retreived profile");
                    _state = newState;
                    return;
                }

                if (_state != newState && newState == State.NeedsAuth)
                {
                    _notifyIcon.ShowBalloonTip(
                        30, "Resurrected Trade", "You need to log in again...", ToolTipIcon.None
                    );
                }

                _logger.Info($"Current state is {newState}");
                _state = newState;
                SpinSleep(30000);
            }
        }

        public void Run()
        {
            CheckForUpdates();
            _state = State.Ok;
            try
            {
                _state = TryFetchProfile();
                if (_state == State.NeedsAuth)
                {
                    var result = Login();
                    while (result == DialogResult.Retry)
                    {
                        result = Login();
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
                else if (_state == State.Down)
                {
                    UIUtils.ShowError(
                        "Resurrected Trade", "Seems resurrected.trade is down at the moment, we will try later..."
                    );
                    _notifyIcon.Icon = _redIcon;
                }
                else
                {
                    _state = State.Ok;
                }
            }
            catch (Exception e)
            {
                _notifyIcon.Icon = _redIcon;
                UIUtils.ShowException("Failed to fetch profile", e);
            }

            while (_keepRunning)
            {
                if (_profile == null)
                {
                    WaitForProfile();
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
                    DoExports();
                }
                catch (Exception e)
                {
                    _logger.Info($"Failed to get exports: {e}");
                    SpinSleep(1000);
                }
            }
        }

        private void CheckForUpdates()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = Spin(
                        client.GetStringAsync("https://resurrected.trade/downloads/ResurrectedTrade.exe.latest.txt")
                    ).Trim().TrimEnd('\r', '\n').Trim();
                    var latestVersion = new Version(result);
                    var myVersion = GetType().Assembly.GetName().Version;
                    _logger.Info($"Latest available version: {latestVersion}, our version {myVersion}, newer: {latestVersion > myVersion}");
                    if (latestVersion > myVersion)
                    {
                        void ClickHandler(object sender, EventArgs args)
                        {
                            var uri = $"https://resurrected.trade/downloads/ResurrectedTrade-{latestVersion}.exe";
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

                        _notifyIcon.ShowBalloonTip(
                            30, "Resurrected Trade", "A new version is available, click here to download",
                            ToolTipIcon.None
                        );
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Info($"Failed to check for updates: {e}");
            }
        }

        private State DoExports()
        {
            var runner = new Runner(Offsets.Instance, _logger, _agentService);

            try
            {
                _logger.Info("Initializing runner...");
                var task = Task.Run(() => runner.Initialize());
                if (!task.IsCompletedSuccessfully)
                {
                    task.GetAwaiter().GetResult();
                }

                _logger.Info($"Initialized... {task.Exception}");
            }
            catch (Exception e)
            {
                _logger.Info($"Failed to initialize: {e}");
                if (Utils.IsStatusCodeException(e, StatusCode.Unavailable))
                {
                    _notifyIcon.Icon = _redIcon;
                    _profile = null;
                    return State.Down;
                }

                if (Utils.IsStatusCodeException(e, StatusCode.Unauthenticated))
                {
                    _notifyIcon.Icon = _redIcon;
                    _profile = null;
                    return State.NeedsAuth;
                }

                UIUtils.ShowException("Failed to initialize", e);
            }

            _logger.Info($"Entering export loop in state: {_state}");
            while (_keepRunning && _profile != null)
            {
                var any = false;
                var failed = false;
                var cooldown = 1000;
                if (_paused)
                {
                    _notifyIcon.Icon = _blueIcon;
                }
                else
                {
                    foreach (Process process in Process.GetProcessesByName("D2R"))
                    {
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

                            var response = runner.SubmitExport(export);
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
                            failed = true;

                            if (Utils.IsStatusCodeException(e, StatusCode.Unavailable))
                            {
                                _notifyIcon.Icon = _redIcon;
                                _profile = null;
                                return State.Down;
                            }

                            if (Utils.IsStatusCodeException(e, StatusCode.Unauthenticated))
                            {
                                _notifyIcon.Icon = _redIcon;
                                _profile = null;
                                return State.NeedsAuth;
                            }

                            _logger.Info(e.ToString());
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
                }

                SpinSleep(cooldown);
            }

            return _state;
        }

        private void Logout()
        {
            _logger.Info("Logging out...");
            Spin(Task.Run(() => _profileService.Logout(new Empty())));
            _profile = null;
            _state = State.NeedsAuth;
            _event.Set();
            Utils.SaveCookieContainer(_cookieContainer);
        }

        private DialogResult Login()
        {
            var prompt = new LoginPrompt(_profileService);
            var outcome = prompt.ShowDialog();
            if (outcome == DialogResult.Cancel)
            {
                return outcome;
            }

            _logger.Info("Successfully logged in");
            Utils.SaveCookieContainer(_cookieContainer);

            try
            {
                _profile = Spin(Task.Run(() => _profileService.GetProfile(new Empty())));
                _logger.Info("Successfully retrieved profile...");
                _state = State.Ok;
                _event.Set();
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
