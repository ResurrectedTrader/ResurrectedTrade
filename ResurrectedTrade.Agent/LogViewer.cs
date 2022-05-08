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
            _logger.Changed += RefreshLogs;
            InitializeComponent();
            Icon = icon;
            Closing += (sender, args) => { _logger.Changed -= RefreshLogs; };
            RefreshLogs(null, null);
            logBox.ScrollBars = ScrollBars.Vertical;
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
