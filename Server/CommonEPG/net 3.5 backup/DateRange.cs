using System;

namespace CommonEPG
{
    /// <summary>
    /// Represents a period between two set dates and times
    /// </summary>
    public class DateRange
    {
        // Private Members
        private DateTime _startTime;
        private DateTime _stopTime;

        // Constructors
        /// <summary>
        /// Creates a new DateRange with blank start and stop times
        /// </summary>
        public DateRange()
        {
            _startTime = new DateTime();
            _stopTime = new DateTime();

            // Ensure times are Utc
            _startTime = DateTime.SpecifyKind(_startTime, DateTimeKind.Utc);
            _stopTime = DateTime.SpecifyKind(_stopTime, DateTimeKind.Utc);
        }
        /// <summary>
        /// Create a new DateRange using specified start and stop times
        /// </summary>
        /// <param name="theStartTime">The start time, specified in Utc</param>
        /// <param name="theStopTime">The stop time, specified in Utc</param>
        public DateRange(DateTime theStartTime, DateTime theStopTime)
        {
            if (theStartTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Specified start time must be in Utc");

            if (theStopTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Specified stop time must be in Utc");

            _startTime = theStartTime;
            _stopTime = theStopTime;
        }

        // Methods
        public bool ContainsPartOfDateRange(DateRange dateRange)
        {
            return (
                (dateRange.StopTime > _startTime) & (dateRange.StartTime < _stopTime)
                );            
        }
        public bool ContainsAllOfDateRange(DateRange dateRange)
        {
            return (
                ((dateRange.StartTime >= _startTime) & (dateRange.StopTime <= _stopTime)) 
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
        public TimeSpan Span
        {
            get
            {
                return (_stopTime - _startTime);
            }
        }
    }
}
