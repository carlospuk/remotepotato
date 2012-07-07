using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RemotePotatoServer
{

public class WizardPages : TabControl {


  

  protected override void WndProc(ref Message m) {
    // Hide tabs by trapping the TCM_ADJUSTRECT message
    if (m.Msg == 0x1328 && !DesignMode) m.Result = (IntPtr)1;
    else base.WndProc(ref m);
  }

}

    
}
