using System;
using System.Drawing;
using System.Windows.Forms;
using TWS.Domain.Models;
using TWS.Infrastructure.DependencyInjection;
using TWS.Infrastructure.Logging;
using TWS.Services.Interfaces;
using static TWS.Domain.Models.OrderEnums;

namespace TWS.Presentation.Forms
{
    public partial class OrderEntryForm : Form
    {
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly TransactionType _transactionType;
        private readonly Scrip _scrip;

        // UI Controls
        private Label lblTitle;
        private TextBox txtSymbol;
        private ComboBox cmbExchange;
        private ComboBox cmbSeries;
        private DateTimePicker dtpExpiry;
        private TextBox txtStrikePrice;
        private ComboBox cmbOptionType;
        private NumericUpDown nudQuantity;
        private TextBox txtPrice;
        private TextBox txtTriggerPrice;
        private ComboBox cmbOrderType;
        private ComboBox cmbProductType;
        private ComboBox cmbValidity;
        private Button btnSubmit;
        private Button btnClear;
        
        private TextBox lblMessage;
        private ComboBox cmbInstrName;
        private TextBox txtProPercent;
        private TextBox txtOC;
        private TextBox txtCU;
        private ComboBox cmbSettlor;
        private TextBox txtMEQty;
        private TextBox txtUserRemarks;

        // Colors based on transaction type
        private static readonly Color BuyBackColor = Color.FromArgb(0, 80, 180);      // Blue
        private static readonly Color SellBackColor = Color.FromArgb(180, 40, 40);    // Red
        private static readonly Color DarkBackColor = Color.FromArgb(20, 20, 20);
        private static readonly Color TextBoxBackColor = Color.FromArgb(40, 40, 40);
        private static readonly Color TextColor = Color.FromArgb(240, 240, 240);

        public OrderEntryForm(TransactionType transactionType, Scrip scrip = null)
        {
            _orderService = ServiceLocator.GetService<IOrderService>();
            _logger = ServiceLocator.GetService<ILogger>();
            _transactionType = transactionType;
            _scrip = scrip;

            InitializeComponents();
            LoadScripDetails();
        }

