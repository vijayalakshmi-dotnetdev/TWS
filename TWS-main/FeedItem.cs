using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWS
{
    using System.ComponentModel;

    public class FeedItem : INotifyPropertyChanged
    {
        private string _tk;
        private string _lp;
        private string _pc;
        private string _ts;

        public string tk { get => _tk; set { _tk = value; OnPropertyChanged(nameof(tk)); } }
        public string lp { get => _lp; set { _lp = value; OnPropertyChanged(nameof(lp)); } }
        public string pc { get => _pc; set { _pc = value; OnPropertyChanged(nameof(pc)); } }
        public string ts { get => _ts; set { _ts = value; OnPropertyChanged(nameof(ts)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


}