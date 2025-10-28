using SqliteWrapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using TWS.Domain.Models;


namespace TWS
{
    public partial class OrderBookForm : Form
    {
        private SQLiteRepositoryAsync<Order> _repo;

        public OrderBookForm()
        {
            InitializeComponent();
            string dbPath = @"Data Source=orders.db;";
            var dbContext = new SqliteDbContext(dbPath);
            _repo = new SQLiteRepositoryAsync<Order>(dbContext);

            // Setup DataGridView
            dataGridView1.VirtualMode = false; // Set to true only if you're handling CellValueNeeded
            dataGridView1.AutoGenerateColumns = true;
        }

        private async void OrderBookForm_Load(object sender, EventArgs e)
        {
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            var orders = await _repo.QueryAsync("SELECT * FROM Orders ORDER BY Timestamp DESC");
            dataGridView1.DataSource = orders;
        }

        private async void btnAddOrder_Click(object sender, EventArgs e)
        {
            string sql = "INSERT INTO Orders (Symbol, Price, Quantity, OrderType, Timestamp) VALUES (@Symbol, @Price, @Qty, @Type, @Time)";
            var parameters = new Dictionary<string, object>
        {
            { "@Symbol", "BANKNIFTY" },
            { "@Price", 49500 },
            { "@Qty", 15 },
            { "@Type", "BUY" },
            { "@Time", DateTime.Now }
        };

            await _repo.ExecuteAsync(sql, parameters);
            await LoadOrdersAsync(); // Refresh after insert
        }

    }
}