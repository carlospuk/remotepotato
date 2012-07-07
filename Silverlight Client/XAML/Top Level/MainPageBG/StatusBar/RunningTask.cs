using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public class RunningTask
    {
        public TaskIcon Icon { get; set; }
        public FrameworkElement Window { get; set; }


        public RunningTask()
        { }
        public RunningTask(TaskIcon _icon, FrameworkElement _window)
        {
            Icon = _icon;
            Window = _window;

            WireUpIconEvents();
        }


        #region External Calls
        public void Maximise()
        {
            VisualManager.MaximiseScreenSpaceWindow(Window);
        }
        public void Minimise()
        {
            VisualManager.MinimiseScreenSpaceWindow(Window);
        }
        public void Close()
        {
            VisualManager.CloseScreenSpaceWindow(Window);
        }
        #endregion

        #region Icon Events - Clicked etc
        void WireUpIconEvents()
        {
            Icon.Clicked += new EventHandler(Icon_Clicked);
        }
        void Icon_Clicked(object sender, EventArgs e)
        {
            Maximise();
        }
        #endregion


    }
}
