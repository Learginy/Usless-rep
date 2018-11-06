
using System;
using System.ComponentModel;
using System.Windows.Media;

namespace WpfStatistics
{
    class StatisticViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Brush _backgroundColor;
        private Brush _foregroundColor;
        private string _value;

        public StatisticViewModel(string key)
        {
            Key = key;
            _backgroundColor = Brushes.White;
            _foregroundColor = Brushes.Black;

        }

        private void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public string Key { get; set; }
        public string AlertId { get; set; }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                RaisePropertyChanged("Value");
            }
        }

        public Brush BackgroundColor
        {
            get
            {
                return _backgroundColor;
            }
            set
            {
                _backgroundColor = value;
                RaisePropertyChanged("BackgroundColor");
            }
        }

        public Brush ForegroundColor
        {
            get
            {
                return _foregroundColor;
            }
            set
            {
                _foregroundColor = value;
                RaisePropertyChanged("ForegroundColor");
            }
        }

        public void Reset()
        {
            ForegroundColor = Brushes.Black;
            BackgroundColor = Brushes.White;
            AlertId = String.Empty;
        }
    }
}
