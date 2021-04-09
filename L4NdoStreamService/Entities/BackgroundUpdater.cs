using System;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
{
    public abstract class BackgroundUpdater: IDisposable
    {
        protected int UpdatesPerSecond { get; set; }

        private Task _updateTask;
        private CancellationTokenSource _cancellationTokenSource;

        protected abstract void Update(TimeSpan timeSinceLastUpdate);

        protected void StartUpdates()
        {
            if (_updateTask != null && !_updateTask.IsCompleted && !_updateTask.IsFaulted)
                return;

            this.StopUpdates();
            _cancellationTokenSource = new CancellationTokenSource();

            _updateTask = Task.Run(() =>
            {
                var before = DateTime.Now;
                var time = DateTime.Now - before;
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    time = DateTime.Now - before;
                    before = DateTime.Now;

                    this.Update(time);

                    Thread.Sleep(1000 / this.UpdatesPerSecond);
                }
            }, this._cancellationTokenSource.Token);
        }

        protected void StopUpdates()
        {
            this._cancellationTokenSource?.Cancel();

            try
            {
                _updateTask?.Dispose();
            }
            finally
            {
                _updateTask = null;
            }
        }

        public virtual void Dispose() =>
            this.StopUpdates();
    }
}
