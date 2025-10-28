// Presentation/LoginForm.cs
using System;
using System.Drawing;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using System.Windows.Forms;
using TWS.Infrastructure.DependencyInjection;
using TWS.Presentation.Forms;
using TWS.Services.Interfaces;
using WebSocketSharp;


namespace TWS.Presentation.Forms
{
    public partial class LoginForm : Form
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger _logger;

        private TextBox _txtUserId;
        private TextBox _txtPassword;
        private TextBox _txtOtp;
        private TextBox _txtDeviceNumber;
        private Button _btnLogin;
        private Button _btnValidateOtp;
        private Label _lblStatus;
        private Panel _otpPanel;
        private string _tempToken;

        public LoginForm()
        {
            _authService = ServiceLocator.GetService<IAuthenticationService>();
            _logger = ServiceLocator.GetService<ILogger>();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "TWS - Login";
            this.Size = new Size(550, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            var lblTitle = new Label
            {
                Text = "Trader Workstation",
                Location = new Point(50, 20),
                Size = new Size(350, 30),
                Font = new Font("Arial", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // User ID
            var lblUserId = new Label
            {
                Text = "User ID:",
                Location = new Point(50, 70),
                Size = new Size(100, 20)
            };
            _txtUserId = new TextBox
            {
                Location = new Point(160, 68),
                Size = new Size(200, 25)
            };

            // Password
            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(50, 110),
                Size = new Size(100, 20)
            };
            _txtPassword = new TextBox
            {
                Location = new Point(160, 108),
                Size = new Size(200, 25),
                PasswordChar = '*'
            };

            // Device Number
            var lblDevice = new Label
            {
                Text = "Device ID:",
                Location = new Point(50, 150),
                Size = new Size(100, 20)
            };
            _txtDeviceNumber = new TextBox
            {
                Location = new Point(160, 148),
                Size = new Size(200, 25),
                Text = "69c8a62f5e739c5b9bdd620653e4a03e" // Default device ID
            };

            // Login Button
            _btnLogin = new Button
            {
                Text = "Send OTP",
                Location = new Point(160, 190),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnLogin.Click += OnLoginClick;

            // OTP Panel (Initially Hidden)
            _otpPanel = new Panel
            {
                Location = new Point(30, 230),
                Size = new Size(380, 70),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblOtp = new Label
            {
                Text = "Enter OTP:",
                Location = new Point(20, 15),
                Size = new Size(80, 20)
            };
            _txtOtp = new TextBox
            {
                Location = new Point(110, 13),
                Size = new Size(150, 25)
            };
            _btnValidateOtp = new Button
            {
                Text = "Login",
                Location = new Point(270, 10),
                Size = new Size(90, 30),
                BackColor = Color.Green,
                ForeColor = Color.White
            };
            _btnValidateOtp.Click += OnValidateOtpClick;

            _otpPanel.Controls.Add(lblOtp);
            _otpPanel.Controls.Add(_txtOtp);
            _otpPanel.Controls.Add(_btnValidateOtp);

            // Status Label
            _lblStatus = new Label
            {
                Location = new Point(50, 305),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red
            };

            // Add controls to form
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblUserId);
            this.Controls.Add(_txtUserId);
            this.Controls.Add(lblPassword);
            this.Controls.Add(_txtPassword);
            this.Controls.Add(lblDevice);
            this.Controls.Add(_txtDeviceNumber);
            this.Controls.Add(_btnLogin);
            this.Controls.Add(_otpPanel);
            this.Controls.Add(_lblStatus);

            this.ResumeLayout();
        }

        private async void OnLoginClick(object sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "Authenticating...";
                _lblStatus.ForeColor = Color.Blue;
                _btnLogin.Enabled = false;

                var userId = _txtUserId.Text.Trim();
                var password = _txtPassword.Text;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
                {
                    _lblStatus.Text = "Please enter User ID and Password";
                    _lblStatus.ForeColor = Color.Red;
                    _btnLogin.Enabled = true;
                    return;
                }

                var result = await _authService.LoginAsync(userId, password);

                if (result.IsSuccess)
                {
                    _tempToken = result.AccessToken;
                    // ✅ STEP 2: Send OTP (automatically after successful login)
                    _lblStatus.Text = "Sending OTP...";

                    var sendOtpResult = await _authService.RequestOTPAsync(userId);

                    if (sendOtpResult.Success)
                    {
                        // ✅ Store token for OTP validation
                       
                      //  _otpToken = sendOtpResult.Token;
                     //   _tokenTimestamp = DateTime.Now;

                        _lblStatus.Text = "OTP sent! Check your phone.";
                        _lblStatus.ForeColor = Color.Green;

                        _logger.LogInformation("OTP sent successfully");

                        // ✅ Show OTP input panel
                        _otpPanel.Visible = true;
                        _txtOtp.Focus();
                    }
                    else
                    {
                        _lblStatus.Text = $"Failed to send OTP: {sendOtpResult.Message}";
                        _lblStatus.ForeColor = Color.Red;
                        _btnLogin.Enabled = true;
                    }
                    _otpPanel.Visible = true;
                    _txtOtp.Focus();
                }
                else
                {
                    _lblStatus.Text = result.ErrorMessage;
                    _lblStatus.ForeColor = Color.Red;
                    _btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Login error", ex);
                _lblStatus.Text = $"Error: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                _btnLogin.Enabled = true;
            }
        }

        private async void OnValidateOtpClick(object sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "Validating OTP...";
                _lblStatus.ForeColor = Color.Blue;
                _btnValidateOtp.Enabled = false;

                var otp = _txtOtp.Text.Trim();
                var deviceNumber = _txtDeviceNumber.Text.Trim();
                var userid = _txtUserId.Text.Trim();

                if (string.IsNullOrEmpty(otp))
                {
                    _lblStatus.Text = "Please enter OTP";
                    _lblStatus.ForeColor = Color.Red;
                    _btnValidateOtp.Enabled = true;
                    return;
                }

                var result = await _authService.ValidateOTPAsync(userid, otp, deviceNumber);

                if (result.IsSuccess)
                {
                    _lblStatus.Text = "Login successful!";
                    _lblStatus.ForeColor = Color.Green;
                    _logger.LogInformation("User logged in successfully");

                    // ✅ Get auth token from result
                    string authToken = result.AccessToken;

                    await Task.Delay(500);

                    // ✅ Hide login form first
                    this.Hide();

                    // ✅ Create MainForm with auth token
                    var mainForm = new MainForm(authToken);

                    // ✅ Exit app when MainForm closes
                    mainForm.FormClosed += (s, args) => Application.Exit();

                    // ✅ Show MainForm (not ShowDialog)
                    mainForm.Show();

                    // ✅ Removed: this.Close() - not needed
                }

                else
                {
                    _lblStatus.Text = result.ErrorMessage;
                    _lblStatus.ForeColor = Color.Red;
                    _btnValidateOtp.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("OTP validation error", ex);
                _lblStatus.Text = $"Error: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                _btnValidateOtp.Enabled = true;
            }
        }


    }
}