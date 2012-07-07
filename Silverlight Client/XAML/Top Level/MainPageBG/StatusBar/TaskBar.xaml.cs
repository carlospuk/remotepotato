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
    public partial class TaskBar : UserControl
    {
        Dictionary<FrameworkElement, RunningTask> RunningTasks;


        public TaskBar()
        {
            InitializeComponent();

            RunningTasks = new Dictionary<FrameworkElement, RunningTask>();   
        }


        public void AddRunningTask(FrameworkElement _window)
        {
            TaskIcon theIcon = new TaskIcon();
            RunningTask newTask = new RunningTask(theIcon, _window);

            // Add task to local store
            RunningTasks.Add(_window, newTask);
            // Add icon to GUI
            newTask.Icon.Opacity = 0.0;
            spRunningTasks.Children.Add(newTask.Icon);
            Animations.DoFadeIn(0.2, newTask.Icon);
        }

 
        public void RemoveRunningTask(FrameworkElement _window)
        {
            if (!RunningTasks.ContainsKey(_window)) return;
            RunningTask task = RunningTasks[_window];

            // Remove icon from GUI
            spRunningTasks.Children.Remove(task.Icon);

            // Remove task from local store
            RunningTasks.Remove(_window);
        }

    }
}
