using System;
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
            if (!Utils.SingleInstanceMutex.WaitOne(0, false))
            {
                UIUtils.ShowError("Resurrected Trade", "Another instance is already running");
                return;
            }

            using (var entrypoint = new Entrypoint())
            {
                entrypoint.Run();
            }
        }
    }
}
