using System;

namespace CommonEPG
{

    // Event arguments for status reports
    public class ProgressEventArgs : EventArgs
    {
        public string TaskName { get; set; }
        public int PercentageProgress { get; set; }

        public ProgressEventArgs(string taskName, int percentageProgress)
        {
            TaskName = taskName;
            PercentageProgress = percentageProgress;
        }
    }
    public class TaskIDEventArgs : EventArgs
    {
        public string TaskID { get; set; }
        public bool TaskSuccess { get; set; }

        public TaskIDEventArgs(string taskID, bool taskSuccess)
        {
            TaskID = taskID;
            TaskSuccess = taskSuccess;
        }
        public TaskIDEventArgs(string taskID)
        {
            TaskID = taskID;
            TaskSuccess = true;
        }
    }

    // Event Args
    public class GenericEventArgs<T> : EventArgs
    {
        T value;
        public T Value { get { return value; } }
        public GenericEventArgs(T value) { this.value = value; }
    }

}
