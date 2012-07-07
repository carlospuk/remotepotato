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
    public static class Animations
    {
        public static void DoCrossFade(double durationInSeconds, DependencyObject fadingObject, DependencyObject showingObject)
        {
            //DoAnimation(durationInSeconds, fadingObject, "(Opacity)", 1, 0.4
        }
        public static void DoGrowToShrink(double durationInSeconds, DependencyObject animTarget, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(ScaleX)", "(ScaleY)", null, 0.0, null, null, 0.0, null, false, completedEvent);
        }
        public static void DoFadeIn(double durationInSeconds, DependencyObject animTarget)
        {
            DoFadeIn(durationInSeconds, animTarget, null);
        }
        public static void DoFadeIn(double durationInSeconds, DependencyObject animTarget, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(Opacity)", null, 1, null, false, completedEvent);
        }
        public static void DoFadeOut(double durationInSeconds, DependencyObject animTarget)
        {
            DoAnimation(durationInSeconds, animTarget, "(Opacity)", null, 0, null, false, null);
        }
        public static void DoFadeOut(double durationInSeconds, DependencyObject animTarget, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(Opacity)", null, 0, null, false, completedEvent);
        }
        public static void DoFadeTo(double durationInSeconds, DependencyObject animTarget, double opacityToValue)
        {
            DoAnimation(durationInSeconds, animTarget, "(Opacity)", null, opacityToValue, null, false, null);
        }
        public static void DoFadeTo(double durationInSeconds, DependencyObject animTarget, double opacityToValue, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(Opacity)", null, opacityToValue, null, false, completedEvent);
        }
        public static void DoChangeWidthTo(double durationInSeconds, DependencyObject animTarget, double newWidth, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(Width)", null, newWidth, null, false, completedEvent);
        }
        public static void DoChangeHeightTo(double durationInSeconds, DependencyObject animTarget, double newWidth, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, "(Height)", null, newWidth, null, false, completedEvent);
        }
        public static void DoAnimation(double durationInSeconds, DependencyObject animTarget, string animTargetProperty, Nullable<double> fromValue, Nullable<double> toValue, Nullable<double> byValue, bool repeatForever, IEasingFunction easingFunction, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, animTargetProperty, null, fromValue, toValue, byValue, null, null, null, repeatForever, easingFunction, completedEvent);
        }
        public static void DoAnimation(double durationInSeconds, DependencyObject animTarget, string animTargetProperty, Nullable<double> fromValue, Nullable<double> toValue, Nullable<double> byValue, bool repeatForever, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, animTargetProperty, null, fromValue, toValue, byValue, null, null, null, repeatForever, completedEvent);
        }
        public static void DoAnimation(double durationInSeconds, DependencyObject animTarget, string animTargetProperty1, string animTargetProperty2, Nullable<double> fromValue1, Nullable<double> toValue1, Nullable<double> byValue1, Nullable<double> fromValue2, Nullable<double> toValue2, Nullable<double> byValue2, bool repeatForever, EventHandler completedEvent)
        {
            DoAnimation(durationInSeconds, animTarget, animTargetProperty1, animTargetProperty2, fromValue1, toValue1, byValue1, fromValue2, toValue2, byValue2, repeatForever, null, completedEvent);
        }
        public delegate void dDoAnimation(double durationInSeconds, DependencyObject animTarget, string animTargetProperty1, string animTargetProperty2, Nullable<double> fromValue1, Nullable<double> toValue1, Nullable<double> byValue1, Nullable<double> fromValue2, Nullable<double> toValue2, Nullable<double> byValue2, bool repeatForever, IEasingFunction easingFunction, EventHandler completedEvent);
        public static void DoAnimation(double durationInSeconds, DependencyObject animTarget, string animTargetProperty1, string animTargetProperty2, Nullable<double> fromValue1, Nullable<double> toValue1, Nullable<double> byValue1, Nullable<double> fromValue2, Nullable<double> toValue2, Nullable<double> byValue2, bool repeatForever, IEasingFunction easingFunction, EventHandler completedEvent)
        {
            dDoAnimation d = new dDoAnimation(DoAnimation2);
            animTarget.Dispatcher.BeginInvoke(d, durationInSeconds, animTarget, animTargetProperty1, animTargetProperty2, fromValue1, toValue1, byValue1, fromValue2, toValue2, byValue2, repeatForever, easingFunction, completedEvent);
        }
        public static void DoAnimation2(double durationInSeconds, DependencyObject animTarget, string animTargetProperty1, string animTargetProperty2, Nullable<double> fromValue1, Nullable<double> toValue1, Nullable<double> byValue1, Nullable<double> fromValue2, Nullable<double> toValue2, Nullable<double> byValue2, bool repeatForever, IEasingFunction easingFunction, EventHandler completedEvent)
        {
            if (animTarget == null) return;

            Duration duration = new Duration(TimeSpan.FromSeconds(durationInSeconds));
            Storyboard sb = new Storyboard();
            sb.Duration = duration;


            DoubleAnimation dX = new DoubleAnimation();
            
            // Easing
            if (easingFunction != null)
                dX.EasingFunction = easingFunction;
            
            dX.Duration = duration;
            sb.Children.Add(dX);
            Storyboard.SetTarget(dX, animTarget);
            Storyboard.SetTargetProperty(dX, new PropertyPath(animTargetProperty1));
            if (toValue1.HasValue)
                dX.To = toValue1.Value;
            if (fromValue1.HasValue)
                dX.From = fromValue1.Value;
            if (byValue1.HasValue)
                dX.By = byValue1.Value;

            if (animTargetProperty2 != null)
            {
                DoubleAnimation d2 = new DoubleAnimation();

                // Easing
                if (easingFunction != null)
                    d2.EasingFunction = easingFunction;

                d2.Duration = duration;
                sb.Children.Add(d2);
                Storyboard.SetTarget(d2, animTarget);
                Storyboard.SetTargetProperty(d2, new PropertyPath(animTargetProperty2));
                if (toValue2.HasValue)
                    d2.To = toValue2.Value;
                if (fromValue2.HasValue)
                    d2.From = fromValue2.Value;
                if (byValue2.HasValue)
                    d2.By = byValue2.Value;
            }

            if (repeatForever)
                sb.RepeatBehavior = RepeatBehavior.Forever;
            if (completedEvent != null)
                sb.Completed += completedEvent;
            sb.Begin();
        }
        public static void DoAnimationEaseOut(double durationInSeconds, DependencyObject animTarget, string animTargetProperty1, string animTargetProperty2, Nullable<double> fromValue1, Nullable<double> toValue1, Nullable<double> byValue1, Nullable<double> fromValue2, Nullable<double> toValue2, Nullable<double> byValue2, bool repeatForever, EventHandler completedEvent)
        {
            if (animTarget == null) return;

            Duration duration = new Duration(TimeSpan.FromSeconds(durationInSeconds));
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            DoubleAnimation dX = new DoubleAnimation();
            

            dX.Duration = duration;
            sb.Children.Add(dX);
            Storyboard.SetTarget(dX, animTarget);
            Storyboard.SetTargetProperty(dX, new PropertyPath(animTargetProperty1));
            if (toValue1.HasValue)
                dX.To = toValue1.Value;
            if (fromValue1.HasValue)
                dX.From = fromValue1.Value;
            if (byValue1.HasValue)
                dX.By = byValue1.Value;

            if (animTargetProperty2 != null)
            {
                DoubleAnimation d2 = new DoubleAnimation();
                d2.Duration = duration;
                sb.Children.Add(d2);
                Storyboard.SetTarget(d2, animTarget);
                Storyboard.SetTargetProperty(d2, new PropertyPath(animTargetProperty2));
                if (toValue2.HasValue)
                    d2.To = toValue2.Value;
                if (fromValue2.HasValue)
                    d2.From = fromValue2.Value;
                if (byValue2.HasValue)
                    d2.By = byValue2.Value;
            }

            if (repeatForever)
                sb.RepeatBehavior = RepeatBehavior.Forever;
            if (completedEvent != null)
                sb.Completed += completedEvent;
            sb.Begin();
        }

    }
}
