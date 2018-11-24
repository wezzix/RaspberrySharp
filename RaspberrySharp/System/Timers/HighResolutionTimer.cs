using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RaspberrySharp.System.Timers
{
    /// <summary>
    /// Represents a high-resolution timer.
    /// </summary>
    public class HighResolutionTimer : ITimer
    {
        #region Fields

        private TimeSpan delay;
        private TimeSpan interval;
        private Action action;

        private CancellationTokenSource tokenSource;
        private Thread thread;

        private static readonly int nanoSleepOffset = Calibrate();

        #endregion

        #region Instance Management

        /// <summary>
        /// Initializes a new instance of the <see cref="HighResolutionTimer"/> class.
        /// </summary>
        public HighResolutionTimer()
        {
            if (!Board.Current.IsRaspberryPi)
                throw new NotSupportedException("Cannot use HighResolutionTimer on a platform different than Raspberry Pi");
        }

        public static int NanoSleepOffset
        {
            get { return nanoSleepOffset; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        /// <value>
        /// The interval.
        /// </value>
        public TimeSpan Interval
        {
            get { return interval; }
            set
            {
                if (value.TotalMilliseconds > uint.MaxValue / 1000)
                    throw new ArgumentOutOfRangeException("value", interval, "Interval must be lower than or equal to uint.MaxValue / 1000");

                interval = value;
            }
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public Action Action
        {
            get { return action; }
            set
            {
                if (value == null)
                    Stop();

                action = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sleeps the specified delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public static void Sleep(TimeSpan delay)
        {
            // Based on [BCM2835 C library](http://www.open.com.au/mikem/bcm2835/)

            // Calling nanosleep() takes at least 100-200 us, so use it for
            // long waits and use a busy wait on the hires timer for the rest.
            var stopWatch = Stopwatch.StartNew();

            var millisecondDelay = delay.TotalMilliseconds;
            if (millisecondDelay == 0) return;

            if (millisecondDelay >= 100)
            {
                // Do not use high resolution timer for long interval (>= 100ms)
                Thread.Sleep(delay);
            }
            // Use nanosleep if interval is higher than 450µs
            else if (millisecondDelay > 0.450d)
            {
                var t1 = new Interop.Timespec();
                var t2 = new Interop.Timespec();
                
                t1.tv_sec = (IntPtr)0;
                t1.tv_nsec = (IntPtr)((long)(millisecondDelay * 1000000) - nanoSleepOffset);

                Interop.nanosleep(ref t1, ref t2);
            }
            else
            {
                while (true)
                {
                    if (stopWatch.Elapsed.TotalMilliseconds >= millisecondDelay)
                        break;
                }
            }
        }


        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <param name="startDelay">The delay before the first occurence, in milliseconds.</param>
        public void Start(TimeSpan startDelay)
        {
            if (startDelay.TotalMilliseconds > uint.MaxValue / 1000)
                throw new ArgumentOutOfRangeException("startDelay", startDelay, "Delay must be lower than or equal to uint.MaxValue / 1000");

            lock (this)
            {
                if (thread != null)
                    return;

                delay = startDelay;
                tokenSource = new CancellationTokenSource();
                thread = new Thread(() => ThreadProcess(tokenSource.Token));
                thread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                if (thread == null)
                    return;
                
                thread = null;
            }
        }

        #endregion

        #region Private Helpers

        private static int Calibrate()
        {
            const int referenceCount = 1000;
            return Enumerable.Range(0, referenceCount)
                .Aggregate(
                    (long)0,
                    (a, i) =>
                    {
                        var t1 = new Interop.Timespec();
                        var t2 = new Interop.Timespec();

                        t1.tv_sec = (IntPtr)0;
                        t1.tv_nsec = (IntPtr)1000000;

                        var stopWatch = Stopwatch.StartNew();
                        Interop.nanosleep(ref t1, ref t2);

                        return a + (long)(stopWatch.Elapsed.TotalMilliseconds * 1000000 - 1000000);
                    },
                    a => (int)(a / referenceCount));
        }

        private void ThreadProcess(CancellationToken token)
        {
            var thisThread = thread;

            Sleep(delay);
            while (thread == thisThread)
            {
                if (token.IsCancellationRequested)
                    return;

                (Action ?? NoOp)();
                Sleep(interval);
            }
        }

        private void NoOp() { }

        #endregion
    }
}
