using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using TWS.Domain.Models;
using TWS.Domain.ViewModels;
using TWS.Infrastructure.DependencyInjection;
using TWS.Infrastructure.Logging;
using TWS.Presentation.Controls;
using TWS.Services.Interfaces;

namespace TWS.Presentation.Forms
{
    public partial class MarketWatchForm : Form
    {
        private readonly IMarketDataService _marketDataService;
        private readonly IPredefinedMarketwatchService _predefinedMwService;
        private readonly IScripMasterService _scripMasterService;
        private readonly ILogger _logger;
        private DataGridView dgvMarketWatch;
        private BindingList<MarketDataViewModel> _marketDataList;
        private ToolStrip toolStrip;
        private ToolStripButton btnRemove;
        private ToolStripButton btnRefresh;
        private ToolStripComboBox cmbMarketwatch;
        private ToolStripLabel lblStatus;
        private ScripLookupControl scripLookup;
        private bool _isScripMasterLoaded = false;

        // 🎨 ODIN Diet Color Scheme
        private static readonly Color BackgroundColor = Color.Black;
        private static readonly Color GridColor = Color.FromArgb(40, 40, 40);
        private static readonly Color TextColor = Color.FromArgb(220, 220, 220);
        private static readonly Color HeaderBackColor = Color.FromArgb(20, 20, 20);
        private static readonly Color HeaderForeColor = Color.FromArgb(180, 180, 180);
        private static readonly Color SelectedBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color PositiveColor = Color.FromArgb(0, 200, 83);  // Bright green
        private static readonly Color NegativeColor = Color.FromArgb(255, 90, 95); // Bright red
        private static readonly Color NeutralColor = Color.FromArgb(150, 150, 150); // Gray

        public MarketWatchForm()
        {
            _marketDataService = ServiceLocator.GetService<IMarketDataService>();
            _predefinedMwService = ServiceLocator.GetService<IPredefinedMarketwatchService>();
            _scripMasterService = ServiceLocator.GetService<IScripMasterService>();
            _logger = ServiceLocator.GetService<ILogger>();

            InitializeComponents();
            InitializeMarketData();
        }

        private void InitializeComponents()
        {
            this.Text = "Market Watch - ODIN Diet";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundColor;

            // 🎨 Dark Toolbar
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = HeaderBackColor,
                ForeColor = HeaderForeColor,
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };

            var lblMarketwatch = new ToolStripLabel("Marketwatch:") { ForeColor = TextColor };
            cmbMarketwatch = new ToolStripComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BackgroundColor,
                ForeColor = TextColor
            };
            cmbMarketwatch.SelectedIndexChanged += CmbMarketwatch_SelectedIndexChanged;

            btnRemove = new ToolStripButton("Remove")
            {
                Image = CreateIcon(NegativeColor),
                ForeColor = TextColor
            };
            btnRemove.Click += BtnRemove_Click;

            btnRefresh = new ToolStripButton("Refresh")
            {
                Image = CreateIcon(Color.DodgerBlue),
                ForeColor = TextColor
            };
            btnRefresh.Click += btnTestOrder_Click;
            // btnRefresh.Click += BtnRefresh_Click;

            lblStatus = new ToolStripLabel("Loading...")
            {
                Alignment = ToolStripItemAlignment.Right,
                ForeColor = Color.FromArgb(100, 180, 255)
            };

