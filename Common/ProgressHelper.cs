// Licensed under the MIT License. See LICENSE in the repository root for license information.

using System;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// Helper for showing progress.
    /// </summary>
    public class ProgressHelper
    {
        /// <summary>
        /// Increment to show progress.
        /// </summary>
        private readonly int increment;

        /// <summary>
        /// Stops to show percentage.
        /// </summary>
        private readonly Dictionary<int, string> stops;

        /// <summary>
        /// Duration between writes.
        /// </summary>
        private readonly TimeSpan duration;

        /// <summary>
        /// The action to send updates to.
        /// </summary>
        private readonly Action<string> action;

        /// <summary>
        /// <c>true</c> when progress is based on time. Otherwise based on percentage.
        /// </summary>
        private readonly bool timeMode = false;

        /// <summary>
        /// Last time-based checkpoint.
        /// </summary>
        private DateTime lastCheckPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressHelper"/> class, configured
        /// for progress based on time.
        /// </summary>
        /// <param name="tick">The tick time.</param>
        /// <param name="action">The action to call with updates.</param>
        public ProgressHelper(TimeSpan tick, Action<string> action)
        {
            Extensions.CheckNotNull(tick, nameof(tick));
            Extensions.CheckNotNull(action, nameof(action));

            this.action = action;
            duration = tick;
            timeMode = true;
            lastCheckPoint = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressHelper"/> class, using
        /// a total count.
        /// </summary>
        /// <param name="total">The total items.</param>
        /// <param name="action">The action to call with updates.</param>
        public ProgressHelper(int total, Action<string> action)
        {
            Extensions.CheckNotNull(action, nameof(action));

            if (total < 1)
            {
                total = 1;
            }

            this.action = action;

            var step = total / 10.0;
            increment = (int)(total / 100.0);

            stops = new Dictionary<int, string>
            {
                { (int)Math.Floor(step), "10%" },
                { (int)Math.Floor(step * 2.0), "20%" },
                { (int)Math.Floor(step * 3.0), "30%" },
                { (int)Math.Floor(step * 4.0), "40%" },
                { (int)Math.Floor(step * 5.0), "50%" },
                { (int)Math.Floor(step * 6.0), "60%" },
                { (int)Math.Floor(step * 7.0), "70%" },
                { (int)Math.Floor(step * 8.0), "80%" },
                { (int)Math.Floor(step * 9.0), "90%" },
            };
        }

        /// <summary>
        /// Gets the current index.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Increment based on progress.
        /// </summary>
        public void Increment()
        {
            if (timeMode)
            {
                IncrementTime();
            }
            else
            {
                IncrementScalar();
            }
        }

        /// <summary>
        /// Increment based on time.
        /// </summary>
        private void IncrementTime()
        {
            var span = DateTime.Now - lastCheckPoint;
            if (span > duration)
            {
                action(".");
                lastCheckPoint = DateTime.Now;
            }
        }

        /// <summary>
        /// Increment the count.
        /// </summary>
        private void IncrementScalar()
        {
            Index++;
            if (stops.ContainsKey(Index))
            {
                action(stops[Index]);
            }
            else if (increment > 1 && (Index % increment) == 0)
            {
                action(".");
            }
        }
    }
}
