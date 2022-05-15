using System;
using System.Drawing;
using System.Windows.Forms;

namespace ResurrectedTrade.Agent
{
    public partial class LogViewer : Form
    {
        private readonly Logger _logger;

        public LogViewer(Icon icon, Logger logger)
        {
            _logger = logger;
            InitializeComponent();
            Icon = icon;
            logBox.ScrollBars = ScrollBars.Vertical;
        }

        protected override void OnShown(EventArgs e)
        {
            _logger.Changed += RefreshLogs;
            RefreshLogs(null, null);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            _logger.Changed -= RefreshLogs;
        }

        private void RefreshLogs(object sender, EventArgs e)
        {
            logBox.Text = _logger.GetBufferContent();
            if (logBox.SelectedText.Length == 0)
            {
                logBox.SelectionStart = logBox.Text.Length;
                logBox.ScrollToCaret();
            }
        }
    }
}
