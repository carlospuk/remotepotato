using System;
using System.Net;
using System.Windows;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SilverPotato
{
    public class LogoCacheRetriever
    {
        public LogoCacheRetriever()
        {
        }

        #region Webclient Methods
        public event EventHandler<GenericEventArgs<MemoryStream>> GetBitmap_Completed;
        public Uri gettingUri;
        /// <summary>
        /// Retrieve a bitmap image from either the web, or local cache - and cache if it wasn't cached already.
        /// </summary>
        /// <param name="theUri"></param>
        public void GetBitmapFromSomewhere( Uri theUri)
        {
            gettingUri = theUri;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Settings.SilverlightCacheLogosAndThumbs)
            {
                // Is it cached?
                if (LogoCache.isCached(gettingUri))
                {
                    MemoryStream ms = LogoCache.getFromCache(gettingUri);

                    if (GetBitmap_Completed != null)
                        GetBitmap_Completed(this, new GenericEventArgs<MemoryStream>(ms));

                    return;
                }
            }
            
            // Not in cache  (or settings says don't use a cache)
            GetBitmapFromWebThenCache(gettingUri);
        }

        public void GetBitmapFromWebThenCache(Uri theUri)
        {
            WebClient wc = new WebClient();
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            wc.OpenReadAsync(theUri);
        }
        void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (gettingUri == null) return;

            // Is stream OK?
            if ((e.Error != null) || (e.Cancelled))
            {
                if (GetBitmap_Completed != null)
                    GetBitmap_Completed(this, new GenericEventArgs<MemoryStream>(null));
                return;
            }

            try
            {
                // Get bytes from the stream
                MemoryStream ms = new MemoryStream();
                ms.SetLength(e.Result.Length);
                e.Result.Read(ms.GetBuffer(), 0, (int)e.Result.Length);
                ms.Flush();  // ??
                e.Result.Close();
                e.Result.Dispose();

                // Save memorystream to cache
                if (Settings.DebugLogos) Functions.WriteLineToLogFile("Storing bitmap in cache for " + gettingUri.ToString());
                LogoCache.storeInCache(gettingUri, ms);

                // and use as source to image
                if (GetBitmap_Completed != null)
                    GetBitmap_Completed(this, new GenericEventArgs<MemoryStream>(ms));

                // Success
                gettingUri = null;
                return;
            }
            catch
            { }

            // Failed
            if (GetBitmap_Completed != null)
                GetBitmap_Completed(this, new GenericEventArgs<MemoryStream>(null));
        }
        #endregion

    }
}
