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
using RPKeySender;

namespace SilverPotato
{
    public partial class RemoteControlPane : UserControl
    {

        public RemoteControlPane()
        {
            InitializeComponent();

            RemoteControlManager.SendRemoteControlCommand_Completed += new EventHandler<SendRemoteControlCommandEventArgs>(RemoteControlManager_SendRemoteControlCommand_Completed);

        }

        
        MCCommands lastCommandSent = MCCommands.Stop;
        void ActivateButtonName(string btnName)
        {
            MCCommands thisCommand = MCCommands.Play;
            Type enumType = typeof(MCCommands);
            try
            {
                thisCommand = (MCCommands)Enum.Parse(enumType, btnName, true);
                lastCommandSent = thisCommand;

            dSetStatus d = new dSetStatus(SetStatus);
            this.Dispatcher.BeginInvoke(d, "Sending " + thisCommand.ToString() + "...", Colors.Gray);      

                RemoteControlManager.SendRemoteControlCommand(thisCommand);
            }
            catch 
            {
                MessageBox.Show("Unknown button encountered.");
            }
             
        }
        void RemoteControlManager_SendRemoteControlCommand_Completed(object sender, SendRemoteControlCommandEventArgs e)
        {
            dSetStatus d = new dSetStatus(SetStatus);
            Color statusColor = Functions.HexColor("#AAFF00"); // light green
            string statusMessage = "";

            if (e.Success)
            {
                statusMessage = "Sent " + lastCommandSent.ToString() + " OK";
            }
            else
            {
                string svrResultText = e.ResultText;

                if (e.ResultText == "HELPER_NOT_RUNNING")
                {
                  //  MessageBox.Show("The IR helper app is not running on the server; check that it is enabled by running the Remote Potato Settings application.");
                    svrResultText = "IR Helper not running.";
                }

                statusMessage = "Error sending " + lastCommandSent.ToString() + ": " + svrResultText;
                statusColor = Colors.Red;
            }


            // Set status
            this.Dispatcher.BeginInvoke(d, statusMessage, statusColor);            
        }

        delegate void dSetStatus(string newStatus, Color newColor);
        void SetStatus(string newStatus, Color newColor)
        {
            lblResult.Foreground = new SolidColorBrush(newColor);
            lblResult.Text = newStatus;
        }

        #region Control Events
        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)sender;
                
                PulseControl(fe);

                string buttonName = fe.Name;
                ActivateButtonName(buttonName);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)sender;
                fe.Opacity = 0.3;
            }
            
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)sender;
                fe.Opacity = 0.0;
                return;
            }
        }

        Queue<FrameworkElement> PulsingControls = new Queue<FrameworkElement>();
        void PulseControl(FrameworkElement fe)
        {
            ScaleTransform st = new ScaleTransform();
            st.CenterX = fe.Width / 2.0;
            st.CenterY = fe.Height / 2.0;
            st.ScaleX = 1.0;
            st.ScaleY = 1.0;

            fe.RenderTransform = st;

            // Make it big and green.
            Shape sh = (Shape)fe;
            sh.Fill = null;
            sh.Stroke = new SolidColorBrush(Colors.Green);
            sh.StrokeThickness = 4.0;
            sh.Opacity = 1.0;


            PulsingControls.Enqueue(fe);
            Animations.DoAnimation2(0.3, st, "ScaleX", "ScaleY", null, 1.8, null, null, 1.8, null, false, null, Pulse_Done);
            Animations.DoFadeOut(0.3, fe);
        }
        void Pulse_Done(object sender, EventArgs e)
        {
            FrameworkElement fe = PulsingControls.Dequeue();
            fe.RenderTransform = null;

            // Leave faded out
            Shape sh = (Shape)fe;
            sh.Fill = new SolidColorBrush(Functions.HexColor("#CCCCFF"));
            sh.Stroke = null;
        }
        #endregion
    }
}
