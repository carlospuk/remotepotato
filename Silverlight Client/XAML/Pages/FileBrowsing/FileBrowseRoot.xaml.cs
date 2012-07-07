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
    public partial class FileBrowseRoot : UserControl
    {
        Stack<FileBrowseFolder> FoldersStack;

        protected bool IsInitialized = false;
        public FileBrowseRoot()
        {
            InitializeComponent();

            FoldersStack = new Stack<FileBrowseFolder>();
        }

        


#region Push / Pop Folders
        public void PushFolder(FileBrowseFolder picFolder)
        {
            // Add to stack
            FoldersStack.Push(picFolder);

            // Set off to the right
            picFolder.ttMover.X = 500;
            picFolder.Opacity = 0;

            gdContent.Children.Add(picFolder);

            Animations.DoFadeIn(0.1, picFolder);
            Animations.DoAnimation(0.2, picFolder.ttMover, "X", null, 0, null, false, null);
        }
        Queue<FileBrowseFolder> PoppingOffElements = new Queue<FileBrowseFolder>();
        public void PopFolder()
        {
            FileBrowseFolder childFolder = FoldersStack.Pop();

            Animations.DoAnimation(0.2, childFolder.ttMover, "X", null, 500, null, false, Move_Completed );
            Animations.DoFadeOut(0.18, childFolder);
            PoppingOffElements.Enqueue(childFolder);
        }
        void Move_Completed(object sender, EventArgs e)
        {
            FileBrowseFolder poppedOffFolder = PoppingOffElements.Dequeue();
            gdContent.Children.Remove(poppedOffFolder);
        }
#endregion




    }
}
