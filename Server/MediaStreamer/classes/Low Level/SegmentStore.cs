using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Text;
using FatAttitude.Collections;

namespace FatAttitude.MediaStreamer.HLS
{

    internal class SegmentStore
    {
        private string workingFolderPath;

        public SegmentStore(string ID)
        {
            workingFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemotePotato");
            workingFolderPath = Path.Combine(workingFolderPath + "\\static\\mediastreams\\", ID);
            if (!Directory.Exists(workingFolderPath)) Directory.CreateDirectory(workingFolderPath);
        }

        #region Top-Level Public
        object syncLock = new object();
        List<int> segmentsWaiting = new List<int>();
        public bool TryGetSegmentByNumber(int SegNumber, ref Segment seg)
        {
            lock (syncLock)
            {

                bool stopWaiting = false;
                if (!DoesFileExistForSegmentNumber(SegNumber))
                {
                    segmentsWaiting.Add(SegNumber);

                    do
                    {
                        Monitor.Wait(syncLock);

                        stopWaiting = (! segmentsWaiting.Contains(SegNumber));
                    }
                    while (
                    (!DoesFileExistForSegmentNumber(SegNumber)) &&
                    (! stopWaiting)
                    );
                }

                if (stopWaiting)
                    return false;

                // It's arrived!  Remove segments waiting flag 
                segmentsWaiting.Remove(SegNumber);

                seg = _GetSegment(SegNumber);
                return true;
            }
        }
        public void CancelWaitingSegments()
        {
            lock (syncLock)
            {
                segmentsWaiting.Clear();
                Monitor.PulseAll(syncLock);
            }
        }
        public void StoreSegment(Segment s)
        {
            lock (syncLock)
            {
                _StoreSegment(s);

                Monitor.PulseAll(syncLock);
            }
        }
        public bool HasSegment(int SegNumber)
        {
            lock (syncLock)
            {
                return DoesFileExistForSegmentNumber(SegNumber);
            }
        }
        public void DeleteAllStoredSegmentsFromDisk()
        {
            lock (syncLock)
            {
                if (workingFolderPath == null) return;

                Directory.Delete(workingFolderPath, true);
            }
        }
        #endregion

        #region Disk Store / Retrieve - Not Thread Safe
        Segment _GetSegment(int segNumber)
        {
            Segment s = new Segment();
            s.Number = segNumber;

            string FN = FileNameForSegmentNumber(s.Number);

            s.Data = FileToByteArray(FN);

            return s;
        }
        /// <summary>
        /// Function to get byte array from a file
        /// </summary>
        /// <param name="_FileName">File name to get byte array</param>
        /// <returns>Byte Array</returns>
        byte[] FileToByteArray(string _FileName)
        {
            byte[] bytes = null;

            FileStream fs = null;
            BinaryReader br = null;

            try
            {
                // Open file for reading
                fs = new FileStream(_FileName, FileMode.Open, FileAccess.Read);

                // attach filestream to binary reader
                br = new BinaryReader(fs);

                // get total byte length of the file
                long _TotalBytes = new System.IO.FileInfo(_FileName).Length;

                // read entire file into buffer
                bytes = br.ReadBytes((Int32)_TotalBytes);
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught: {0}", _Exception.Message);
            }
            finally
            {
                try
                {
                    if (fs != null) fs.Close();
                    if (fs != null) fs.Dispose();
                    if (br != null) br.Close();
                }
                catch { }
            }

            return bytes;
        }
        void _StoreSegment(Segment s)
        {
            try
            {
                string FN = FileNameForSegmentNumber(s.Number);
                FileStream fs = File.Create(FN, 1000);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(s.Data);
                bw.Flush();
                bw.Close();
            }
            catch { } // e.g. directory structure now erased, cannot store
        }
        bool DoesFileExistForSegmentNumber(int n)
        {
            return (File.Exists(FileNameForSegmentNumber(n)));
        }
        string FileNameForSegmentNumber(int n)
        {
            return Path.Combine(workingFolderPath, "segment-" + n.ToString() + ".ts");
        }
        #endregion


    }

    internal class SegmentStoredEventArgs : EventArgs
    {
        public int SegmentNumber {get; set;}

        internal SegmentStoredEventArgs(int _Number)
        {
            SegmentNumber = _Number;
        }
    }
    
}