        /***
        private void InitializeComponents()
        {
            this.Size = new Size(450, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // Set colors based on transaction type
            var themeColor = _transactionType == TransactionType.BUY ? BuyBackColor : SellBackColor;
            this.BackColor = themeColor;
            this.Text = $"{_transactionType} Order Entry";

            // Title Bar
            lblTitle = new Label
            {
                Text = $"Derivatives {_transactionType} Order Entry Form",
                Location = new Point(10, 10),
                Size = new Size(420, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = themeColor
            };

            // Main Panel
            var mainPanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(420, 410),
                BackColor = DarkBackColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            int yPos = 15;
            int labelWidth = 100;
            int controlWidth = 290;

            // Symbol
            AddLabel(mainPanel, "Symbol:", 15, yPos, labelWidth);
            txtSymbol = AddTextBox(mainPanel, 120, yPos, controlWidth);
            txtSymbol.ReadOnly = true;
            yPos += 35;

            // Exchange & Series (side by side)
            AddLabel(mainPanel, "Exchange:", 15, yPos, labelWidth);
            cmbExchange = AddComboBox(mainPanel, 120, yPos, 90);
            cmbExchange.Items.AddRange(new[] { "NSE", "BSE", "NFO", "MCX" });
            cmbExchange.SelectedIndex = 0;

            AddLabel(mainPanel, "Series:", 230, yPos, 50);
            cmbSeries = AddComboBox(mainPanel, 285, yPos, 125);
            cmbSeries.Items.AddRange(new[] { "EQ", "FUT", "OPT" });
            cmbSeries.SelectedIndex = 0;
            cmbSeries.SelectedIndexChanged += CmbSeries_SelectedIndexChanged;
            yPos += 35;

            // Expiry Date & Strike Price (for derivatives)
            AddLabel(mainPanel, "Expiry Date:", 15, yPos, labelWidth);
            dtpExpiry = new DateTimePicker
            {
                Location = new Point(120, yPos),
                Width = 90,
                Format = DateTimePickerFormat.Short,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                Enabled = false
            };
            mainPanel.Controls.Add(dtpExpiry);

            AddLabel(mainPanel, "Strike:", 230, yPos, 50);
            txtStrikePrice = AddTextBox(mainPanel, 285, yPos, 125);
            txtStrikePrice.Enabled = false;
            yPos += 35;

            // Option Type
            AddLabel(mainPanel, "Option Type:", 15, yPos, labelWidth);
            cmbOptionType = AddComboBox(mainPanel, 120, yPos, controlWidth);
            cmbOptionType.Items.AddRange(new[] { "CE", "PE" });
            cmbOptionType.Enabled = false;
            yPos += 35;

            // Quantity
            AddLabel(mainPanel, "Quantity:", 15, yPos, labelWidth);
            nudQuantity = new NumericUpDown
            {
                Location = new Point(120, yPos),
                Width = controlWidth,
                Minimum = 1,
                Maximum = 100000,
                Value = 1,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(nudQuantity);
            yPos += 35;

            // Price
            AddLabel(mainPanel, "Price:", 15, yPos, labelWidth);
            txtPrice = AddTextBox(mainPanel, 120, yPos, controlWidth);
            txtPrice.Text = "0.00";
            yPos += 35;

            // Trigger Price
            AddLabel(mainPanel, "Trigger Price:", 15, yPos, labelWidth);
            txtTriggerPrice = AddTextBox(mainPanel, 120, yPos, controlWidth);
            txtTriggerPrice.Text = "0.00";
            txtTriggerPrice.Enabled = false;
            yPos += 35;

            // Order Type
            AddLabel(mainPanel, "Order Type:", 15, yPos, labelWidth);
            cmbOrderType = AddComboBox(mainPanel, 120, yPos, controlWidth);
            cmbOrderType.Items.AddRange(new[] { "MARKET", "LIMIT", "SL", "SL-M" });
            cmbOrderType.SelectedIndex = 0;
            cmbOrderType.SelectedIndexChanged += CmbOrderType_SelectedIndexChanged;
            yPos += 35;

            // Product Type
            AddLabel(mainPanel, "Product Type:", 15, yPos, labelWidth);
            cmbProductType = AddComboBox(mainPanel, 120, yPos, controlWidth);
            cmbProductType.Items.AddRange(new[] { "INTRADAY", "DELIVERY", "CARRYFORWARD" });
            cmbProductType.SelectedIndex = 0;
            yPos += 35;

            // Validity
            AddLabel(mainPanel, "Validity:", 15, yPos, labelWidth);
            cmbValidity = AddComboBox(mainPanel, 120, yPos, controlWidth);
            cmbValidity.Items.AddRange(new[] { "DAY", "IOC" });
            cmbValidity.SelectedIndex = 0;
            yPos += 35;

            // Message Label
            lblMessage = new Label
            {
                Location = new Point(15, yPos),
                Size = new Size(390, 20),
                ForeColor = Color.Yellow,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F)
            };
            mainPanel.Controls.Add(lblMessage);

            // Buttons
            var buttonY = 470;
            btnSubmit = new Button
            {
                Text = "Submit",
                Location = new Point(150, buttonY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(260, buttonY),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;

            this.Controls.AddRange(new Control[] { lblTitle, mainPanel, btnSubmit, btnClear });
        }
         
         ***/

        private void InitializeComponents()
        {
            // === Form Properties ===
            this.Size = new Size(1200, 210); //  form height
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // === Set colors based on transaction type ===
            var themeColor = _transactionType == TransactionType.BUY ? BuyBackColor : SellBackColor; 
            this.BackColor = themeColor;
            this.Text = $"{_transactionType} Order Entry";

            // === Title  Bar ===
            lblTitle = new Label
            {
                Text = $"Derivatives {_transactionType} Order Entry Form",
                Location = new Point(10, 10),
                Size = new Size(600, 20),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = themeColor
            };
            this.Controls.Add(lblTitle);

            // === Main Panel ===
            var mainPanel = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(1170, 90), //  Panel height 
                BackColor = themeColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(mainPanel);

            // === Table Layout ===
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 13,
                RowCount = 2,
                Padding = new Padding(5, 0, 5, 0), // no top/bottom padding
                BackColor = themeColor,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            for (int i = 0; i < 13; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 13));

            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            mainPanel.Controls.Add(table);

