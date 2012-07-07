using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public partial class EPGCell : UserControl, IDisposable
    {
        // Private / Public Members
        public bool LabelsShifted {get; set;}
        public EPGCellType CellType {get; set;}
        public TVProgramme LinkedTVProgramme { get; set; }

        // Events
        public event EventHandler<GenericEventArgs<TVProgramme>> Clicked;

        public EPGCell()
        {
            InitializeComponent();

            LabelsShifted = false;
            lblTitle.FontSize = (Settings.ShowTimesInEPG) ? 14 : 18;
            
        }
        public EPGCell(TVProgramme tvp) : this()
        {
            LinkedTVProgramme = tvp;
            LinkedTVProgramme.Updated += new EventHandler(LinkedTVProgramme_Updated);
            LayoutCellFromLinkedProgramme();
        }
        public void Dispose()
        {
            if (LinkedTVProgramme != null)
            {
                LinkedTVProgramme.Updated -= new EventHandler(LinkedTVProgramme_Updated);
                LinkedTVProgramme = null;
            }
        }

        void LinkedTVProgramme_Updated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(LayoutCellFromLinkedProgramme);
        }
        public EPGCell(EPGCellType cellType, double durationMins)
            : this()
        {
            CellType = cellType;

            if (cellType == EPGCellType.Filler)
            {
                brdMainBorder.Background = null;
                lblTimes.Text = "";
                lblTitle.Text = "";
            }

            SetDurationMinutes(durationMins);
        }


        public void LayoutCellFromLinkedProgramme()
        {
            TVProgramme tvp = LinkedTVProgramme;

            FadeCellIfProgrammeEnded();

            CellType = EPGCellType.Programme;
            lblTitle.Text = tvp.Title;

            lblTimes.Text = (Settings.ShowTimesInEPG) ?
                tvp.ToPrettyStartStopLocalTimes() :
                "";

            lblEpisodeTitle.Text = (Settings.ShowEpisodeTitlesInEPG) ?
                tvp.EpisodeTitle :
                "";

            SetDurationMinutes(Convert.ToDouble(tvp.DurationMinutes()));

            if (SettingsImporter.SettingIsTrue("ShowBackgroundColoursInEPG"))
            {
                switch (tvp.ProgramType)
                {
                    case TVProgrammeType.Documentary:
                        gsCellColour.Color = Functions.HexColor("#FFfa9028");
                        break;

                    case TVProgrammeType.Kids:
                        gsCellColour.Color = Functions.HexColor("#FF4cd0e0");
                        break;

                    case TVProgrammeType.Movie:
                        gsCellColour.Color = Functions.HexColor("#FFb84ce0");
                        break;

                    case TVProgrammeType.News:
                        gsCellColour.Color = Functions.HexColor("#FFc4ba1c");
                        break;

                    case TVProgrammeType.Sport:
                        gsCellColour.Color = Functions.HexColor("#FF0d9f11");
                        break;

                    default:
                        gsCellColour.Color = Functions.HexColor("#FF5A88E8");
                        break;
                }
            }
            else
                gsCellColour.Color = Functions.HexColor("#FF5A88E8");

            if (tvp.DurationMinutes() < 20)
                ToolTipService.SetToolTip(brdMainBorder, tvp.ToTooltipString());

            if (tvp.HasActiveRecording())
            {
                imgTop.Source = (tvp.isSeriesRecording()) ? ImageManager.bmpRecordDotSeries : ImageManager.bmpRecordDotOneTime;
                colRightIcons.Width = new GridLength(25.0);
            }
            else if (tvp.IsRecommended())
            {
                imgTop.Source = ImageManager.bmpStarOn;
                colRightIcons.Width = new GridLength(25.0);
            }
            else
            {
                imgTop.Source = null;
                colRightIcons.Width = new GridLength(0.0);
            }


            UpdateLayout();
        }
        public void FadeCellIfProgrammeEnded()
        {
            // OPacity - fade if in past
            if (LinkedTVProgramme.HasEndedYet()) brdMainBorder.Opacity = 0.2;

            // If on now - highlight
            gsCellFadeColour.Color = LinkedTVProgramme.IsCurrentlyShowing() ?
                Functions.HexColor("#FF666666") :
                Functions.HexColor("#FF222222");
        }

        public void SetDurationMinutes(double durationMinutes)
        {
            LayoutRoot.MaxWidth = durationMinutes * Functions.EPGZoomFactor;
            LayoutRoot.Width = durationMinutes * Functions.EPGZoomFactor;
            LayoutRoot.MinWidth = durationMinutes * Functions.EPGZoomFactor;
        }

        public void shiftLabelsToX(double X)
        {
            gdInnerContent.Margin = new Thickness(X + 10, 10, 10, 10);
            LabelsShifted = true;
        }
        public void resetLabels()
        {
            if (LabelsShifted)
            {
                gdInnerContent.Margin = new Thickness(10);
                LabelsShifted = false;
            }
        }

        Color replacedCellColor;
        private void brdMainBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CellType == EPGCellType.Filler) return;
            if (lblTitle.Text == "Channel Off Air") return;

            replacedCellColor = gsCellColour.Color;
            gsCellColour.Color = Colors.White;
            //brdMainBorder.BorderBrush = new SolidColorBrush(Colors.Yellow);
        }
        private void brdMainBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CellType == EPGCellType.Filler) return;
            if (lblTitle.Text == "Channel Off Air") return;

            gsCellColour.Color = replacedCellColor;
            //brdMainBorder.BorderBrush = new SolidColorBrush(Colors.Black);
        }
        private void brdMainBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CellType == EPGCellType.Filler) return;

            if (Clicked != null)
                Clicked(this, new GenericEventArgs<TVProgramme>(LinkedTVProgramme));
        }


    }

    public enum EPGCellType
    {
        Programme,
        Filler
    }
}
