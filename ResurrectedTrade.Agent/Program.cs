using System;
using System.Diagnostics;
using System.IO;
#if OFFICIAL_BUILD
using System.Reflection;
#endif
using System.Windows.Forms;
using ResurrectedTrade.AgentBase;

namespace ResurrectedTrade.Agent
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var logger = new Logger(50000);
#if OFFICIAL_BUILD
            logger.Info("Official build");
#else
            logger.Info("Unofficial build");
#endif
#if SELF_CONTAINED
            logger.Info("Self contained build");
#else
            logger.Info("Framework dependant build");
#endif
            var currentProcessLocation = Process.GetCurrentProcess().MainModule.FileName;
            if (currentProcessLocation.EndsWith(".update.exe"))
            {
                logger.Info("Start of update sequence");
                if (!Utils.SingleInstanceMutex.WaitOne(60000, false))
                {
                    UIUtils.ShowError("Update failed", "Timed out waiting for parent process to exit");
                    return;
                }

                File.Copy(currentProcessLocation, currentProcessLocation.Replace(".update.exe", ".exe"), true);
                Process.Start(currentProcessLocation.Replace(".update.exe", ".exe"));
                return;
            }

            var previousUpdateFile = currentProcessLocation.Replace(".exe", ".update.exe");
            if (File.Exists(previousUpdateFile))
            {
                logger.Info("Previous update file exists");
                if (!Utils.SingleInstanceMutex.WaitOne(60000, false))
                {
                    UIUtils.ShowError("Update failed", "Timed out waiting for parent process to exit");
                    return;
                }

                try
                {
                    File.Delete(previousUpdateFile);
                }
                catch (Exception e)
                {
                    logger.Info($"Failed to delete previous update file: {e}");
                }
            }

            var installState = MaybePerformInstall(logger, currentProcessLocation);
            logger.Info($"Installation state: {installState}");
            if (installState == InstallState.Installed)
            {
                return;
            }

            if (!Utils.SingleInstanceMutex.WaitOne(0, false))
            {
                UIUtils.ShowError("Resurrected Trade", "Another instance is already running");
                return;
            }

            using (var entrypoint = new Entrypoint(currentProcessLocation, logger, installState == InstallState.AlreadyInstalled))
            {
                async void IdleHandler(object sender, EventArgs e)
                {
                    Application.Idle -= IdleHandler;
                    await entrypoint.Run();
                    Application.ExitThread();
                }q

                Application.Idle += IdleHandler;
                Application.Run();
                logger.Info("Exiting...");
            }
        }

        private enum InstallState
        {
            AlreadyInstalled,
            Installed,
            NotInstalled
        }

        private static InstallState MaybePerformInstall(Logger logger, string currentProcessLocation)
        {
#if OFFICIAL_BUILD
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var installDirectory = Path.Join(appData, "Resurrected Trade");
            var installLocation = Path.Join(installDirectory, "Resurrected Trade.exe");
            logger.Info($"Current process: {currentProcessLocation}");
            logger.Info($"Expected installation directory: {installLocation}");
            var assembly = Assembly.GetAssembly(typeof(Entrypoint));
            var ourVersion = assembly?.GetName().Version;
            if (currentProcessLocation != installLocation)
            {
                try
                {
                    Directory.CreateDirectory(installDirectory);
                    var existingNewerOrEqual = false;
                    var exists = File.Exists(installLocation);
                    if (exists)
                    {
                        logger.Info("Installation already exists.");
                        try
                        {
                            var existingVersion = FileVersionInfo.GetVersionInfo(installLocation);
                            var parsedExistingVersion = Version.Parse(existingVersion.FileVersion);
                            logger.Info($"Existing installed version: {parsedExistingVersion}");
                            existingNewerOrEqual = parsedExistingVersion >= ourVersion;
                        }
                        catch (Exception e)
                        {
                            logger.Info($"Failed to check existing installation version: {e}");
                        }
                    }

                    if (!existingNewerOrEqual)
                    {
                        if (exists) {
                            logger.Info("Overwriting existing installation");
                        }
                        else
                        {
                            logger.Info($"Installing to {installLocation}");
                        }
                        File.Copy(currentProcessLocation, installLocation, true);
                    }

                    Process.Start(installLocation);
                    return InstallState.Installed;
                }
                catch (Exception e)
                {
                    UIUtils.ShowException("Installing", e);
                    return InstallState.NotInstalled;
                }
            }

            if (currentProcessLocation == installLocation)
            {
                return InstallState.AlreadyInstalled;
            }
#endif
            return InstallState.NotInstalled;
        }
    }
}
