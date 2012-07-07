using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemotePotatoServer
{
    /// <summary>
    /// Represents a period between two set dates and times
    /// </summary>
    class TimeWindow
    {
        // Private Members
        private DateTime _startTime;
        private DateTime _stopTime;

        // Constructors
        /// <summary>
        /// Creates a new TimeWindow with blank start and stop times
        /// </summary>
        public TimeWindow()
        {
            _startTime = new DateTime();
            _stopTime = new DateTime();
        }
        /// <summary>
        /// Create a new TimeWindow using specified start and stop times
        /// </summary>
        /// <param name="theStartTime">The start time, specified in Utc</param>
        /// <param name="theStopTime">The stop time, specified in Utc</param>
        public TimeWindow(DateTime theStartTime, DateTime theStopTime)
        {
            _startTime = theStartTime;
            _stopTime = theStopTime;
        }

        // Methods
        public bool PartiallyContainsTimeWindow(TimeWindow tw)
        {
            return (
                ((tw.StartTime >= _startTime) & (tw.StartTime < _stopTime)) |
                ((tw.StopTime > _startTime) & (tw.StopTime <= _stopTime)) |
                ((tw.StartTime < _startTime) & (tw.StopTime > _stopTime))
                );            
        }
        public bool ContainsTimeWindow(TimeWindow tw)
        {
            return (
                ((tw.StartTime >= _startTime) & (tw.StartTime < _stopTime)) |
                ((tw.StopTime > _startTime) & (tw.StopTime <= _stopTime)) 
                );
        }

        // Properties
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }
        public DateTime StopTime
        {
            get { return _stopTime; }
            set { _stopTime = value; }
        }
    }
}
