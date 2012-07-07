using System;
using System.Net;
using System.Reflection;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using CommonEPG;

namespace SilverPotato
{
    public static class Functions
    {
        static Functions()
        {
            
        }


        // Staic values
        public static Version MinimumServerVersionRequired = new Version(0, 92);
        public static int EPGZoomFactor = 5;
        public static string RPHostPassword = "";
        public static StringBuilder CurrentLog = new StringBuilder();

        public static bool RPHostRequiresPassword
        {
            get
            {
                return (!string.IsNullOrEmpty(RPHostPassword));
            }
        }
        public static string VersionString()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                AssemblyName assemblyName = new AssemblyName(assembly.FullName);
                return assemblyName.Version.Major.ToString() + "." + assemblyName.Version.Minor.ToString();

            }
            catch
            {
                return "?.?";
            }

        }


        // Logging
        public static void WriteLineToLogFile(string txt)
        {
            CurrentLog.Append(txt);
            CurrentLog.Append(Environment.NewLine);
        }
        public static void WriteExceptionToLogFile(Exception e)
        {
            if (e == null) return;

            string txtException = "EXCEPTION DETAILS: " + e.Message +  Environment.NewLine + e.StackTrace + Environment.NewLine;
            if (e.InnerException != null) txtException += Environment.NewLine + "(INNER: " + e.InnerException.Message + ")";
            WriteLineToLogFile(txtException);
        }

        // Helpers

        // COLORS
        public static Color HexColor(String hex)
        {
            //remove the # at the front
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;

            int start = 0;

            //handle ARGB strings (8 characters long)
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            //convert RGB characters to bytes
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return Color.FromArgb(a, r, g, b);
        }

        #region Base64
        public static string DecodeFromBase64(string encodedData)
        {
            return DecodeFromBase64(encodedData, Encoding.UTF8);

        }
        public static string DecodeFromBase64(string encodedData, Encoding _encoding)
        {
            string returnValue = "";
            try
            {
                byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
                returnValue = _encoding.GetString(encodedDataAsBytes, 0, encodedDataAsBytes.Length);
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Couldn't decode base64 string " + encodedData);
                Functions.WriteExceptionToLogFile(ex);
            }

            return returnValue;
        }
        public static string EncodeToBase64(string strToEncode)
        {
            return EncodeToBase64(strToEncode, Encoding.UTF8);
        }
        public static string EncodeToBase64(string strToEncode, Encoding _encoding)
        {
            string returnValue = "";
            try
            {
                byte[] encodedData = _encoding.GetBytes(strToEncode);
                returnValue = Convert.ToBase64String(encodedData);
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Couldn't encode base64 string " + strToEncode);
                Functions.WriteExceptionToLogFile(ex);
            }

            return returnValue;


        }

        #endregion

        // Xaml
        public static void ShowHideElement(FrameworkElement fe, bool show)
        {
            fe.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
        private static int HighestZ = 0;
        public static int ZOrderHighest
        {
            get
            {
                return ++HighestZ;
            }
        }
        public static void ChangeImageTo(Image img, string strNewImageResourcePath)
        {
            img.Source = ImageManager.LoadImageFromContentPath(strNewImageResourcePath);
        }

        // Text
        static char[] splitChars = new char[] { ' ', '-', '\t' };
        private static string WordWrap(string str, int width)
        {
            if (string.IsNullOrEmpty(str)) return "";

            string[] words = Explode(str, splitChars);

            int curLineLength = 0;
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < words.Length; i += 1)
            {
                string word = words[i];
                // If adding the new word to the current line would be too long,
                // then put it on a new line (and split it up if it's too long).
                if (curLineLength + word.Length > width)
                {
                    // Only move down to a new line if we have text on the current line.
                    // Avoids situation where wrapped whitespace causes emptylines in text.
                    if (curLineLength > 0)
                    {
                        strBuilder.Append(Environment.NewLine);
                        curLineLength = 0;
                    }

                    // If the current word is too long to fit on a line even on it's own then
                    // split the word up.
                    while (word.Length > width)
                    {
                        strBuilder.Append(word.Substring(0, width - 1) + "-");
                        word = word.Substring(width - 1);

                        strBuilder.Append(Environment.NewLine);
                    }

                    // Remove leading whitespace from the word so the new line starts flush to the left.
                    word = word.TrimStart();
                }
                strBuilder.Append(word);
                curLineLength += word.Length;
            }

            return strBuilder.ToString();
        }
        private static string[] Explode(string str, char[] splitChars)
        {
            if (str == null) return null;

            List<string> parts = new List<string>();
            int startIndex = 0;
            while (true)
            {
                int index = str.IndexOfAny(splitChars, startIndex);

                if (index == -1)
                {
                    parts.Add(str.Substring(startIndex));
                    return parts.ToArray();
                }

                string word = str.Substring(startIndex, index - startIndex);
                char nextChar = str.Substring(index, 1)[0];
                // Dashes and the likes should stick to the word occuring before it. Whitespace doesn't have to.
                if (char.IsWhiteSpace(nextChar))
                {
                    parts.Add(word);
                    parts.Add(nextChar.ToString());
                }
                else
                {
                    parts.Add(word + nextChar);
                }

                startIndex = index + 1;
            }
        }
        public static void ProcessAlphaForNumbers(ref string Alpha)
        {
            if ((Alpha.Equals("0")) || (Alpha.Equals("1")) || (Alpha.Equals("2")) || (Alpha.Equals("3")) || (Alpha.Equals("4")) ||
                (Alpha.Equals("5")) || (Alpha.Equals("6")) || (Alpha.Equals("7")) || (Alpha.Equals("8")) || (Alpha.Equals("9")))
            {
                Alpha = "#";
            }
        }

        // Files
        public static string finalPathComponentOfString(string strFullPath)
        {

            // Workaround for Mac paths
            strFullPath = strFullPath.Replace("\\\\", "#UNC#");
            strFullPath = strFullPath.Replace("/", "\\");
            strFullPath = strFullPath.Replace("#UNC#", "\\\\");

            try
            {
                string[] strResult = strFullPath.Split('\\');
                return strResult[strResult.Length - 1];
            }
            catch
            {
                return strFullPath;
            }

        }

        // Zoom / full screen
        public static void ToggleFullScreen()
        {
#if SILVERPOTATO
             Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
#endif
        }

        // Extension methods
        // TV Programme
        public static string ToPrettyLongDayNameAndDate(this DateTime dt)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(dt.ToLocalTime().DayOfWeek) + " " + dt.ToPrettyDate();
        }
        public static string ToPrettyDayNameAndDate(this DateTime dt)
        {
            if (dt.ToLocalTime().Date.Equals(DateTime.Now.Date)) return "Today";
            if (dt.ToLocalTime().Date.Equals(DateTime.Now.AddDays(1).Date)) return "Tomorrow";
            
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.ToLocalTime().DayOfWeek) + " " + dt.ToPrettyDate();
        }
        /// <summary>
        /// Assuming dt is either Utc or Local (but of known Utc-ness), converts to a local date and displays, e.g. as 4 Jan
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ToPrettyDate(this DateTime dt)
        {
            DateTime localDT = dt.ToLocalTime();

            return localDT.Day.ToString() + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(localDT.Month);
        }
        public static DateTime StartTimeDT(this TVProgramme tvp)
        {
            return new DateTime(tvp.StartTime, DateTimeKind.Utc);
        }
        public static DateTime StopTimeDT(this TVProgramme tvp)
        {
            return new DateTime(tvp.StopTime, DateTimeKind.Utc);
        }
        public static string ToPrettyStartStopLocalTimes(this TVProgramme tvp)
        {
            return tvp.StartTimeDT().ToLocalTime().ToShortTimeString() + " - " + tvp.StopTimeDT().ToLocalTime().ToShortTimeString();
        }
        public static string ToPrettyDate(this TVProgramme tvp)
        {
            return (tvp.StartTimeDT().ToPrettyDate());
        }
        public static string ToPrettyDayNameAndDate(this TVProgramme tvp)
        {
            return (tvp.StartTimeDT()).ToPrettyDayNameAndDate();
        }


        // Framework Elements
        public static Point TopLeft(this UIElement element, UIElement parent)
        {
            GeneralTransform gt = element.TransformToVisual(parent);
            return gt.Transform(new Point(0, 0));
        }
        public static bool IsInView(this FrameworkElement element, FrameworkElement parent)
        {
            var topLeft = element.TopLeft(parent);
            var elementRect = new Rect(topLeft.X, topLeft.Y, element.RenderSize.Width, element.RenderSize.Height);
            var parentRect = new Rect(0, 0, parent.RenderSize.Width, parent.RenderSize.Height);
            elementRect.Intersect(parentRect);
            return !elementRect.IsEmpty;
        }
        public static void RegisterForNotification(string property, FrameworkElement frameworkElement, PropertyChangedCallback OnCallBack)
        {
            Binding binding = new Binding(property)
            {
                Source = frameworkElement
            };

            var dependencyproperty = System.Windows.DependencyProperty.RegisterAttached("ListenAttached" + property,
                                     typeof(object), typeof(UserControl), new System.Windows.PropertyMetadata(OnCallBack));

            frameworkElement.SetBinding(dependencyproperty, binding);
        } 
      
        
    }


    // Event Args
    public class GenericEventArgs<T> : EventArgs
    {
        T value;
        public T Value { get { return value; } }
        public GenericEventArgs(T value) { this.value = value; }
    }
}
