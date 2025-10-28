using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TWS.Domain.Models;
using TWS.Infrastructure.DependencyInjection;
using TWS.Infrastructure.Logging;
using TWS.Services.Interfaces;

namespace TWS.Presentation.Forms
{
    public partial class OrderBookForm : Form
    {
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;

        // UI Controls
        private DataGridView dgvOrders;
        private BindingList<OrderBookEntry> _orderList;
        private ToolStrip toolStrip;
        private ToolStripButton btnRefresh;
        private ToolStripButton btnCancel;
        private ToolStripButton btnModify;
        private ToolStripLabel lblStatus;
        private ToolStripComboBox cmbFilter;

        // 🎨 ODIN Diet Colors
        private static readonly Color BackgroundColor = Color.Black;
        private static readonly Color GridColor = Color.FromArgb(40, 40, 40);
        private static readonly Color TextColor = Color.FromArgb(220, 220, 220);
        private static readonly Color HeaderBackColor = Color.FromArgb(20, 20, 20);
        private static readonly Color HeaderForeColor = Color.FromArgb(180, 180, 180);
        private static readonly Color SelectedBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color BuyColor = Color.FromArgb(0, 150, 255);
        private static readonly Color SellColor = Color.FromArgb(255, 100, 100);

        // Status Colors
        private static readonly Color OpenColor = Color.FromArgb(100, 180, 255);
        private static readonly Color CompleteColor = Color.FromArgb(0, 200, 83);
        private static readonly Color CancelledColor = Color.FromArgb(150, 150, 150);
        private static readonly Color RejectedColor = Color.FromArgb(255, 90, 95);

        public OrderBookForm()
        {
            _orderService = ServiceLocator.GetService<IOrderService>();
            _logger = ServiceLocator.GetService<ILogger>();

            InitializeComponents();
            SubscribeToOrderUpdates();
            LoadOrdersAsync();
        }

        private void InitializeComponents()
        {
            this.Text = "Order Book";
            this.Size = new Size(1400, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundColor;

            // Toolbar
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = HeaderBackColor,
                ForeColor = HeaderForeColor,
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };

            btnRefresh = new ToolStripButton("Refresh")
            {
                Image = CreateIcon(Color.DodgerBlue),
                ForeColor = TextColor
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnModify = new ToolStripButton("Modify")
            {
                Image = CreateIcon(Color.Orange),
                ForeColor = TextColor,
                Enabled = false
            };
            btnModify.Click += BtnModify_Click;

            btnCancel = new ToolStripButton("Cancel")
            {
                Image = CreateIcon(Color.Red),
                ForeColor = TextColor,
                Enabled = false
            };
            btnCancel.Click += BtnCancel_Click;

            var lblFilter = new ToolStripLabel("Filter:") { ForeColor = TextColor };
            cmbFilter = new ToolStripComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BackgroundColor,
                ForeColor = TextColor
            };
            cmbFilter.Items.AddRange(new object[] { "ALL", "OPEN", "COMPLETE", "CANCELLED", "REJECTED" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += CmbFilter_SelectedIndexChanged;

            lblStatus = new ToolStripLabel("Ready")
            {
                Alignment = ToolStripItemAlignment.Right,
                ForeColor = Color.FromArgb(100, 180, 255)
            };

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnRefresh,
                new ToolStripSeparator(),
                btnModify,
                btnCancel,
                new ToolStripSeparator(),
                lblFilter,
                cmbFilter,
                new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right },
                lblStatus
            });

            // Virtual DataGridView for high performance
            dgvOrders = new DataGridView
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
                VirtualMode = true, // ✅ Enable virtual mode for huge datasets
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

            dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;
            dgvOrders.CellFormatting += DgvOrders_CellFormatting;

            this.Controls.Add(dgvOrders);
            this.Controls.Add(toolStrip);
        }