            // === Helper Function ===
            void AddColumn(int col, int row, string labelText, Control control, int colspan = 1)
            {
                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1, 0, 1, 0) }; //  tightened padding

                var lbl = new Label
                {
                    Text = labelText,
                    ForeColor = Color.White,
                    Dock = DockStyle.Top,
                    Height = 13, // label height
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 8F)
                };

                control.Dock = DockStyle.Top;
                control.Height = 21; // control height
                control.Margin = new Padding(0);
                control.BackColor = Color.White;
                control.Font = new Font("Segoe UI", 8.5F);

                panel.Controls.Add(control);
                panel.Controls.Add(lbl);
                table.Controls.Add(panel, col, row);
                if (colspan > 1)
                    table.SetColumnSpan(panel, colspan);
            }

            // === Row 1 ===

            //Exchange
            AddColumn(0, 0, "Exchange", cmbExchange = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbExchange.Items.AddRange(new[] { "NSE", "BSE", "NFO", "MCX" });
            cmbExchange.SelectedIndex = 0;

            //Type
            AddColumn(1, 0, "Type", cmbSeries = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbSeries.Items.AddRange(new[] { "EQ","FUT", "OPT" });
            cmbSeries.SelectedIndex = 0;
            cmbSeries.SelectedIndexChanged += CmbSeries_SelectedIndexChanged;

            //Instr Name
            AddColumn(2, 0, "Instr Name", cmbInstrName = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbInstrName.Items.AddRange(new[] { "FUTSTK", "FUTIDX" });
            cmbInstrName.SelectedIndex = 0;
            cmbInstrName.SelectedIndexChanged += cmbInstrName_SelectedIndexChanged;

            //Symbol
            AddColumn(3, 0, "Symbol", txtSymbol = new TextBox());
            txtSymbol.ReadOnly = true;

            //Expiry Date
            AddColumn(4, 0, "Expiry Date", 
                dtpExpiry = new DateTimePicker 
                { Format = DateTimePickerFormat.Short,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                Enabled = false
                });

            //Strike Price
            AddColumn(5, 0, "Strike Price", txtStrikePrice = new TextBox());
            txtStrikePrice.Enabled = false;

            //Opt Type
            AddColumn(6, 0, "Opt Type", cmbOptionType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbOptionType.Items.AddRange(new[] { "CE", "PE" });
            cmbOptionType.SelectedIndex = 0;
            cmbOptionType.Enabled = false;

            //Total Qty
            AddColumn(7, 0, "Total Qty", 
                nudQuantity = new NumericUpDown {
                    Minimum = 1,
                    Maximum = 100000,
                    Value = 1,
                    BackColor = TextBoxBackColor,
                    BorderStyle = BorderStyle.FixedSingle
                });

            //Price
            AddColumn(8, 0, "Price", txtPrice = new TextBox());
            txtPrice.Text = "0.00";

            //Pro %
            AddColumn(9, 0, "Pro %", txtProPercent = new TextBox());

            //O/C
            AddColumn(10, 0, "O/C", txtOC = new TextBox());

            //C/U
            AddColumn(11, 0, "C/U", txtCU = new TextBox());

            // === Row 2 ===

            //Settlor
            AddColumn(0, 1, "Settlor", cmbSettlor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });

            //Product Type
            AddColumn(1, 1, "Product Type", cmbProductType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbProductType.Items.AddRange(new[] { "INTRADAY", "DELIVERY", "CARRYFORWARD" });
            cmbProductType.SelectedIndex = 0;

            //Trig. Price
            AddColumn(2, 1, "Trig. Price", txtTriggerPrice = new TextBox());
            txtTriggerPrice.Text = "0.00";
            txtTriggerPrice.Enabled = false;

            //M/F AON
            AddColumn(3, 1, "M/F AON", cmbOrderType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbOrderType.Items.AddRange(new[] { "MARKET", "LIMIT", "SL", "SL-M" });
            cmbOrderType.SelectedIndex = 0;
            cmbOrderType.SelectedIndexChanged += CmbOrderType_SelectedIndexChanged;

            //M/F Qty
            AddColumn(4, 1, "M/F Qty", txtMEQty = new TextBox());

            //Validity
            AddColumn(5, 1, "Validity", cmbValidity = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
            cmbValidity.Items.AddRange(new[] { "DAY", "IOC" });
            cmbValidity.SelectedIndex = 0;

            // === Right-end: User Remarks + Buttons ===
            AddColumn(8, 1, "User Remarks", txtUserRemarks = new TextBox(), 3);

            btnSubmit = new Button
            {
                Text = "Submit",
                Size = new Size(80, 24),
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Size = new Size(80, 24),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };
            buttonPanel.Controls.Add(btnSubmit);
            buttonPanel.Controls.Add(btnClear);

            table.Controls.Add(buttonPanel, 11, 1);
            table.SetColumnSpan(buttonPanel, 2);

            // === Bottom section: Message label + multiline input next to it ===
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30, //  bottom panel height
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(10, 5, 10, 5) //  bottom padding
            };

            var lblMessageCaption = new Label
            {
                Text = "Message:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(10, 10)
            };

            lblMessage = new TextBox
            {
                Multiline = true,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(350, 20), //  message input height
                Location = new Point(lblMessageCaption.Right + 2, 8),
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            bottomPanel.Controls.Add(lblMessageCaption);
            bottomPanel.Controls.Add(lblMessage);
            this.Controls.Add(bottomPanel);
        }

        private void cmbInstrName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedInstr = cmbInstrName.SelectedItem?.ToString();

            // For FUTSTK or FUTIDX, strike price & option type are not applicable
            bool isOptionType = !(selectedInstr == "FUTSTK" || selectedInstr == "FUTIDX");

            txtStrikePrice.Enabled = isOptionType;
            cmbOptionType.Enabled = isOptionType;

            if (!isOptionType)
            {
                // Clear values when not applicable
                txtStrikePrice.Text = string.Empty;
                cmbOptionType.SelectedIndex = -1;
            }
        }

        private Label AddLabel(Control parent, string text, int x, int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Width = width,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F)
            };
            parent.Controls.Add(label);
            return label;
        }

        private TextBox AddTextBox(Control parent, int x, int y, int width)
        {
            var textBox = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };
            parent.Controls.Add(textBox);
            return textBox;
        }

        private ComboBox AddComboBox(Control parent, int x, int y, int width)
        {
            var comboBox = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = TextBoxBackColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            parent.Controls.Add(comboBox);
            return comboBox;
        }

        private void LoadScripDetails()
        {
            if (_scrip != null)
            {
                txtSymbol.Text = _scrip.Symbol;
                cmbExchange.SelectedItem = _scrip.Exchange;
                try
                {
                    // Set series based on instrument type
                    if (_scrip.InstrumentType.Contains("FUT"))
                        cmbSeries.SelectedItem = "FUT";
                    else if (_scrip.InstrumentType.Contains("OPT"))
                        cmbSeries.SelectedItem = "OPT";
                    else
                        cmbSeries.SelectedItem = "EQ";
                }
                catch (Exception ex)
                {
                    cmbSeries.SelectedItem = "EQ";
                }
            }
        }

        private void CmbSeries_SelectedIndexChanged(object sender, EventArgs e)
        {
            var isDerivative = cmbSeries.SelectedItem?.ToString() != "EQ";
            dtpExpiry.Enabled = isDerivative;
            txtStrikePrice.Enabled = cmbSeries.SelectedItem?.ToString() == "OPT";
            cmbOptionType.Enabled = cmbSeries.SelectedItem?.ToString() == "OPT";
        }

        private void CmbOrderType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var orderType = cmbOrderType.SelectedItem?.ToString();

            txtPrice.Enabled = orderType == "LIMIT" || orderType == "SL";
            txtTriggerPrice.Enabled = orderType == "SL" || orderType == "SL-M";

            if (orderType == "MARKET" || orderType == "SL-M")
            {
                txtPrice.Text = "0.00";
            }
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                btnSubmit.Enabled = false;
                lblMessage.Text = "Placing order...";
                lblMessage.ForeColor = Color.Yellow;

                // Validate inputs
                if (!ValidateInputs())
                {
                    btnSubmit.Enabled = true;
                    return;
                }

                // Create order request
                var orderRequest = CreateOrderRequest();

                // Place order
                var response = await _orderService.PlaceOrderAsync(orderRequest);

                if (response?.Status == "Ok")
                {
                    lblMessage.Text = $"✓ Order placed successfully! Order ID: {response}";
                    lblMessage.ForeColor = Color.LightGreen;

                    _logger.LogInformation($"Order placed: {response}");

                    // Close form after 2 seconds
                    await System.Threading.Tasks.Task.Delay(2000);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblMessage.Text = $"✗ Order failed: {response}";
                    lblMessage.ForeColor = Color.Red;
                //    _logger.LogError($"Order failed: {response?.emsg}");
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"✗ Error: {ex.Message}";
                lblMessage.ForeColor = Color.Red;
                _logger.LogError("Error placing order", ex);
            }
            finally
            {
                btnSubmit.Enabled = true;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtSymbol.Text))
            {
                ShowError("Symbol is required");
                return false;
            }

            if (nudQuantity.Value <= 0)
            {
                ShowError("Quantity must be greater than 0");
                return false;
            }

            var orderType = cmbOrderType.SelectedItem?.ToString();
            if ((orderType == "LIMIT" || orderType == "SL") && !decimal.TryParse(txtPrice.Text, out var price))
            {
                ShowError("Valid price is required for limit orders");
                return false;
            }

            if ((orderType == "SL" || orderType == "SL-M") && !decimal.TryParse(txtTriggerPrice.Text, out var triggerPrice))
            {
                ShowError("Valid trigger price is required for stop loss orders");
                return false;
            }

            return true;
        }

        private OrderRequest CreateOrderRequest()
        {
            var orderType = cmbOrderType.SelectedItem?.ToString();

            return new OrderRequest
            {
                tradingsymbol = txtSymbol.Text,
                symbolname = txtSymbol.Text,
                exchange = cmbExchange.SelectedItem?.ToString(),
                transtype = _transactionType.ToString(),
                qty = (int)nudQuantity.Value,
                priceType = ConvertOrderType(orderType),
                price = txtPrice.Enabled ? txtPrice.Text : "0",
                triggerPrice = txtTriggerPrice.Enabled ? txtTriggerPrice.Text : "0",
                product = ConvertProductType(cmbProductType.SelectedItem?.ToString()),
                transType = "B",                     
                ret = cmbValidity.SelectedItem?.ToString()
            };

  
        }

    
        private string ConvertOrderType(string orderType)
        {
            switch (orderType)
            {
                case "MARKET":
                    return "MKT";
                case "LIMIT":
                    return "L";
                case "SL":
                    return "SL-LMT";
                case "SL-M":
                    return "SL-MKT";
                default:
                    return "MKT";
            }
        }

        private string ConvertProductType(string productType)
        {
            switch (productType)
            {
                case "INTRADAY":
                    return "I";
                case "DELIVERY":
                    return "C";
                case "CARRYFORWARD":
                    return "M";
                default:
                    return "I";
            }
        }

        private void ShowError(string message)
        {
            lblMessage.Text = $"✗ {message}";
            lblMessage.ForeColor = Color.Red;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            nudQuantity.Value = 1;
            txtPrice.Text = "0.00";
            txtTriggerPrice.Text = "0.00";
            cmbOrderType.SelectedIndex = 0;
            cmbProductType.SelectedIndex = 0;
            cmbValidity.SelectedIndex = 0;
            lblMessage.Text = "";
        }
    }
}