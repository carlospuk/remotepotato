using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Timers;
using CommonEPG;

namespace RemotePotatoServer
{
    class RecordingQueue
    {
        static int purgeTimeMinutes = 60;  // purge items more than X minutes old
        static int idCounter;
        static Dictionary<int, RecordingRequest> RQueue;
        static Timer purgeTimer;

        static RecordingQueue()
        {
            RQueue = new Dictionary<int, RecordingRequest>();
            purgeTimer = new Timer(10000);
            purgeTimer.Elapsed += new ElapsedEventHandler(purgeTimer_Elapsed);
            purgeTimer.Start();
        }

        static void purgeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PurgeQueue();
        }

        public static string AddToQueue(RecordingRequest rr)
        {
            int newID = idCounter++;

            RQueue.Add(newID, rr);

            return newID.ToString();
        }

        public static bool ExtractFromQueue(int id, ref RecordingRequest rr)
        {
            if (!RQueue.ContainsKey(id))
                return false;

            rr = RQueue[id];
            return true;
        }


        public static void PurgeQueue()  // purge old recording requests (more than an hour old)
        {
            //for (int i =0; i < RQueue.Count; i++)
            List<int> purgeIDs = new List<int>();
            foreach (KeyValuePair<int, RecordingRequest> kvp in RQueue)
            {
                TimeSpan elapsed = (DateTime.Now - kvp.Value.RequestCreationDate);
                if (Math.Abs(elapsed.TotalMinutes) > purgeTimeMinutes)
                {
                    purgeIDs.Add(kvp.Key);   
                }
            }

            // Anything to purge?
            if (purgeIDs.Count > 0)
            {
                foreach (int i in purgeIDs)
                {
                    RQueue.Remove(i);
                }
            }
        }
    }
}
