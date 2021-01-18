using System;

namespace LeagueDeck.Core
{
    public class ProgressEventArgs : EventArgs
    {
        public uint Current { get; }
        public uint Total { get; }
        public double Percentage { get; }

        public ProgressEventArgs(uint current, uint total)
        {
            Current = current;
            Total = total;
            Percentage = (double)Current / Total * 100;
        }
    }

    public class UpdateProgressReporter
    {
        private static UpdateProgressReporter _instance;

        public event EventHandler<ProgressEventArgs> OnUpdateStarted;
        public event EventHandler<ProgressEventArgs> OnUpdateCompleted;
        public event EventHandler<ProgressEventArgs> OnUpdateProgress;

        private uint _current;
        public uint Current
        {
            get => _current;
            private set
            {
                _current = value;
                if (_current == _total)
                    OnUpdateCompleted?.Invoke(this, new ProgressEventArgs(_current, _total));
                else
                    OnUpdateProgress?.Invoke(this, new ProgressEventArgs(_current, _total));
            }
        }

        private uint _total;
        public uint Total
        {
            get => _total;
            set
            {
                if (_total == 0 && value > 0)
                    OnUpdateStarted?.Invoke(this, new ProgressEventArgs(_current, value));

                _total = value;
            }
        }

        private UpdateProgressReporter()
        { }

        public static UpdateProgressReporter GetInstance()
        {
            if(_instance == null)
            {
                _instance = new UpdateProgressReporter();
            }

            return _instance;
        }

        internal void IncrementCurrent()
        {
            this.Current++;
        }
    }
}