            toolStrip.Items.AddRange(new ToolStripItem[] {
                lblMarketwatch,
                cmbMarketwatch,
                new ToolStripSeparator(),
                btnRemove,
                btnRefresh,
                new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right },
                lblStatus
            });

            // 🎨 Dark DataGridView (ODIN Diet Style)
            dgvMarketWatch = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = BackgroundColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = GridColor,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderBackColor,
                    ForeColor = HeaderForeColor,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    SelectionBackColor = HeaderBackColor,
                    SelectionForeColor = HeaderForeColor,
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(5)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = BackgroundColor,
                    ForeColor = TextColor,
                    Font = new Font("Consolas", 9F),
                    SelectionBackColor = SelectedBackColor,
                    SelectionForeColor = TextColor,
                    Padding = new Padding(5, 2, 5, 2)
                },
                RowTemplate = { Height = 28 }
            };

            SetupColumns();

            // 🔍 Inline Search Control (between toolbar and grid)
            scripLookup = new Controls.ScripLookupControl
            {
                Dock = DockStyle.Top
            };
            scripLookup.ScripSelected += ScripLookup_ScripSelected;

            this.Controls.Add(dgvMarketWatch);
            this.Controls.Add(scripLookup);
            this.Controls.Add(toolStrip);
        }

        private void SetupColumns()
        {
            dgvMarketWatch.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Symbol",
                    HeaderText = "Symbol",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(100, 180, 255) // Blue for symbols
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Exchange",
                    HeaderText = "Exch",
                    Width = 60,
                    DefaultCellStyle = new DataGridViewCellStyle { ForeColor = NeutralColor }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "LTP",
                    HeaderText = "LTP",
                    Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        Font = new Font("Consolas", 10F, FontStyle.Bold)
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Change",
                    HeaderText = "Change",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "ChangePercent",
                    HeaderText = "Chg %",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Volume",
                    HeaderText = "Volume",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(180, 140, 220) // Purple for volume
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "BidPrice",
                    HeaderText = "Bid",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(100, 200, 255) // Light blue
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "AskPrice",
                    HeaderText = "Ask",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = Color.FromArgb(255, 180, 100) // Light orange
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Open",
                    HeaderText = "Open",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = NeutralColor
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "High",
                    HeaderText = "High",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = NeutralColor
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Low",
                    HeaderText = "Low",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = NeutralColor
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Close",
                    HeaderText = "Close",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight,
                        ForeColor = NeutralColor
                    }
                }
            });

            dgvMarketWatch.CellDoubleClick += DgvMarketWatch_CellDoubleClick;
            dgvMarketWatch.KeyDown += DgvMarketWatch_KeyDown;
            dgvMarketWatch.CellFormatting += DgvMarketWatch_CellFormatting;
        }

        private async void InitializeMarketData()
        {
            try
            {
                _marketDataList = new BindingList<MarketDataViewModel>();
                dgvMarketWatch.DataSource = _marketDataList;

                UpdateStatus("Loading marketwatches...");

                // ✅ STEP 1: Subscribe to tick events BEFORE connecting
                _marketDataService.OnTickReceived -= MarketDataService_OnTickReceived;
                _marketDataService.OnTickReceived += MarketDataService_OnTickReceived;

                // ✅ STEP 2: Connect to WebSocket
                _logger.LogInformation("Connecting to market data service...");
                await _marketDataService.ConnectAsync();
                _logger.LogInformation("✅ Connected to market data service");

                // Load predefined marketwatches
                var marketwatches = await _predefinedMwService.GetPredefinedMarketwatchesAsync();

                if (marketwatches != null && marketwatches.Any())
                {
                    _logger.LogInformation($"Loaded {marketwatches.Count} predefined marketwatches");

                    cmbMarketwatch.ComboBox.DataSource = marketwatches;
                    cmbMarketwatch.ComboBox.DisplayMember = "Name";
                    cmbMarketwatch.ComboBox.ValueMember = "Id";

                    if (marketwatches.Count > 0)
                    {
                        cmbMarketwatch.SelectedIndex = 0;
                    }

                    UpdateStatus($"Loaded {marketwatches.Count} marketwatches");
                }
                else
                {
                    UpdateStatus("No predefined marketwatch found");
                    _logger.LogWarning("No predefined marketwatches available");
                }

                // Download scrip master in background
                DownloadScripMasterInBackground();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing market data", ex);
                UpdateStatus("Error loading marketwatch");
                MessageBox.Show($"Error loading marketwatch: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CmbMarketwatch_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbMarketwatch.SelectedItem == null) return;

                var selectedMarketwatch = (PredefinedMarketwatch)cmbMarketwatch.SelectedItem;

                _logger.LogInformation($"Loading scrips for marketwatch: {selectedMarketwatch.Name}");
                UpdateStatus($"Loading {selectedMarketwatch.Name}...");

                var scrips = await _predefinedMwService.GetMarketwatchScripsAsync(selectedMarketwatch.Id);

                if (scrips != null && scrips.Any())
                {
                    _logger.LogInformation($"Loaded {scrips.Count} scrips from {selectedMarketwatch.Name}");

                    _marketDataList.Clear();

                    foreach (var scrip in scrips)
                    {
                        var viewModel = new MarketDataViewModel { Scrip = scrip };
                        _marketDataList.Add(viewModel);
                        _logger.LogInformation($"Added to grid: {scrip.Symbol} ({scrip.Exchange}|{scrip.Token})");
                    }

                    UpdateStatus($"Loaded {scrips.Count} scrips from {selectedMarketwatch.Name}");

                    await System.Threading.Tasks.Task.Delay(500);
                    await SubscribeToMarketDataAsync(scrips);
                }
                else
                {
                    UpdateStatus("No scrips found in this marketwatch");
                    _marketDataList.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading marketwatch scrips", ex);
                UpdateStatus("Error loading scrips");
                MessageBox.Show($"Error loading scrips: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DownloadScripMasterInBackground()
        {
            try
            {
                _logger.LogInformation("Starting background scrip master download");
                UpdateStatus("Downloading scrip master in background...");

                var result = await _scripMasterService.DownloadAllScripMastersAsync();

                if (result.Success)
                {
                    _isScripMasterLoaded = true;
                    _logger.LogInformation($"Scrip master download completed: {result.TotalScripsLoaded} scrips");
                    UpdateStatus($"Scrip master loaded: {result.TotalScripsLoaded:N0} scrips");

                    // ✅ Notify inline search control
                    scripLookup?.RefreshData();

                    OnScripMasterLoaded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _logger.LogWarning("Scrip master download failed, continuing with predefined scrips only");
                    UpdateStatus("Scrip master download failed - using predefined scrips");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error downloading scrip master in background", ex);
                UpdateStatus("Scrip master download error");
            }
        }

        private async System.Threading.Tasks.Task SubscribeToMarketDataAsync(List<Scrip> scrips)
        {
            try
            {
                _logger.LogInformation($"📡 Starting subscription for {scrips.Count} scrips");
                UpdateStatus($"Subscribing to {scrips.Count} scrips...");

                if (!_marketDataService.IsConnected)
                {
                    _logger.LogWarning("Not connected, attempting to reconnect...");
                    await _marketDataService.ConnectAsync();
                    await System.Threading.Tasks.Task.Delay(1000);
                }

                var tokens = scrips.Select(s => $"{s.Exchange}|{s.Token}").ToArray();

                _logger.LogInformation($"📡 Sending subscription request for {tokens.Length} tokens");
                foreach (var token in tokens.Take(5))
                {
                    _logger.LogInformation($"   - {token}");
                }
                if (tokens.Length > 5)
                {
                    _logger.LogInformation($"   ... and {tokens.Length - 5} more");
                }

                await _marketDataService.SubscribeAsync(tokens);

                UpdateStatus($"✅ Subscribed to {scrips.Count} scrips - Waiting for ticks...");
                _logger.LogInformation($"✅ Subscription request sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error subscribing to market data", ex);
                UpdateStatus("❌ Subscription error");
                MessageBox.Show($"Error subscribing to market data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ✅ This is called when a tick is received from WebSocket
        /// </summary>
        private void MarketDataService_OnTickReceived(object sender, TickData tick)
        {
            try
            {
                _logger.LogInformation($"📊 TICK: {tick.Exchange}|{tick.Token} LTP={tick.LastPrice}");

                if (dgvMarketWatch.InvokeRequired)
                {
                    dgvMarketWatch.BeginInvoke(new Action(() => UpdateMarketData(tick)));
                }
                else
                {
                    UpdateMarketData(tick);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error in OnTickReceived handler: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ✅ Updates ONLY the fields that are present in the tick (partial updates)
        /// </summary>
        private void UpdateMarketData(TickData tick)
        {
            try
            {
                var item = _marketDataList.FirstOrDefault(m =>
                    m.Scrip.Token == tick.Token && m.Scrip.Exchange == tick.Exchange);

                if (item != null)
                {
                    _logger.LogInformation($"✅ Match found: {item.Scrip.Symbol}");

                    // ✅ PARTIAL UPDATE: Only update fields that are present in the tick
                    bool updated = false;

                    if (tick.LastPrice > 0)
                    {
                        item.LTP = tick.LastPrice;
                        updated = true;
                        _logger.LogInformation($"   LTP: {tick.LastPrice:N2}");
                    }

                    if (tick.Volume > 0)
                    {
                        item.Volume = tick.Volume;
                        updated = true;
                        _logger.LogInformation($"   Volume: {tick.Volume:N0}");
                    }

                    if (tick.BuyPrice > 0)
                    {
                        item.BidPrice = tick.BuyPrice;
                        updated = true;
                    }

                    if (tick.SellPrice > 0)
                    {
                        item.AskPrice = tick.SellPrice;
                        updated = true;
                    }

                    if (tick.OpenPrice > 0)
                    {
                        item.Open = tick.OpenPrice;
                        updated = true;
                    }

                    if (tick.HighPrice > 0)
                    {
                        item.High = tick.HighPrice;
                        updated = true;
                    }

                    if (tick.LowPrice > 0)
                    {
                        item.Low = tick.LowPrice;
                        updated = true;
                    }

                    if (tick.ClosePrice > 0)
                    {
                        item.Close = tick.ClosePrice;
                        updated = true;
                    }

                    // ✅ Calculate change if we have both LTP and Close
                    if (item.LTP > 0 && item.Close > 0)
                    {
                        item.Change = item.LTP - item.Close;
                        item.ChangePercent = (item.Change / item.Close) * 100;
                    }

                    // ✅ CRITICAL: Notify the BindingList only if something changed
                    if (updated)
                    {
                        var index = _marketDataList.IndexOf(item);
                        _marketDataList.ResetItem(index);

                        _logger.LogInformation($"✅ UI Updated: {item.Scrip.Symbol} " +
                            $"LTP={item.LTP:N2} Chg={item.Change:N2} ({item.ChangePercent:N2}%)");
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ No match for {tick.Exchange}|{tick.Token}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error updating market data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ✅ Event handler when scrip is selected from inline search
        /// </summary>
        private async void ScripLookup_ScripSelected(object sender, Scrip scrip)
        {
            await AddScripToWatch(scrip);
        }

        private async Task AddScripToWatch(Scrip scrip)
        {
            try
            {
                if (_marketDataList.Any(m => m.Scrip.Token == scrip.Token && m.Scrip.Exchange == scrip.Exchange))
                {
                    MessageBox.Show("This scrip is already in the market watch.", "Duplicate",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _marketDataList.Add(new MarketDataViewModel { Scrip = scrip });
                _logger.LogInformation($"Added {scrip.Symbol} to market watch");

                var tokens = new[] { $"{scrip.Exchange}|{scrip.Token}" };
                await _marketDataService.SubscribeAsync(tokens);

                _logger.LogInformation($"✅ Subscribed to {scrip.Symbol}");
                UpdateStatus($"{scrip.Symbol} added and subscribed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding scrip to watch", ex);
                MessageBox.Show($"Error adding scrip: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvMarketWatch.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a scrip to remove.", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedItem = dgvMarketWatch.SelectedRows[0].DataBoundItem as MarketDataViewModel;
                if (selectedItem != null)
                {
                    var result = MessageBox.Show(
                        $"Remove {selectedItem.Scrip.Symbol} from market watch?",
                        "Confirm Remove",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        var tokens = new[] { $"{selectedItem.Scrip.Exchange}|{selectedItem.Scrip.Token}" };
                        await _marketDataService.UnsubscribeAsync(tokens);

                        _marketDataList.Remove(selectedItem);
                        _logger.LogInformation($"Removed and unsubscribed: {selectedItem.Scrip.Symbol}");
                        UpdateStatus($"{selectedItem.Scrip.Symbol} removed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing scrip", ex);
                MessageBox.Show($"Error removing scrip: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnTestOrder_Click(object sender, EventArgs e)
        {
            try
            {
                var orderService = ServiceLocator.GetService<IOrderService>();

                var testRequest = new OrderRequest
                {
                    exchange = "NSE",
                    tradingsymbol = "IDEA_EQ",
                    token = "14366",
                    transtype = "B",
                    priceType = "L",
                    qty = 1,
                    price = "7.45",
                    product = "MIS",
                    ret = "DAY",
                    orderType = "Regular",
                    source = "WEB",
                    triggerPrice = "0",
                    disqty = 0,
                    mkt_protection = "",
                    trgprc = "0",
                    stopLoss = "0"
                };

                MessageBox.Show("Placing test order...");
                var result = await orderService.PlaceOrderAsync(testRequest);

                if (result.Status == "Ok")
                {
                    MessageBox.Show(
                        $"✅ SUCCESS!\n\n" +
                        $"Order No: {result.Result[0].OrderNo}\n" +
                        $"Time: {result.Result[0].RequestTime}\n\n" +
                        $"The 405 error is fixed!",
                        "Order Placed Successfully");
                }
                else
                {
                    MessageBox.Show($"❌ Order failed: {result.Message}");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("405"))
            {
                MessageBox.Show(
                    "⚠️ Still getting 405 error!\n\n" +
                    "Check:\n" +
                    "1. Using correct endpoint: /orders/web/execute\n" +
                    "2. Using POST method\n" +
                    "3. Sending array: [{...}]\n" +
                    "4. Authentication headers present",
                    "405 Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                btnRefresh.Enabled = false;
                UpdateStatus("Refreshing market data...");

                if (_marketDataList.Any())
                {
                    var tokens = _marketDataList
                        .Select(m => $"{m.Scrip.Exchange}|{m.Scrip.Token}")
                        .ToArray();

                    _logger.LogInformation($"Refreshing {tokens.Length} subscriptions");

                    await _marketDataService.UnsubscribeAsync(tokens);
                    await System.Threading.Tasks.Task.Delay(500);
                    await _marketDataService.SubscribeAsync(tokens);

                    _logger.LogInformation("Refresh completed");
                }

                UpdateStatus("Market data refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing market data", ex);
                UpdateStatus("Refresh error");
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private void DgvMarketWatch_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvMarketWatch.Columns[e.ColumnIndex].DataPropertyName == "Change" ||
                dgvMarketWatch.Columns[e.ColumnIndex].DataPropertyName == "ChangePercent")
            {
                if (e.Value != null && decimal.TryParse(e.Value.ToString(), out var value))
                {
                    if (value > 0)
                    {
                        e.CellStyle.ForeColor = PositiveColor;
                    }
                    else if (value < 0)
                    {
                        e.CellStyle.ForeColor = NegativeColor;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = NeutralColor;
                    }

                    // Add ▲ or ▼ symbol for percentage
                    if (dgvMarketWatch.Columns[e.ColumnIndex].DataPropertyName == "ChangePercent" && value != 0)
                    {
                        string arrow = value > 0 ? "▲ " : "▼ ";
                        e.Value = arrow + Math.Abs(value).ToString("N2");
                        e.FormattingApplied = true;
                    }
                }
            }
        }

        /// <summary>
        /// Double-click on row to open BUY order
        /// </summary>
        private void DgvMarketWatch_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                OpenOrderEntry(Domain.Models.OrderEnums.TransactionType.BUY);
            }
        }

        /// <summary>
        /// F1 = BUY, F2 = SELL
        /// </summary>
        private void DgvMarketWatch_KeyDown(object sender, KeyEventArgs e)
        {
            if (dgvMarketWatch.SelectedRows.Count == 0)
                return;

            if (e.KeyCode == Keys.F1)
            {
                OpenOrderEntry(Domain.Models.OrderEnums.TransactionType.BUY);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                OpenOrderEntry(Domain.Models.OrderEnums.TransactionType.SELL);
                e.Handled = true;
            }
        }

        private void OpenOrderEntry(Domain.Models.OrderEnums.TransactionType transactionType)
        {
            try
            {
                if (dgvMarketWatch.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a scrip first", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedItem = dgvMarketWatch.SelectedRows[0].DataBoundItem as MarketDataViewModel;
                if (selectedItem?.Scrip != null)
                {
                    _logger.LogInformation($"Opening {transactionType} order for {selectedItem.Scrip.Symbol}");

                    using (var orderForm = new OrderEntryForm(transactionType, selectedItem.Scrip))
                    {
                        if (orderForm.ShowDialog(this) == DialogResult.OK)
                        {
                            // Order placed successfully
                            UpdateStatus($"Order placed for {selectedItem.Scrip.Symbol}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening order entry", ex);
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus.Owner?.InvokeRequired == true)
            {
                lblStatus.Owner.BeginInvoke(new Action(() => lblStatus.Text = message));
            }
            else
            {
                lblStatus.Text = message;
            }
        }

        private Image CreateIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(color), 2, 2, 12, 12);
            }
            return bitmap;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _logger.LogInformation("Form closing - cleaning up subscriptions");

                if (_marketDataService != null)
                {
                    _marketDataService.OnTickReceived -= MarketDataService_OnTickReceived;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during form closing", ex);
            }

            base.OnFormClosing(e);
        }

        public event EventHandler OnScripMasterLoaded;
    }

    /// <summary>
    /// 🎨 Dark color table for ToolStrip
    /// </summary>
    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
        public override Color MenuItemSelected => Color.FromArgb(45, 45, 48);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(30, 30, 30);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(30, 30, 30);
    }
}