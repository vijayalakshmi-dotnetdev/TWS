using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TWS.Domain.Models;
using TWS.Infrastructure.DependencyInjection;
using TWS.Infrastructure.Logging;
using TWS.Services.Interfaces;

namespace TWS.Presentation.Controls
{
    /// <summary>
    /// Modern inline scrip search control with smart autocomplete
    /// Optimized for low latency and async operations
    /// </summary>
    public partial class ScripLookupControl : UserControl
    {
        private readonly IScripMasterService _scripMasterService;
        private readonly ILogger _logger;

        // UI Components
        private TextBox txtSearch;
        private ComboBox cmbExchange;
        private Panel pnlDropdown;
        private ListBox lstResults;
        private Label lblStatus;
        private PictureBox picSearchIcon;
        private PictureBox picClearIcon;

        // Data
        private List<Scrip> _allScrips;
        private CancellationTokenSource _searchCancellation;
        private System.Windows.Forms.Timer _debounceTimer;
        private bool _isDataLoaded = false;
        private bool _isDropdownVisible = false;

        // Configuration
        private const int DEBOUNCE_DELAY_MS = 300;
        private const int MAX_RESULTS = 15;
        private const int DROPDOWN_MAX_HEIGHT = 400;
        private const int DROPDOWN_ITEM_HEIGHT = 35;
        private const string PLACEHOLDER_TEXT = "Search scrip by symbol, name or token...";

        // 🎨 ODIN Diet Colors
        private static readonly Color BackgroundColor = Color.FromArgb(20, 20, 20);
        private static readonly Color TextBoxBackColor = Color.FromArgb(30, 30, 30);
        private static readonly Color TextColor = Color.FromArgb(220, 220, 220);
        private static readonly Color PlaceholderColor = Color.FromArgb(120, 120, 120);
        private static readonly Color BorderColor = Color.FromArgb(60, 60, 60);
        private static readonly Color HighlightColor = Color.FromArgb(45, 45, 48);
        private static readonly Color AccentColor = Color.FromArgb(100, 180, 255);

        // Events
        public event EventHandler<Scrip> ScripSelected;

        public ScripLookupControl()
        {
            _scripMasterService = ServiceLocator.GetService<IScripMasterService>();
            _logger = ServiceLocator.GetService<ILogger>();

            // ✅ Initialize timer FIRST (before components)
            InitializeDebounceTimer();
            InitializeComponents();
            LoadScripDataAsync();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(800, 45);
            this.BackColor = BackgroundColor;
            this.Padding = new Padding(10, 5, 10, 5);

            // 🔍 Search Icon
            picSearchIcon = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(15, 12),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = CreateSearchIcon(),
                Cursor = Cursors.Default
            };

            // 📝 Search TextBox
            txtSearch = new TextBox
            {
                Location = new Point(45, 10),
                Width = 400,
                Height = 28,
                Font = new Font("Segoe UI", 10F),
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            txtSearch.KeyDown += TxtSearch_KeyDown;
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
            SetPlaceholder(txtSearch, PLACEHOLDER_TEXT);

            // ❌ Clear Icon
            picClearIcon = new PictureBox
            {
                Size = new Size(16, 16),
                Location = new Point(448, 14),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = CreateClearIcon(),
                Cursor = Cursors.Hand,
                Visible = false
            };
            picClearIcon.Click += PicClearIcon_Click;

            // 📊 Exchange ComboBox
            var lblExchange = new Label
            {
                Text = "Exchange:",
                Location = new Point(480, 13),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F)
            };

