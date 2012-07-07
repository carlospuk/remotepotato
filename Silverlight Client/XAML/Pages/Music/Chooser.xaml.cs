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

namespace SilverPotato
{
    public partial class Chooser : UserControl
    {
        Stack<ChooserStrip> Strips;


        public Chooser()
        {
            InitializeComponent();

            
            Strips = new Stack<ChooserStrip>();
            RemovingStrips = new Queue<ChooserStrip>();
        }


        #region Master Strip

        public void AddStrip(ChooserStrip cs)
        {
            // Is there a previous strip to contract first?
            if (Strips.Count > 0)
            {
                ChooserStrip csToContract = Strips.Peek();
                csToContract.ContractStrip();
            }

            // Push onto local stack
            Strips.Push(cs);

            // Add to UI
            cs.Opacity = 0.0;
            spMaster.Children.Add(cs);
            Animations.DoFadeIn(0.4, cs);

            // Events
            cs.StripBeginExpanding += new EventHandler(cs_StripExpanding);
        }

        void cs_StripExpanding(object sender, EventArgs e)
        {
            if (!(sender is ChooserStrip)) return;
            
            ChooserStrip cs = (ChooserStrip)sender;
            ClearBackToStrip(cs);
        }
        void ClearBackToStrip(ChooserStrip cs)
        {
            if (!Strips.Contains(cs)) return; // not in stack

            // Clear off until last visible strip == cs
            while (Strips.Peek() != cs)
            {
                RemoveStrip(Strips.Pop(), 0.2);   
            }
        }
        public void ClearAllStrips()
        {
            foreach (ChooserStrip cs in Strips)
            {
                RemoveStrip(cs, 0.1);
            }
        }
        Queue<ChooserStrip> RemovingStrips;
        void RemoveStrip(ChooserStrip cs, double animationDuration)
        {
            RemovingStrips.Enqueue(cs);
            Animations.DoFadeOut(animationDuration, cs, StripFadeOutCompleted);
        }
        void StripFadeOutCompleted(object sender, EventArgs e)
        {
            ChooserStrip cs = RemovingStrips.Dequeue();
            spMaster.Children.Remove(cs);
            cs = null;
        }
        #endregion

        
        // Size stackpanel to grid (limit vertical expansion - tough bug)
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (gdRoot == null) return;
            if (gdRoot.ActualHeight < 2) return;
            if (spMaster == null) return;

            spMaster.Height = gdRoot.ActualHeight;
        }



    }
}
