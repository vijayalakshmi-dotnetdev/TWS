using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using TWS.Domain.Models;
using static TWS.Domain.Models.OrderEnums;

namespace TWS.Presentation.Forms
{
    public partial class BasketOrderForm : Form
    {
        public BasketOrderForm()
        {
            // === FORM SETTINGS ===
            Text = "Basket Order";
            Size = new Size(1020, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9);
            BackColor = Color.White;

            int topMargin = 20;
            int leftMargin = 20;
            int colSpacing = 5;

            // === COLUMN SETUP ===
            int[] colWidthsRow1 = { 90, 100, 100, 150, 100, 100, 130, 100, 60 };
            int[] colWidthsRow2 = { 90, 100, 100, 150, 100, 100, 300 };

            List<int> colPositions = new List<int>();
            int currentX = leftMargin;
            for (int i = 0; i < colWidthsRow1.Length; i++)
            {
                colPositions.Add(currentX);
                currentX += colWidthsRow1[i] + colSpacing;
            }

            // === ROW 1 ===
            Label lblTransType = MakeLabel("Trans Type", colPositions[0], topMargin);
            ComboBox cbTransType = MakeComboBox("cbTransType", colPositions[0], topMargin + 20, colWidthsRow1[0], null);
            LoadEnumToComboBox<OrderEnums.TransactionType>(cbTransType);
            cbTransType.SelectedIndex = 0;

            Label lblExchange = MakeLabel("Exchange", colPositions[1], topMargin);
            ComboBox cbExchange = MakeComboBox("cbExchange", colPositions[1], topMargin + 20, colWidthsRow1[1], new[] { "NSE", "BSE", "MCX", "NCDEX" });

            Label lblInstrument = MakeLabel("Instruments", colPositions[2], topMargin);
            ComboBox cbInstrument = MakeComboBox("cbInstrument", colPositions[2], topMargin + 20, colWidthsRow1[2], new[] { "EQ" });

            Label lblSymbol = MakeLabel("Symbol", colPositions[3], topMargin);
            Panel pnlSymbol = MakeSearchPanel("pnlSymbol", colPositions[3], topMargin + 20, colWidthsRow1[3], "");

            Label lblOptionType = MakeLabel("Option Type", colPositions[4], topMargin);
            ComboBox cbOptionType = MakeComboBox("cbOptionType", colPositions[4], topMargin + 20, colWidthsRow1[4], new[] { "CE", "PE" });

            Label lblStrike = MakeLabel("Strike Price", colPositions[5], topMargin);
            ComboBox cbStrike = MakeComboBox("cbStrike", colPositions[5], topMargin + 20, colWidthsRow1[5],
                new[] { "1500", "1600", "1700", "1800", "1900" });
            cbStrike.SelectedIndex = 2;

            Label lblExpiry = MakeLabel("Expiry Date", colPositions[6], topMargin);
            DateTimePicker dtExpiry = new DateTimePicker
            {
                Name = "dtExpiry",
                Location = new Point(colPositions[6], topMargin + 20),
                Width = colWidthsRow1[6],
                Format = DateTimePickerFormat.Short
            };

            Label lblDuration = MakeLabel("Duration", colPositions[7], topMargin);
            ComboBox cbDuration = MakeComboBox("cbDuration", colPositions[7], topMargin + 20, colWidthsRow1[7], new[] { "DAY", "IOC" });

            Label lblMktProt = MakeLabel("MKT Prot%", colPositions[8], topMargin);
            TextBox txtMktProt = MakeTextBox("txtMktProt", colPositions[8], topMargin + 20, colWidthsRow1[8], "0");

            Controls.AddRange(new Control[]
            {
                lblTransType, cbTransType,
                lblExchange, cbExchange,
                lblInstrument, cbInstrument,
                lblSymbol, pnlSymbol,
                lblOptionType, cbOptionType,
                lblStrike, cbStrike,
                lblExpiry, dtExpiry,
                lblDuration, cbDuration,
                lblMktProt, txtMktProt
            });

            // === ROW 2 ===
            topMargin += 50;

            Label lblProducts = MakeLabel("Products", colPositions[0], topMargin);
            ComboBox cbProducts = MakeComboBox("cbProducts", colPositions[0], topMargin + 20, colWidthsRow2[0], new[] { "MIS", "CNC" });

            Label lblOrderType = MakeLabel("Order Type", colPositions[1], topMargin);
            ComboBox cbOrderType = MakeComboBox("cbOrderType", colPositions[1], topMargin + 20, colWidthsRow2[1], new[] { "LMT", "MKT" });

            Label lblQty = MakeLabel("Qty", colPositions[2], topMargin);
            TextBox txtQty = MakeTextBox("txtQty", colPositions[2], topMargin + 20, colWidthsRow2[2], "1");

            Label lblPrice = MakeLabel("Price", colPositions[3], topMargin);
            TextBox txtPrice = MakeTextBox("txtPrice", colPositions[3], topMargin + 20, colWidthsRow2[3], "");

            Label lblTrigger = MakeLabel("Trigger Price", colPositions[4], topMargin);
            TextBox txtTrigger = MakeTextBox("txtTrigger", colPositions[4], topMargin + 20, colWidthsRow2[4], "");

            Label lblDisc = MakeLabel("Disc Qty", colPositions[5], topMargin);
            TextBox txtDisc = MakeTextBox("txtDisc", colPositions[5], topMargin + 20, colWidthsRow2[5], "0");

            Label lblAccount = MakeLabel("Account", colPositions[6], topMargin);
            Panel pnlAccount = MakeSearchPanel("pnlAccount", colPositions[6], topMargin + 20, colWidthsRow2[6], "");

            CheckBox chkClientSet = new CheckBox
            {
                Text = "ClientSet",
                AutoSize = true,
                Location = new Point(colPositions[6] + colWidthsRow2[6] - 80, topMargin)
            };

            Controls.AddRange(new Control[]
            {
                lblProducts, cbProducts,
                lblOrderType, cbOrderType,
                lblQty, txtQty,
                lblPrice, txtPrice,
                lblTrigger, txtTrigger,
                lblDisc, txtDisc,
                lblAccount, pnlAccount, chkClientSet
            });

            // === REMARKS ROW ===
            topMargin += 30;
            TextBox txtRemarks = MakeTextBox("txtRemarks", colPositions[0], topMargin + 20, 790, "");

            Button btnAdd = MakeButton("+ Add", txtRemarks.Right + 5, txtRemarks.Top - 1, 70);
            btnAdd.BackColor = Color.FromArgb(0, 120, 215); // Blue background (Windows-style)
            btnAdd.ForeColor = Color.White;                 // White text
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;          // Clean look
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Click += BtnAdd_Click;

            Button btnPlace = MakeButton("Place Basket", btnAdd.Right + 5, txtRemarks.Top - 1, 100);
            btnPlace.Font = new Font("Segoe UI", 9, FontStyle.Bold); // make font bold
            btnPlace.Click += btnPlace_Click;

            Controls.AddRange(new Control[] { txtRemarks, btnAdd, btnPlace });

            // === GRID ===

            DataGridView dgv = new DataGridView
            {
                Location = new Point(20, topMargin + 50),
                Size = new Size(970, 300),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, // Disable "fill"
                BackgroundColor = Color.White,
                ScrollBars = ScrollBars.Both, // Enable both scrollbars
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Exchange", HeaderText = "Exchange" },
                new DataGridViewTextBoxColumn { Name = "Instr", HeaderText = "Instr" },
                new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "Symbol" },
                new DataGridViewTextBoxColumn { Name = "OptionType", HeaderText = "OptionType" },
                new DataGridViewTextBoxColumn { Name = "Strike", HeaderText = "Strike" },
                new DataGridViewTextBoxColumn { Name = "ExpDate", HeaderText = "ExpDate" },
                new DataGridViewTextBoxColumn { Name = "AccountId", HeaderText = "AccountId" },
                new DataGridViewTextBoxColumn { Name = "BUY/SELL", HeaderText = "BUY/SELL" },
                new DataGridViewTextBoxColumn { Name = "CustFirm", HeaderText = "CustFirm" },
                new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "Product" },
                new DataGridViewTextBoxColumn { Name = "PriceType", HeaderText = "PriceType" },
                new DataGridViewTextBoxColumn { Name = "OrdDuration", HeaderText = "OrdDuration" },
                new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Price" },
                new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty" },
                new DataGridViewTextBoxColumn { Name = "TriggerPrice", HeaderText = "TriggerPrice" },
                new DataGridViewTextBoxColumn { Name = "DiscQty", HeaderText = "DiscQty" },
                new DataGridViewTextBoxColumn { Name = "Remarks", HeaderText = "Remarks" },
                new DataGridViewTextBoxColumn { Name = "MKT Prot%", HeaderText = "MKT Prot%" },
                new DataGridViewTextBoxColumn { Name = "PCode", HeaderText = "PCode" },
                new DataGridViewTextBoxColumn { Name = "OrderValue", HeaderText = "OrderValue" }
            );