        private void SetupColumns()
        {
            dgvOrders.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "OrderId",
                    HeaderText = "Order ID",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Font = new Font("Consolas", 8.5F, FontStyle.Bold)
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "OrderTime",
                    HeaderText = "Time",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "HH:mm:ss" }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Symbol",
                    HeaderText = "Symbol",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Exchange",
                    HeaderText = "Exch",
                    Width = 60
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "TransactionType",
                    HeaderText = "Type",
                    Width = 60,
                    DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9F, FontStyle.Bold) }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "OrderType",
                    HeaderText = "Order Type",
                    Width = 90
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "ProductType",
                    HeaderText = "Product",
                    Width = 100
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Quantity",
                    HeaderText = "Qty",
                    Width = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Price",
                    HeaderText = "Price",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "TriggerPrice",
                    HeaderText = "Trigger",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "Status",
                    HeaderText = "Status",
                    Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9F, FontStyle.Bold) }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "FilledQuantity",
                    HeaderText = "Filled",
                    Width = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "PendingQuantity",
                    HeaderText = "Pending",
                    Width = 80,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "AveragePrice",
                    HeaderText = "Avg Price",
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "N2",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "StatusMessage",
                    HeaderText = "Message",
                    Width = 200
                }
            });
        }

        private void SubscribeToOrderUpdates()
        {
          //  _orderService.OnOrderUpdate += OrderService_OnOrderUpdate;
        }

        private void OrderService_OnOrderUpdate(object sender, Order order)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateOrder(order)));
            }
            else
            {
                UpdateOrder(order);
            }
        }

        private void UpdateOrder(Order order)
        {
            try
            {
                var entry = _orderList.FirstOrDefault(o => o.OrderId == order.OrderId);

                if (entry != null)
                {
                    // Update existing order
                    entry.Status = order.Status;
                    entry.FilledQuantity = order.FilledQuantity;
                    entry.PendingQuantity = order.PendingQuantity;
                    entry.AveragePrice = order.AveragePrice;
                    entry.StatusMessage = order.StatusMessage;
                }
                else
                {
                    // Add new order
                    _orderList.Add(ConvertToOrderBookEntry(order));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating order", ex);
            }
        }

        private async void LoadOrdersAsync()
        {
            try
            {
                lblStatus.Text = "Loading orders...";
                btnRefresh.Enabled = false;

                _orderList = new BindingList<OrderBookEntry>();
                dgvOrders.DataSource = _orderList;

                var orders = await _orderService.GetOrderBookAsync();

                //foreach (var order in orders)
                //{
                //    _orderList.Add(ConvertToOrderBookEntry(order));
                //}

                lblStatus.Text = $"Loaded {orders.Count} orders";
                _logger.LogInformation($"Loaded {orders.Count} orders");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading orders", ex);
                lblStatus.Text = "Error loading orders";
                MessageBox.Show($"Error loading orders: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private OrderBookEntry ConvertToOrderBookEntry(Order order)
        {
            return new OrderBookEntry
            {
                OrderId = order.OrderId,
                OrderTime = order.OrderTime,
                Symbol = order.TradingSymbol,
                Exchange = order.Exchange,
                TransactionType = order.TransactionType,
                OrderType = order.OrderType,
                ProductType = order.ProductType,
                Quantity = order.Quantity,
                Price = order.Price,
                TriggerPrice = order.TriggerPrice,
                Status = order.Status,
                FilledQuantity = order.FilledQuantity,
                PendingQuantity = order.PendingQuantity,
                AveragePrice = order.AveragePrice,
                StatusMessage = order.StatusMessage
            };
        }

        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvOrders.Rows[e.RowIndex];
            var entry = row.DataBoundItem as OrderBookEntry;
            if (entry == null) return;

            // Color BUY/SELL
            if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "TransactionType")
            {
                e.CellStyle.ForeColor = entry.TransactionType == "BUY" ? BuyColor : SellColor;
            }

            // Color Status
            if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "Status")
            {
                switch (entry.Status)
                {
                    case "OPEN":
                        e.CellStyle.ForeColor = OpenColor;
                        break;
                    case "COMPLETE":
                        e.CellStyle.ForeColor = CompleteColor;
                        break;
                    case "CANCELLED":
                        e.CellStyle.ForeColor = CancelledColor;
                        break;
                    case "REJECTED":
                        e.CellStyle.ForeColor = RejectedColor;
                        break;
                    default:
                        e.CellStyle.ForeColor = TextColor;
                        break;
                }
            }
        }

        private void DgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count > 0)
            {
                var entry = dgvOrders.SelectedRows[0].DataBoundItem as OrderBookEntry;
                btnModify.Enabled = entry?.Status == "OPEN";
                btnCancel.Enabled = entry?.Status == "OPEN";
            }
            else
            {
                btnModify.Enabled = false;
                btnCancel.Enabled = false;
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadOrdersAsync();
        }

        private void BtnModify_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            var entry = dgvOrders.SelectedRows[0].DataBoundItem as OrderBookEntry;
            if (entry != null)
            {
                MessageBox.Show($"Modify order: {entry.OrderId}\n(Implementation pending)",
                    "Modify Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void BtnCancel_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;

            var entry = dgvOrders.SelectedRows[0].DataBoundItem as OrderBookEntry;
            if (entry == null) return;

            var result = MessageBox.Show(
                $"Cancel order for {entry.Symbol}?\n\nOrder ID: {entry.OrderId}",
                "Confirm Cancel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    btnCancel.Enabled = false;
                    lblStatus.Text = "Cancelling order...";

                    var response = await _orderService.CancelOrderAsync(entry.OrderId);

                  //  if (response?.stat == "Ok")
                  //  {
                  //      lblStatus.Text = "Order cancelled";
                  //      MessageBox.Show("Order cancelled successfully", "Success",
                  //          MessageBoxButtons.OK, MessageBoxIcon.Information);
                  //  }
                  //  else
                  //  {
                  //      lblStatus.Text = "Cancel failed";
                  ////      MessageBox.Show($"Cancel failed: {response?.emsg}", "Error",
                  // //         MessageBoxButtons.OK, MessageBoxIcon.Error);
                  //  }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error cancelling order", ex);
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnCancel.Enabled = true;
                }
            }
        }

        private void CmbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            var filter = cmbFilter.SelectedItem?.ToString();

            if (filter == "ALL")
            {
                dgvOrders.DataSource = _orderList;
            }
            else
            {
                var filtered = _orderList.Where(o => o.Status == filter).ToList();
                dgvOrders.DataSource = new BindingList<OrderBookEntry>(filtered);
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
         //   _orderService.OnOrderUpdate -= OrderService_OnOrderUpdate;
            base.OnFormClosing(e);
        }
    }

}