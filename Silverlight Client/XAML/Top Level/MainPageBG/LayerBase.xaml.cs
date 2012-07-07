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
    public partial class LayerBase : UserControl
    {
        public double MovementAmount;  // the amount the layer will move around.  from 0 - 600 ish

        public LayerBase()
        {
            MovementAmount = 0;  // initially wont move.

            InitializeComponent();
        }
    }
}
