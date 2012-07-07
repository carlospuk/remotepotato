using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class EPGDayJumpButton : UserControl
    {
        public DateTime StoredDate;
        bool IsHighlighted;

        public EPGDayJumpButton()
        {
            InitializeComponent();
        }
        public EPGDayJumpButton(DateTime localDate)
            : this()
        {
            StoredDate = localDate;
            Populate();

            IsHighlighted = false;
            ColourBackground();
        }
        public void SetHighlighted()
        {
            IsHighlighted = true;
            ColourBackground();
        }
        public void ClearHighlighted()
        {
            IsHighlighted = false;
            ColourBackground();
        }

        // Events
        public event EventHandler<EPGDayJumpButtonEventArgs> Click;

        // Methods
        void Populate()
        {
            string dayLetters1 = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(StoredDate.DayOfWeek).Substring(0, 2);
            string displayText = dayLetters1.Substring(0, 1).ToUpper() + dayLetters1.Substring(1, 1).ToLower();

            bool isToday = (StoredDate.Date.Equals(DateTime.Now.Date));

            lblDayName.Text = isToday  ?
                "Today" :
                displayText;

            if (isToday) brdMain.Width = 48;

            lblDateNumber.Text = StoredDate.Day.ToString();
        }
        void ColourBackground()
        {
            if (IsHighlighted)
            {
                brdMain.Background = new SolidColorBrush(Functions.HexColor("#88DDDD"));
                brdMain.Cursor = Cursors.Arrow;
            }
            else
            {
                brdMain.Background = new SolidColorBrush(Functions.HexColor("#EEEEEE"));
                brdMain.Cursor = Cursors.Hand;
            }
        }




        private void brdAnyJumpLink_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsHighlighted)
                brdMain.Background = new SolidColorBrush(Colors.Yellow);
        }
        private void brdAnyJumpLink_MouseLeave(object sender, MouseEventArgs e)
        {
            ColourBackground();
        }
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsHighlighted) return;

            if (Click != null) Click(this, new EPGDayJumpButtonEventArgs(StoredDate));
        }



    }

    public class EPGDayJumpButtonEventArgs : EventArgs
    {
        public readonly DateTime SelectedDate;

        public EPGDayJumpButtonEventArgs(DateTime selectedDate)
        {
            SelectedDate = selectedDate;
        }
    }
}
