
using System;
using System.Threading;

namespace KittyHawk.MqttLib.Utilities
{
    public delegate void NetworkTimeoutTimerHandler(object sender, object data);

    internal class TimeoutTimer : IDisposable
    {
        // Some magic to make up for the lack of Interlock.Exchange on object types in .NET MF
        private int _syncToken;
        private int _timeout;
        private Timer _timer;

        public event NetworkTimeoutTimerHandler Timeout;

        /// <summary>
        /// Timer used for network timeouts and keep alive times.
        /// </summary>
        /// <param name="timeInSeconds">Timeout in seconds. A value of 0 disables the timer.</param>
        public TimeoutTimer(int timeInSeconds)
        {
            _syncToken = 0;
            _timeout = timeInSeconds;
        }

        public object TimeOutData { get; set; }

        /// <summary>
        /// Starts the timer with the timeout time given when the timer was created.
        /// </summary>
        public virtual void Start()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            if (_timeout > 0)
            {
                int timeSpan = _timeout * 1000;
                _timer = new Timer(TimerCallback, null, timeSpan, timeSpan);
                _syncToken = 1;
            }
        }

        public void Reset()
        {
            Reset(_timeout);
        }

        /// <summary>
        /// Reset the timeout on the timer.
        /// </summary>
        /// <param name="timeInSeconds">Timeout in seconds. A value of 0 disables the timer.</param>
        public virtual void Reset(int timeInSeconds)
        {
            _timeout = timeInSeconds;
            if (_timer != null)
            {
                if (_timeout > 0)
                {
                    // Reset existing timer
                    int timeSpan = _timeout*1000;
                    _timer.Change(timeSpan, timeSpan);
                }
                else
                {
                    // Disable the timer
                    _timer.Dispose();
                    _timer = null;
                }
            }
            else
            {
                // Create a new timer
                Start();
            }
        }

        /// <summary>
        /// Stops the timer, no event will be fired.
        /// </summary>
        public void Stop()
        {
            Dispose();
        }

        private void TimerCallback(object state)
        {
            if (Timeout != null)
            {
                Timeout(this, TimeOutData);
            }
        }

        public void Dispose()
        {
            // Thread safe disposing of timer. Only helpful if multiple threads attempt dispose.
            // Not protected against other method calls.
            int token = Interlocked.Exchange(ref _syncToken, 0);
            if (token != 0 && _timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }

    internal class ThreadSafeTimeoutTimer : TimeoutTimer
    {
        private readonly object _syncLock = new object();

        public ThreadSafeTimeoutTimer(int timeInSeconds)
            : base(timeInSeconds)
        {
        }

        public override void Start()
        {
            lock (_syncLock)
            {
                base.Start();
            }
        }

        public override void Reset(int timeInSeconds)
        {
            lock (_syncLock)
            {
                base.Reset(timeInSeconds);
            }
        }
    }
}
