using System;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.Utilities
{
    public abstract class BackgroundUpdater: IDisposable
    {
        protected int UpdatesPerSecond 
        {
            get => _updatesPerSecond;
            set
            {
                _timer?.Change(0, 1000 / value);
                _updatesPerSecond = value;
            }
        }

        private int _updatesPerSecond;
        private Task _updateTask;
        private Timer _timer;

        protected abstract Task Update();

        protected void StartUpdates()
        {
            if (_timer != null)
                return;

            this.StopUpdates();

            _timer = new Timer(this.Update, this, 0, 1000 / this.UpdatesPerSecond);
        }

        protected void StopUpdates()
        {
            try { _timer?.Dispose(); }
            finally { _timer = null; }
        }

        private void Update(object state)
        {
            if (this._updateTask == null || this._updateTask.IsCompleted)
            {
                this._updateTask = this.Update();
            }
        }

        public virtual void Dispose() =>
            this.StopUpdates();
    }
}