            cmbExchange = new ComboBox
            {
                Location = new Point(550, 10),
                Width = 100,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            cmbExchange.Items.AddRange(new object[] { "ALL", "NSE", "BSE", "NFO", "MCX" });
            cmbExchange.SelectedIndex = 0;
            cmbExchange.SelectedIndexChanged += CmbExchange_SelectedIndexChanged;

            // ℹ️ Status Label
            lblStatus = new Label
            {
                Location = new Point(665, 13),
                AutoSize = true,
                ForeColor = PlaceholderColor,
                Font = new Font("Segoe UI", 8.5F),
                Text = "Loading..."
            };

            // 📋 Dropdown Panel (Will be added to parent form, not this control)
            pnlDropdown = new Panel
            {
                Width = 600,
                MaximumSize = new Size(600, DROPDOWN_MAX_HEIGHT),
                BackColor = TextBoxBackColor,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                AutoSize = false
            };

            // 📋 Results ListBox
            lstResults = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F),
                ItemHeight = DROPDOWN_ITEM_HEIGHT,
                DrawMode = DrawMode.OwnerDrawFixed,
                SelectionMode = SelectionMode.One
            };
            lstResults.DrawItem += LstResults_DrawItem;
            lstResults.Click += LstResults_Click;
            lstResults.KeyDown += LstResults_KeyDown;

            pnlDropdown.Controls.Add(lstResults);

