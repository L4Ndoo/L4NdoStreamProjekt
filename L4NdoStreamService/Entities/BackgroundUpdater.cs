using System;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
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
        private Timer _timer;

        protected abstract void Update();

        protected void StartUpdates()
        {
            if (_timer != null)
                return;

            this.StopUpdates();

            _timer = new Timer(state => this.Update(), null, 0, 1000 / this.UpdatesPerSecond);
        }

        protected void StopUpdates()
        {
            try
            {
                _timer?.Dispose();
            }
            finally
            {
                _timer = null;
            }
        }

        public virtual void Dispose() =>
            this.StopUpdates();
    }
}
