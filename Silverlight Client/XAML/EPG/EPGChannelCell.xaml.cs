using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using CommonEPG;

namespace SilverPotato
{
    public partial class EPGChannelCell : UserControl
    {
        public TVService LinkedTVService;

        public EPGChannelCell()
        {
            InitializeComponent();

            double ChannelNumberColumnWidth = 40;
            LayoutRoot.Width = (SettingsImporter.SettingIsTrue("EPGShowChannelNumbers")) ? 125 : (125-ChannelNumberColumnWidth);
            cdChannelNumber.Width = (SettingsImporter.SettingIsTrue("EPGShowChannelNumbers")) ? new GridLength(ChannelNumberColumnWidth) : new GridLength(0);
        }
        public EPGChannelCell(TVService tvs)
            : this()
        {
            LinkedTVService = tvs;
            
            // Callsign
            lblCallsign.Text = tvs.Callsign;
            lblChannelNumber.Text = tvs.ChannelNumberString();

            if (SettingsImporter.SettingIsTrue("ShowChannelLogos"))
            {
                imgChannelLogo.Opacity = 0.0;  // Hide until it's loaded

                // From cache?
                LogoCacheRetriever retriever = new LogoCacheRetriever();
                retriever.GetBitmap_Completed += new EventHandler<GenericEventArgs<MemoryStream>>(retriever_GetBitmap_Completed);
                Uri logoUri = new Uri(NetworkManager.hostURL + "logo/" + System.Windows.Browser.HttpUtility.UrlEncode(tvs.UniqueId), UriKind.Absolute);
                retriever.GetBitmapFromSomewhere(logoUri);
            }
            else
            {
                imgChannelLogo.Visibility = Visibility.Collapsed;
                lblCallsign.Visibility = Visibility.Visible;
            }
        }

        
        delegate void dSetImageBitmap(MemoryStream ms);
        void retriever_GetBitmap_Completed(object sender, GenericEventArgs<MemoryStream> e)
        {
            if (e.Value == null)
            {
                Dispatcher.BeginInvoke(ImageFailed);
            }
            else
            {
                dSetImageBitmap d = new dSetImageBitmap(SetImageBitmap);       
                Dispatcher.BeginInvoke(d, e.Value);
            }
        }

        void SetImageBitmap(MemoryStream returnedStream)
        {
            if (returnedStream == null) return;

            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.SetSource(returnedStream);
            }
            catch (Exception ex)  // not a valid bitmap
            {
                if (Settings.DebugLogos)
                {
                    Functions.WriteLineToLogFile("Cannot create BMP from returned logo stream.");
                    Functions.WriteExceptionToLogFile(ex);
                }
                imgChannelLogo.Source = null;
                return;
            }

            imgChannelLogo.Source = bmp;
            returnedStream.Close();
            returnedStream.Dispose();

            ImageOpened();
        }



        void ImageFailed()
        {
            if (Settings.DebugLogos)
                Functions.WriteLineToLogFile("No logo for channel " + LinkedTVService.Callsign + " - reverting to callsign.");
            imgChannelLogo.Visibility = Visibility.Collapsed;
            lblCallsign.Visibility = Visibility.Visible;
            lblCallsign.Opacity = 0.0;
            Animations.DoFadeIn(0.5, lblCallsign);
        }

        private void ImageOpened()
        {
            imgChannelLogo.Opacity = 0.0;
            imgChannelLogo.Visibility = Visibility.Visible;
            Animations.DoFadeIn(0.5, imgChannelLogo);
            if (Settings.DebugLogos)
                Functions.WriteLineToLogFile("Logo opened OK for channel " + LinkedTVService.Callsign);

        }
    }
}