            // Add controls to UserControl (except dropdown - it will be added to parent)
            this.Controls.AddRange(new Control[]
            {
                picSearchIcon,
                txtSearch,
                picClearIcon,
                lblExchange,
                cmbExchange,
                lblStatus
            });
        }

        private void InitializeDebounceTimer()
        {
            _debounceTimer = new System.Windows.Forms.Timer
            {
                Interval = DEBOUNCE_DELAY_MS
            };
            _debounceTimer.Tick += async (s, e) =>
            {
                _debounceTimer.Stop();
                await PerformSearchAsync();
            };
        }

        #region Parent Attachment

        /// <summary>
        /// ✅ Attach dropdown to parent form when control is added to form
        /// This prevents clipping by the UserControl boundaries
        /// </summary>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (this.Parent != null && pnlDropdown != null)
            {
                // Remove from current parent if any
                if (pnlDropdown.Parent != null)
                {
                    pnlDropdown.Parent.Controls.Remove(pnlDropdown);
                }

                // Add dropdown to parent form
                this.Parent.Controls.Add(pnlDropdown);
                pnlDropdown.BringToFront();

                // ✅ Subscribe to parent events
                this.Parent.Resize += Parent_Resize;
              //  this.Parent.Scroll += Parent_Scroll;
                this.Parent.Click += Parent_Click;

                _logger?.LogInformation("Dropdown panel attached to parent form");
            }
        }

        private void Parent_Resize(object sender, EventArgs e)
        {
            if (_isDropdownVisible)
            {
                pnlDropdown.Location = GetDropdownPosition();
            }
        }

        private void Parent_Scroll(object sender, ScrollEventArgs e)
        {
            if (_isDropdownVisible)
            {
                HideDropdown();
            }
        }

        private void Parent_Click(object sender, EventArgs e)
        {
            if (_isDropdownVisible)
            {
                HideDropdown();
            }
        }

        #endregion

        #region Data Loading

        private async void LoadScripDataAsync()
        {
            try
            {
                lblStatus.Text = "Loading scrips...";
                lblStatus.ForeColor = AccentColor;

                _allScrips = await _scripMasterService.GetAllScripsAsync();

                if (_allScrips != null && _allScrips.Any())
                {
                    _isDataLoaded = true;
                    lblStatus.Text = $"{_allScrips.Count:N0} scrips";
                    lblStatus.ForeColor = Color.FromArgb(0, 200, 83);
                    _logger.LogInformation($"Loaded {_allScrips.Count} scrips for lookup");
                }
                else
                {
                    lblStatus.Text = "No data";
                    lblStatus.ForeColor = Color.FromArgb(255, 90, 95);
                    _allScrips = new List<Scrip>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading scrip data", ex);
                lblStatus.Text = "Load error";
                lblStatus.ForeColor = Color.FromArgb(255, 90, 95);
                _allScrips = new List<Scrip>();
            }
        }

        public async void RefreshData()
        {
            try
            {
                lblStatus.Text = "Refreshing...";
                _allScrips = await _scripMasterService.GetAllScripsAsync();

                if (_allScrips != null && _allScrips.Any())
                {
                    _isDataLoaded = true;
                    lblStatus.Text = $"{_allScrips.Count:N0} scrips";
                    lblStatus.ForeColor = Color.FromArgb(0, 200, 83);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing scrip data", ex);
                lblStatus.Text = "Refresh error";
            }
        }

        #endregion

        #region Search Logic

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // ✅ Show/hide clear icon
            if (picClearIcon != null)
            {
                picClearIcon.Visible = !string.IsNullOrEmpty(txtSearch.Text) &&
                                       txtSearch.Text != PLACEHOLDER_TEXT;
            }

            // ✅ Check if timer is initialized
            if (_debounceTimer == null)
                return;

            // Restart debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void CmbExchange_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearch.Text) && txtSearch.Text != PLACEHOLDER_TEXT)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        private async Task PerformSearchAsync()
        {
            // Cancel any ongoing search
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                var searchText = txtSearch.Text.Trim();

                // ✅ Ignore if placeholder is showing
                if (string.IsNullOrEmpty(searchText) || searchText == PLACEHOLDER_TEXT)
                {
                    HideDropdown();
                    return;
                }

                // Ensure data is loaded
                if (!_isDataLoaded || _allScrips == null || !_allScrips.Any())
                {
                    HideDropdown();
                    return;
                }

                // ✅ CRITICAL: Capture exchange value on UI thread BEFORE Task.Run
                var selectedExchange = cmbExchange.SelectedItem?.ToString();

                // Perform search asynchronously
                var results = await Task.Run(() => SearchScrips(searchText, selectedExchange), token);

                // Check if cancelled
                if (token.IsCancellationRequested)
                    return;

                // Update UI on UI thread
                UpdateSearchResults(results);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError("Error performing search", ex);
            }
        }

        private List<Scrip> SearchScrips(string searchText, string selectedExchange)
        {
            var upperSearch = searchText.ToUpper();

            IEnumerable<Scrip> filtered = _allScrips;

            // Filter by exchange
            if (!string.IsNullOrEmpty(selectedExchange) && selectedExchange != "ALL")
            {
                filtered = filtered.Where(s =>
                    s.Exchange.Equals(selectedExchange, StringComparison.OrdinalIgnoreCase));
            }

            // 🎯 Smart search with ranking
            var results = filtered
                .Select(s => new
                {
                    Scrip = s,
                    Score = CalculateSearchScore(s, upperSearch)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(MAX_RESULTS)
                .Select(x => x.Scrip)
                .ToList();

            return results;
        }

        private int CalculateSearchScore(Scrip scrip, string search)
        {
            int score = 0;

            // Exact match on symbol (highest priority)
            if (scrip.Symbol.Equals(search, StringComparison.OrdinalIgnoreCase))
                return 1000;

            // Starts with match on symbol (high priority)
            if (scrip.Symbol.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                score += 500;

            // Contains in symbol
            if (scrip.Symbol.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 200;

            // Contains in name
            if (!string.IsNullOrEmpty(scrip.Name) &&
                scrip.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 100;

            // Contains in token
            if (scrip.Token.Contains(search))
                score += 50;

            return score;
        }

        private void UpdateSearchResults(List<Scrip> results)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateSearchResults(results)));
                return;
            }

            lstResults.BeginUpdate();
            lstResults.Items.Clear();

            if (results != null && results.Any())
            {
                foreach (var scrip in results)
                {
                    lstResults.Items.Add(scrip);
                }

                // Resize dropdown based on results
                int height = Math.Min(results.Count * DROPDOWN_ITEM_HEIGHT + 4, DROPDOWN_MAX_HEIGHT);
                pnlDropdown.Height = height;

                ShowDropdown();
            }
            else
            {
                HideDropdown();
            }

            lstResults.EndUpdate();
        }

        #endregion

        #region Dropdown Management

        /// <summary>
        /// ✅ Calculate dropdown position relative to parent form
        /// </summary>
        private Point GetDropdownPosition()
        {
            if (this.Parent == null)
                return new Point(45, 40);

            try
            {
                // Get search box position relative to parent form
                var searchBoxLocation = this.Parent.PointToClient(
                    txtSearch.Parent.PointToScreen(txtSearch.Location));

                int x = searchBoxLocation.X;
                int y = searchBoxLocation.Y + txtSearch.Height + 2;

                // ✅ Ensure dropdown doesn't go off screen
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    // Check if dropdown would extend beyond form width
                    if (x + pnlDropdown.Width > parentForm.ClientSize.Width)
                    {
                        x = parentForm.ClientSize.Width - pnlDropdown.Width - 10;
                    }

                    // Check if dropdown would extend beyond form height
                    if (y + pnlDropdown.Height > parentForm.ClientSize.Height)
                    {
                        // Show above search box instead
                        y = searchBoxLocation.Y - pnlDropdown.Height - 2;
                    }
                }

                return new Point(Math.Max(x, 5), Math.Max(y, 5));
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error calculating dropdown position", ex);
                return new Point(45, 40);
            }
        }

        private void ShowDropdown()
        {
            if (_isDropdownVisible) return;

            // ✅ Update position before showing
            pnlDropdown.Location = GetDropdownPosition();

            pnlDropdown.Visible = true;
            _isDropdownVisible = true;
            pnlDropdown.BringToFront();
        }

        private void HideDropdown()
        {
            if (!_isDropdownVisible) return;

            pnlDropdown.Visible = false;
            _isDropdownVisible = false;
            lstResults.Items.Clear();
        }

        #endregion

        #region Event Handlers

        private void LstResults_Click(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem is Scrip scrip)
            {
                SelectScrip(scrip);
            }
        }

        private void LstResults_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && lstResults.SelectedItem is Scrip scrip)
            {
                SelectScrip(scrip);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideDropdown();
                txtSearch.Focus();
                e.Handled = true;
            }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _isDropdownVisible && lstResults.Items.Count > 0)
            {
                lstResults.Focus();
                lstResults.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (_isDropdownVisible)
                {
                    HideDropdown();
                }
                else
                {
                    txtSearch.Text = PLACEHOLDER_TEXT;
                    txtSearch.ForeColor = PlaceholderColor;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && lstResults.Items.Count > 0)
            {
                if (lstResults.Items[0] is Scrip scrip)
                {
                    SelectScrip(scrip);
                }
                e.Handled = true;
            }
        }

        private void TxtSearch_GotFocus(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearch.Text) &&
                txtSearch.Text != PLACEHOLDER_TEXT &&
                lstResults.Items.Count > 0)
            {
                ShowDropdown();
            }
        }

        private void TxtSearch_LostFocus(object sender, EventArgs e)
        {
            Task.Delay(200).ContinueWith(t =>
            {
                if (this.IsDisposed) return;

                this.BeginInvoke(new Action(() =>
                {
                    if (this.IsDisposed) return;

                    if (!lstResults.Focused && !txtSearch.Focused)
                    {
                        HideDropdown();
                    }
                }));
            });
        }

        private void PicClearIcon_Click(object sender, EventArgs e)
        {
            txtSearch.Text = PLACEHOLDER_TEXT;
            txtSearch.ForeColor = PlaceholderColor;
            txtSearch.Focus();
            HideDropdown();
        }

        private void SelectScrip(Scrip scrip)
        {
            try
            {
                _logger.LogInformation($"Scrip selected: {scrip.Symbol} ({scrip.Exchange})");

                txtSearch.Text = PLACEHOLDER_TEXT;
                txtSearch.ForeColor = PlaceholderColor;
                HideDropdown();

                ScripSelected?.Invoke(this, scrip);

                Task.Delay(100).ContinueWith(t =>
                {
                    if (this.IsDisposed) return;
                    this.BeginInvoke(new Action(() => txtSearch.Focus()));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error selecting scrip", ex);
            }
        }

        #endregion

        #region Custom Drawing

        private void LstResults_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var scrip = lstResults.Items[e.Index] as Scrip;
            if (scrip == null) return;

            e.DrawBackground();

            var backgroundColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? HighlightColor
                : TextBoxBackColor;

            using (var brush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Draw symbol
            using (var symbolBrush = new SolidBrush(AccentColor))
            using (var symbolFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            {
                e.Graphics.DrawString(
                    scrip.Symbol,
                    symbolFont,
                    symbolBrush,
                    e.Bounds.Left + 8,
                    e.Bounds.Top + 3);
            }

            // Draw name
            if (!string.IsNullOrEmpty(scrip.Name))
            {
                using (var nameBrush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                using (var nameFont = new Font("Segoe UI", 8F))
                {
                    var nameText = scrip.Name.Length > 50
                        ? scrip.Name.Substring(0, 50) + "..."
                        : scrip.Name;

                    e.Graphics.DrawString(
                        nameText,
                        nameFont,
                        nameBrush,
                        e.Bounds.Left + 8,
                        e.Bounds.Top + 18);
                }
            }

            // Draw exchange and type
            using (var metaBrush = new SolidBrush(PlaceholderColor))
            using (var metaFont = new Font("Segoe UI", 8F))
            {
                var metadata = $"{scrip.Exchange} | {scrip.InstrumentType}";
                var size = e.Graphics.MeasureString(metadata, metaFont);

                e.Graphics.DrawString(
                    metadata,
                    metaFont,
                    metaBrush,
                    e.Bounds.Right - size.Width - 10,
                    e.Bounds.Top + 8);
            }

            e.DrawFocusRectangle();
        }

        #endregion

        #region Helper Methods

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
            // ✅ Temporarily unsubscribe to avoid triggering TextChanged
            textBox.TextChanged -= TxtSearch_TextChanged;

            textBox.Text = placeholder;
            textBox.ForeColor = PlaceholderColor;

            // ✅ Re-subscribe
            textBox.TextChanged += TxtSearch_TextChanged;

            textBox.Enter += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = TextColor;
                }
            };

            textBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = PlaceholderColor;
                }
            };
        }

        private Image CreateSearchIcon()
        {
            var bitmap = new Bitmap(20, 20);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(PlaceholderColor, 2))
                {
                    g.DrawEllipse(pen, 3, 3, 11, 11);
                    g.DrawLine(pen, 12, 12, 17, 17);
                }
            }
            return bitmap;
        }

        private Image CreateClearIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(PlaceholderColor, 2))
                {
                    g.DrawLine(pen, 4, 4, 12, 12);
                    g.DrawLine(pen, 12, 4, 4, 12);
                }
            }
            return bitmap;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // ✅ Unsubscribe from parent events
            if (this.Parent != null)
            {
                this.Parent.Resize -= Parent_Resize;
              //  this.Parent.Scroll -= Parent_Scroll;
                this.Parent.Click -= Parent_Click;
            }

            // ✅ Remove dropdown from parent form
            if (pnlDropdown?.Parent != null)
            {
                pnlDropdown.Parent.Controls.Remove(pnlDropdown);
            }

            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _debounceTimer?.Dispose();
            pnlDropdown?.Dispose();

            base.OnHandleDestroyed(e);
        }

        #endregion

        #region Public Methods

        public void FocusSearch()
        {
            txtSearch.Focus();
            if (txtSearch.Text != PLACEHOLDER_TEXT)
            {
                txtSearch.SelectAll();
            }
        }

        public void ClearSearch()
        {
            txtSearch.Text = PLACEHOLDER_TEXT;
            txtSearch.ForeColor = PlaceholderColor;
            HideDropdown();
        }

        #endregion
    }
}