            Controls.Add(dgv);

            // === BOTTOM BAR ===

            // Common size for all three controls
            int controlHeight = 26;
            int controlWidth = 100;

            // Resize the reset icon before assigning (small and balanced)
            Image resetIcon = new Bitmap(Properties.Resources.reset, new Size(14, 14));

            // Reset icon + text button (F5)
            Button btnReset = new Button
            {
                Text = "Reset (F5)",
                Location = new Point(20, dgv.Bottom + 8),
                Size = new Size(controlWidth, controlHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Image = resetIcon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 0, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold) // Bold font
            };

            btnReset.FlatAppearance.BorderSize = 1;
            btnReset.FlatAppearance.BorderColor = Color.LightGray;
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
            btnReset.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 230, 230);
            btnReset.Click += BtnReset_Click;

            // Tooltip for reset
            ToolTip ttReset = new ToolTip();
            ttReset.SetToolTip(btnReset, "Reset (F5)");

            // Basket File combo box (Import / Export)
            ComboBox cbBasketFile = new ComboBox
            {
                Location = new Point(btnReset.Right + 5, dgv.Bottom + 8),
                Size = new Size(controlWidth, controlHeight),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9, FontStyle.Bold) // Bold font
            };
            cbBasketFile.Items.AddRange(new[] { "Basket File", "Import", "Export" });
            cbBasketFile.SelectedIndex = 0;

            // Text box for uploaded path (same height)
            TextBox txtFilePath = new TextBox
            {
                Name = "txtFilePath",   
                Location = new Point(cbBasketFile.Right + 5, dgv.Bottom + 8),
                Size = new Size(740, controlHeight),
                ReadOnly = true,
                Text = ""
            };

            // Label to show number of imported files
            Label lblImportCount = new Label
            {
                Name = "lblImportCount",
                Location = new Point(txtFilePath.Right + 5, txtFilePath.Top + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Black,
                Text = "0"
            };

            Controls.Add(lblImportCount);

            // Add controls to form
            Controls.AddRange(new Control[]
            {
                btnReset,
                cbBasketFile,
                txtFilePath,
                lblImportCount
            });

            // === EVENT: Basket File Import/Export ===
            cbBasketFile.SelectedIndexChanged += CbBasketFile_SelectedIndexChanged;

            // === FOOTER LABEL (Full Yellow Border + Working 3 Vertical Dividers) ===
            int footerTop = dgv.Bottom + 36;
            int footerLeft = dgv.Left;
            int footerHeight = 26;

            // Create one full-width footer panel
            Panel pnlFooter = new Panel
            {
                Location = new Point(footerLeft, footerTop),
                Size = new Size(dgv.Width, footerHeight),
                BorderStyle = BorderStyle.None
            };

            // Draw yellow border + 3 working vertical dividers
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen yellowPen = new Pen(Color.Yellow, 2))
                {
                    Rectangle r = pnlFooter.ClientRectangle;
                    r.Width -= 1;
                    r.Height -= 1;

                    // Outer border
                    e.Graphics.DrawRectangle(yellowPen, r);

                    // Inner dividers (4 sections → 3 dividers)
                    if (r.Width > 0)
                    {
                        float sectionWidth = r.Width / 4f;
                        for (int i = 1; i <= 3; i++)
                        {
                            float x = sectionWidth * i;
                            e.Graphics.DrawLine(yellowPen, x, 0, x, r.Height);
                        }
                    }
                }
            };

            // Redraw on resize to keep dividers visible
            pnlFooter.Resize += (s, e) => pnlFooter.Invalidate();

            // Add summary labels
            Label lblBuyQty = new Label
            {
                Text = "BUY QTY: 0",
                AutoSize = false,
                Location = new Point(10, 2),
                Size = new Size(pnlFooter.Width / 4 - 10, footerHeight - 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            Label lblBuyVal = new Label
            {
                Text = "BUY VAL: 0.00",
                AutoSize = false,
                Location = new Point(pnlFooter.Width / 4 + 10, 2),
                Size = new Size(pnlFooter.Width / 4 - 10, footerHeight - 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            Label lblSellQty = new Label
            {
                Text = "SELL QTY: 0",
                AutoSize = false,
                Location = new Point(pnlFooter.Width / 2 + 10, 2),
                Size = new Size(pnlFooter.Width / 4 - 10, footerHeight - 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Red
            };

            Label lblSellVal = new Label
            {
                Text = "SELL VAL: 0.00",
                AutoSize = false,
                Location = new Point(pnlFooter.Width * 3 / 4 + 10, 2),
                Size = new Size(pnlFooter.Width / 4 - 10, footerHeight - 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Red
            };

            // Add labels inside footer panel
            pnlFooter.Controls.AddRange(new Control[] { lblBuyQty, lblBuyVal, lblSellQty, lblSellVal });

            // Add footer panel to form
            Controls.Add(pnlFooter);


        }

        // === Helper method ===
        private void LoadEnumToComboBox<T>(ComboBox combo)
        {
            combo.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(T)))
            {
                combo.Items.Add(value);
            }
        }

        // === EVENTS ===
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // Get references to the relevant controls
            DataGridView dgv = Controls["dataGridView1"] as DataGridView;
            if (dgv == null)
            {
                dgv = null;
                foreach (Control ctrl in Controls)
                    if (ctrl is DataGridView grid)
                        dgv = grid;
            }

            if (dgv == null)
            {
                MessageBox.Show("Grid not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Collect values from the UI
            string exchange = ((ComboBox)Controls.Find("cbExchange", true)[0]).Text;
            string instrument = ((ComboBox)Controls.Find("cbInstrument", true)[0]).Text;
            string optionType = ((ComboBox)Controls.Find("cbOptionType", true)[0]).Text;
            string strike = ((ComboBox)Controls.Find("cbStrike", true)[0]).Text;
            string transType = ((ComboBox)Controls.Find("cbTransType", true)[0]).Text;
            string products = ((ComboBox)Controls.Find("cbProducts", true)[0]).Text;
            string orderType = ((ComboBox)Controls.Find("cbOrderType", true)[0]).Text;
            string duration = ((ComboBox)Controls.Find("cbDuration", true)[0]).Text;

            // Panels (symbol/account)
            Panel pnlSymbol = Controls.Find("pnlSymbol", true)[0] as Panel;
            Panel pnlAccount = Controls.Find("pnlAccount", true)[0] as Panel;
            string symbol = ((TextBox)pnlSymbol.Controls[0]).Text;
            string account = ((TextBox)pnlAccount.Controls[0]).Text;

            // Textboxes
            string price = ((TextBox)Controls.Find("txtPrice", true)[0]).Text;
            string qty = ((TextBox)Controls.Find("txtQty", true)[0]).Text;
            string trigger = ((TextBox)Controls.Find("txtTrigger", true)[0]).Text;
            string disc = ((TextBox)Controls.Find("txtDisc", true)[0]).Text;
            string remarks = ((TextBox)Controls.Find("txtRemarks", true)[0]).Text;
            string mktProt = ((TextBox)Controls.Find("txtMktProt", true)[0]).Text;
            string expiry = ((DateTimePicker)Controls.Find("dtExpiry", true)[0]).Text;

            // Add row to DataGridView
            dgv.Rows.Add(exchange, instrument, symbol, optionType, strike, expiry, account,
                transType, "C", products, orderType, duration, price, qty, trigger, disc,
                remarks, mktProt, "", "");
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            foreach (Control ctrl in Controls)
            {
                if (ctrl is DataGridView dgv)
                {
                    dgv.Rows.Clear();
                }
                else if (ctrl is TextBox txt && txt.Name == "txtFilePath")
                {
                    txt.Clear();
                }
                else if (ctrl is Label lbl && lbl.Name == "lblImportCount")
                {
                    lbl.Text = "0";
                }
            }
        }

        private void btnPlace_Click(object sender, EventArgs e)
        {
        }

        private void CbBasketFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbBasketFile = sender as ComboBox;
            if (cbBasketFile == null || cbBasketFile.SelectedItem == null)
                return;

            if (cbBasketFile.SelectedItem.ToString() == "Import")
            {
                // find the TextBox, Label (for count), and DataGridView on the form
                TextBox txtFilePath = null;
                Label lblImportCount = null;
                DataGridView dgv = null;

                foreach (Control ctrl in Controls)
                {
                    if (ctrl is TextBox tb && tb.ReadOnly)
                        txtFilePath = tb;

                    if (ctrl is Label lbl && lbl.Name == "lblImportCount")
                        lblImportCount = lbl;

                    if (ctrl is DataGridView grid)
                        dgv = grid;
                }

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    ofd.Multiselect = true;

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string[] selectedFiles = ofd.FileNames;
                        txtFilePath.Text = string.Join("; ", selectedFiles);

                        try
                        {
                            dgv.Columns.Clear();
                            dgv.Rows.Clear();

                            bool headerCreated = false;

                            foreach (string filePath in selectedFiles)
                            {
                                string[] lines = System.IO.File.ReadAllLines(filePath);
                                if (lines.Length == 0)
                                    continue;

                                // Create columns once from the first file
                                if (!headerCreated)
                                {
                                    string[] headers = lines[0].Split(',');
                                    foreach (string header in headers)
                                    {
                                        string trimmed = header.Trim();
                                        if (!string.IsNullOrWhiteSpace(trimmed))
                                        {
                                            dgv.Columns.Add(trimmed, trimmed);
                                            dgv.Columns[trimmed].Width = 120;
                                        }
                                    }
                                    headerCreated = true;
                                }

                                // Add data rows
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    string[] cells = lines[i].Split(',');
                                    if (cells.Length < dgv.Columns.Count)
                                        Array.Resize(ref cells, dgv.Columns.Count);
                                    else if (cells.Length > dgv.Columns.Count)
                                        Array.Resize(ref cells, dgv.Columns.Count);
                                    dgv.Rows.Add(cells);
                                }
                            }

                            // Enable scrollbars
                            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                            dgv.ScrollBars = ScrollBars.Both;

                            // Show imported file count
                            if (lblImportCount != null)
                            {
                                lblImportCount.Text = $"{selectedFiles.Length}";
                                lblImportCount.ForeColor = Color.Black;
                            }

                            MessageBox.Show($"Successfully imported {selectedFiles.Length} file(s)!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error importing files:\n{ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                cbBasketFile.SelectedIndex = 0;
            }
            else if (cbBasketFile.SelectedItem.ToString() == "Export")
            {
                // find the TextBox and DataGridView on the form
                TextBox txtFilePath = null;
                DataGridView dgv = null;

                foreach (Control ctrl in Controls)
                {
                    if (ctrl is TextBox tb && tb.ReadOnly)
                        txtFilePath = tb;

                    if (ctrl is DataGridView grid)
                        dgv = grid;
                }

                if (dgv == null || dgv.Rows.Count == 0)
                {
                    MessageBox.Show("No data available to export.", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbBasketFile.SelectedIndex = 0;
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    sfd.FileName = "UFT_BasketExport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(sfd.FileName, false))
                            {
                                // Write column headers
                                for (int i = 0; i < dgv.Columns.Count; i++)
                                {
                                    sw.Write(dgv.Columns[i].HeaderText);
                                    if (i < dgv.Columns.Count - 1)
                                        sw.Write(",");
                                }
                                sw.WriteLine();

                                // Write rows
                                foreach (DataGridViewRow row in dgv.Rows)
                                {
                                    if (!row.IsNewRow)
                                    {
                                        for (int i = 0; i < dgv.Columns.Count; i++)
                                        {
                                            var value = row.Cells[i].Value?.ToString()?.Replace(",", " ") ?? "";
                                            sw.Write(value);
                                            if (i < dgv.Columns.Count - 1)
                                                sw.Write(",");
                                        }
                                        sw.WriteLine();
                                    }
                                }
                            }

                            txtFilePath.Text = sfd.FileName;

                            MessageBox.Show("Basket exported successfully!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error exporting file:\n{ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                // Reset selection back to default
                cbBasketFile.SelectedIndex = 0;
            }
        }


        // === Helper UI Builders ===
        private Label MakeLabel(string text, int x, int y) {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true };
            return lbl;
        }

        private TextBox MakeTextBox(string name, int x, int y, int width, string text)
        {
           var txt = new TextBox { Name = name, Location = new Point(x, y), Width = width, Text = text };
            return txt;
        }

        private ComboBox MakeComboBox(string name, int x, int y, int width, string[] items)
        {
            var cb = new ComboBox
            {
                Name = name,
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            if (items != null)
            {
                cb.Items.AddRange(items);
                cb.SelectedIndex = 0;
            }
            return cb;
        }

        private Panel MakeSearchPanel(string name, int x, int y, int width, string defaultText)
        {
            var panel = new Panel
            {
                Name = name,
                Location = new Point(x, y),
                Size = new Size(width, 26),
                BorderStyle = BorderStyle.FixedSingle
            };
            var txt = new TextBox { BorderStyle = BorderStyle.None, Location = new Point(5, 5), Width = width - 30, Text = defaultText , Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var icon = new PictureBox
            {
                Image = Properties.Resources.search,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Location = new Point(width - 22, 4),
                Size = new Size(18, 18),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(txt);
            panel.Controls.Add(icon);
            return panel;
        }

        private Button MakeButton(string text, int x, int y, int width)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Width = width, Height = 28 };
            return btn;
        }
    }
}
