using System;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public static class FirewallHelper
    {

        enum TransportProtocols
        {
            UDP, 
            TCP
        }

        enum RuleDirections
        {
            Inbound,
            Outbound
        }

        public static void AskThenAddFirewallRules()
        {
            if (RPMessageBox.ShowQuestion("Do you use Windows Firewall?  If so, we can add rules to your firewall to allow Remote Potato to be accessed over the Internet.  You will only ever need to do this once, unless you change port numbers.\r\n\r\nDo you want to do this now?", "Add Firewall Rules?") == DialogResult.No)
                return;

            if (AddFirewallRules())
            {
                RPMessageBox.Show("The rules were successfully added to Windows Firewall.\r\n\r\nRemember, you do not need to do this again unless you change port numbers.");
            }
            else
            {
                RPMessageBox.ShowAlert("There was an error adding the rules to Windows Firewall - see debug log for more information.\r\n\r\nClick the 'Re-add firewall rules' button to try this again.");
            }
        }
        public static bool AddFirewallRules()
        {
            bool result = true;

            result &= AddFirewallRule(Convert.ToInt32( Settings.Default.Port ), RuleDirections.Inbound, TransportProtocols.TCP);

            int basePort = Convert.ToInt32(Settings.Default.SilverlightStreamingPort);
            int highestPort = basePort + Settings.Default.SilverlightStreamingNumberOfPorts - 1;
            for (int port = basePort; port <= highestPort; port++)
            {
                result &= AddFirewallRule(port, RuleDirections.Inbound, TransportProtocols.TCP);
                if (Functions.OSSupportsAdvancedFirewallInNetSH) // Don't bother adding a second rule to XP, one rule supports both Inbound and Outbound
                   result &= AddFirewallRule(port, RuleDirections.Outbound, TransportProtocols.TCP);
            }
            return result;
        }
        static string NetShArgumentsForRule(int port, RuleDirections direction, TransportProtocols protocol)
        {
            string strArguments = "";

            if (Functions.OSSupportsAdvancedFirewallInNetSH)
            {
                strArguments += "advfirewall firewall add rule name=" + "\"" + "Remote Potato - " + port.ToString() + " " + direction.ToString() + "\"";
                strArguments += " dir=" + ((direction == RuleDirections.Inbound) ? "in" : "out");
                strArguments += " action=allow";
                strArguments += " protocol=" + ((protocol == TransportProtocols.TCP) ? "TCP" : "UDP");
                strArguments += " localport=" + port.ToString();
            }
            else
            {
                // XP old syntax
                strArguments += "firewall add portopening name=" + "\"" + "Remote Potato - " + port.ToString() + " " + direction.ToString() + "\"";
                strArguments += " protocol=" + ((protocol == TransportProtocols.TCP) ? "TCP" : "UDP");
                strArguments += " port=" + port.ToString();
            }

            return strArguments;
        }
        static bool AddFirewallRule(int port, RuleDirections direction, TransportProtocols protocol)
        {
            System.Diagnostics.Process process = null;
            System.Diagnostics.ProcessStartInfo processStartInfo;

            processStartInfo = new System.Diagnostics.ProcessStartInfo();
            processStartInfo.FileName = "netsh.exe";
            processStartInfo.Arguments = NetShArgumentsForRule(port, direction, protocol);
            if (Functions.OSSupportsAdvancedFirewallInNetSH)
                processStartInfo.Verb = "runas";
            processStartInfo.UseShellExecute = true;
            //processStartInfo.UseShellExecute = false;
            //processStartInfo.CreateNoWindow = true;
            //processStartInfo.RedirectStandardOutput = true;
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            try
            {
                process = System.Diagnostics.Process.Start(processStartInfo);

                DateTime begunProcess = DateTime.Now;
                bool processTimeout = false;
                while (!process.HasExited)
                {
                    System.Threading.Thread.Sleep(200);
                    TimeSpan elapsed = (DateTime.Now - begunProcess);
                    if (elapsed.TotalSeconds > 25)
                    {
                        processTimeout = true;
                        break;
                    }
                }

                if (processTimeout)
                {
                    Functions.WriteLineToLogFile("FirewallHelper: NOT OK - TimeOut");
                    return false;
                }
                else if (process.ExitCode != 0)
                {
                    string processOutput = process.StandardOutput.ReadToEnd();

                    Functions.WriteLineToLogFile("FirewallHelper: NOT OK (error code " + process.ExitCode.ToString() );
                    Functions.WriteLineToLogFile(processOutput);
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("FirewallHelper: Exception trying to add a firewall rule using netsh.");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Dispose();
                }
            }

            Functions.WriteLineToLogFile("FirewallHelper: Rule Added 0 OK");
            return true;
        }


    }
}
