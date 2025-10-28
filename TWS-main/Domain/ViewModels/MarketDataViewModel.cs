using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TWS.Domain.Models;

namespace TWS.Domain.ViewModels
{
    /// <summary>
    /// ViewModel for binding market data to UI controls
    /// Implements INotifyPropertyChanged for automatic UI updates
    /// </summary>
    public class MarketDataViewModel : INotifyPropertyChanged
    {
        private Scrip _scrip;
        private decimal _ltp;
        private decimal _change;
        private decimal _changePercent;
        private long _volume;
        private decimal _bidPrice;
        private decimal _askPrice;
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private decimal _close;
        private DateTime _lastUpdateTime;

        public event PropertyChangedEventHandler PropertyChanged;

        // ✅ Scrip reference
        public Scrip Scrip
        {
            get => _scrip;
            set
            {
                if (_scrip != value)
                {
                    _scrip = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Symbol));
                    OnPropertyChanged(nameof(Exchange));
                    OnPropertyChanged(nameof(Token));
                }
            }
        }

        // ✅ Computed properties from Scrip
        public string Symbol => Scrip?.Symbol ?? string.Empty;
        public string Exchange => Scrip?.Exchange ?? string.Empty;
        public string Token => Scrip?.Token ?? string.Empty;

        // ✅ Market data properties with change notification
        public decimal LTP
        {
            get => _ltp;
            set
            {
                if (_ltp != value)
                {
                    _ltp = value;
                    OnPropertyChanged();
                    LastUpdateTime = DateTime.Now;
                }
            }
        }

        public decimal Change
        {
            get => _change;
            set
            {
                if (_change != value)
                {
                    _change = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal ChangePercent
        {
            get => _changePercent;
            set
            {
                if (_changePercent != value)
                {
                    _changePercent = value;
                    OnPropertyChanged();
                }
            }
        }

        public long Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal BidPrice
        {
            get => _bidPrice;
            set
            {
                if (_bidPrice != value)
                {
                    _bidPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AskPrice
        {
            get => _askPrice;
            set
            {
                if (_askPrice != value)
                {
                    _askPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Open
        {
            get => _open;
            set
            {
                if (_open != value)
                {
                    _open = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal High
        {
            get => _high;
            set
            {
                if (_high != value)
                {
                    _high = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Low
        {
            get => _low;
            set
            {
                if (_low != value)
                {
                    _low = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Close
        {
            get => _close;
            set
            {
                if (_close != value)
                {
                    _close = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                if (_lastUpdateTime != value)
                {
                    _lastUpdateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ✅ Updates all properties from a TickData object
        /// </summary>
        public void UpdateFromTick(TickData tick)
        {
            if (tick == null) return;

            // Verify this is the correct scrip
            if (Scrip != null && (Scrip.Token != tick.Token || Scrip.Exchange != tick.Exchange))
            {
                return; // This tick is not for this scrip
            }

            // Update all available properties
            LTP = tick.LastPrice;
            Volume = tick.Volume;
            BidPrice = tick.BuyPrice;
            AskPrice = tick.SellPrice;
            Open = tick.OpenPrice;
            High = tick.HighPrice;
            Low = tick.LowPrice;
            Close = tick.ClosePrice;

            // Calculate change if we have close price
            if (Close > 0 && LTP > 0)
            {
                Change = LTP - Close;
                ChangePercent = (Change / Close) * 100;
            }

            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// ✅ Helper method to raise PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Symbol} ({Exchange}) LTP: {LTP:N2}";
        }
    }
}