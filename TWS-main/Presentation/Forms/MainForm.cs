using System;
using System.Drawing;
using System.Windows.Forms;
using TWS.Infrastructure.DependencyInjection;
using TWS.Infrastructure.Logging;
using TWS.Services;
using TWS.Services.Interfaces;

namespace TWS.Presentation.Forms
{
    public partial class MainForm : Form
    {
        private readonly IAuthenticationService _authService;
        private readonly IMarketDataService _marketDataService;
        private readonly IPredefinedMarketwatchService _predefinedMwService;
        private readonly ILogger _logger;
        private TabControl tabControl;
        private MenuStrip menuStrip;
        private ToolStripStatusLabel statusLabel;
        private MarketWatchForm _marketWatchForm;

        public MainForm(string authToken)
        {
            _authService = ServiceLocator.GetService<IAuthenticationService>();
            _marketDataService = ServiceLocator.GetService<IMarketDataService>();
            _predefinedMwService = ServiceLocator.GetService<IPredefinedMarketwatchService>();
            _logger = ServiceLocator.GetService<ILogger>();

            // ✅ REMOVED: Services automatically get token from IAuthenticationService
            // No need to manually set auth token anymore

            InitializeComponents();
            InitializeAsync(authToken);
        }

        private void InitializeComponents()
        {
            this.Text = "TWS - Trading Workspace";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.IsMdiContainer = true;

            // Menu Strip
            menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Application.Exit());

            var viewMenu = new ToolStripMenuItem("View");
            viewMenu.DropDownItems.Add("Market Watch", null, ShowMarketWatch);
            viewMenu.DropDownItems.Add("Order Book", null, ShowOrderBook);
            viewMenu.DropDownItems.Add("Positions", null, (s, e) => MessageBox.Show("Coming soon"));

            var toolsMenu = new ToolStripMenuItem("Tools");
            toolsMenu.DropDownItems.Add("Settings", null, (s, e) => MessageBox.Show("Coming soon"));
            toolsMenu.DropDownItems.Add("Logs", null, OpenLogs);

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About", null, ShowAbout);

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, toolsMenu, helpMenu });

            // Status Bar
            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            var timeLabel = new ToolStripStatusLabel(DateTime.Now.ToString("HH:mm:ss"));

            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            timer.Start();

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(new ToolStripSeparator());
            statusStrip.Items.Add(timeLabel);

            // Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(tabControl);
            this.Controls.Add(statusStrip);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }

        private async void InitializeAsync(string authToken)
        {
            try
            {
                statusLabel.Text = "Initializing...";

                // Connect to WebSocket
                await _marketDataService.ConnectAsync();
                statusLabel.Text = "Connected to market data";

                // Show Market Watch tab immediately
                ShowMarketWatch(null, null);

                _logger.LogInformation("MainForm initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing MainForm", ex);
                statusLabel.Text = "Initialization error";
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowOrderBook(object sender, EventArgs e)
        {
            var orderBookForm = new OrderBookForm();
            orderBookForm.Show();
        }

        private void ShowMarketWatch(object sender, EventArgs e)
        {
            // Check if Market Watch tab already exists
            foreach (TabPage tab in tabControl.TabPages)
            {
                if (tab.Text == "Market Watch")
                {
                    tabControl.SelectedTab = tab;
                    return;
                }
            }

            // Create new Market Watch tab
            var tabPage = new TabPage("Market Watch");

            _marketWatchForm = new MarketWatchForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };

            // Subscribe to scrip master loaded event
            _marketWatchForm.OnScripMasterLoaded += MarketWatchForm_OnScripMasterLoaded;

            tabPage.Controls.Add(_marketWatchForm);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            _marketWatchForm.Show();
            statusLabel.Text = "Market Watch loaded";
        }

        private void MarketWatchForm_OnScripMasterLoaded(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Scrip master loaded - updating lookup controls");
                statusLabel.Text = "Scrip master loaded - All features ready";

                // Update any open lookup controls or other forms that need scrip data
                UpdateAllScripDependentControls();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling scrip master loaded event", ex);
            }
        }

        private void UpdateAllScripDependentControls()
        {
            // Iterate through all forms and update scrip-dependent controls
            foreach (TabPage tab in tabControl.TabPages)
            {
                foreach (Control control in tab.Controls)
                {
                    if (control is Form form)
                    {
                        UpdateFormControls(form);
                    }
                }
            }
        }

        private void UpdateFormControls(Form form)
        {
            foreach (Control control in form.Controls)
            {
                // If it's a ScripLookupControl, refresh its data
                if (control is Controls.ScripLookupControl lookupControl)
                {
                    lookupControl.RefreshData();
                }

                // Recursively check child controls
                if (control.HasChildren)
                {
                    UpdateNestedControls(control);
                }
            }
        }

        private void UpdateNestedControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Controls.ScripLookupControl lookupControl)
                {
                    lookupControl.RefreshData();
                }

                if (control.HasChildren)
                {
                    UpdateNestedControls(control);
                }
            }
        }

        private void OpenLogs(object sender, EventArgs e)
        {
            try
            {
                var logPath = System.IO.Path.Combine(Application.StartupPath, "Logs");
                if (System.IO.Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logPath);
                }
                else
                {
                    MessageBox.Show("Log directory not found.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening logs", ex);
                MessageBox.Show($"Error opening logs: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show(
                "TWS - Trading Workspace\n" +
                "Version 2.0\n\n" +
                "A professional trading application\n" +
                "Built with clean architecture principles",
                "About TWS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // ✅ FIX 1: Changed from DisconnectAsync() to Disconnect() (synchronous)
                _marketDataService?.Disconnect();

                _logger.LogInformation("Application closing");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during application shutdown", ex);
            }

            base.OnFormClosing(e);
        }
    }
}