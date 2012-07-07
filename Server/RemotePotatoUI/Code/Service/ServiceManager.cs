using System;
using System.Threading;
using System.ServiceProcess;
using System.Management;

namespace RemotePotatoServer
{
    public static class ServiceManager
    {
        const string ServiceName="Remote Potato Service";

        // Static Constructor
        static ServiceManager()
        {
            InitCheckServiceTimer();
        }


        #region Monitor Service Status
        static Timer tCheckServiceStatus;
        static RPServiceStatusTypes LastServiceStatus;
        static void InitCheckServiceTimer()
        {
            tCheckServiceStatus = new Timer(tCheckServiceStatus_Tick, null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
        }
        public static event EventHandler ServiceStatusChanged;
        static void tCheckServiceStatus_Tick(Object stateInfo)
        {
            RPServiceStatusTypes serviceStatus = RemotePotatoServiceStatus;
            

            if (serviceStatus != LastServiceStatus)
            {
                LastServiceStatus = serviceStatus;
                if (ServiceStatusChanged != null)
                    ServiceStatusChanged(new object(), new EventArgs());


                if (serviceStatus == RPServiceStatusTypes.NotInstalled)
                {
                    tCheckServiceStatus.Dispose();
                    return;
                }

            }
        }
        #endregion



        #region Start / Stop Service
                // Web Service
        public static RPServiceStatusTypes RemotePotatoServiceStatus
        {
            get
            {
                // Services
                ServiceController svcWinSearch = new ServiceController("Remote Potato Service");
                try
                {
                    return (svcWinSearch.Status == ServiceControllerStatus.Running) ?
                        RPServiceStatusTypes.Running :
                        RPServiceStatusTypes.Stopped;
                }
                catch (System.InvalidOperationException)
                {
                    return RPServiceStatusTypes.NotInstalled;
                }
                catch 
                {
                    return RPServiceStatusTypes.NotInstalled;
                }
                finally
                {
                    svcWinSearch.Dispose();
                    svcWinSearch = null;
                }
            }
        }
        public static bool StartRemotePotatoService()
        {
            return StartorStopRemotePotatoService(true);
        }
        public static bool StopRemotePotatoService()
        {
            return StartorStopRemotePotatoService(false);
        }
        static bool StartorStopRemotePotatoService(bool start)
        {
            // Services
            bool isRunning;
            ServiceController svcRP = null;
            try
            {
                svcRP = new ServiceController("Remote Potato Service");
                isRunning = (svcRP.Status == ServiceControllerStatus.Running);

                if (start)
                {
                    if (!isRunning)
                    {
                        svcRP.Start();
                        svcRP.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        return (svcRP.Status == ServiceControllerStatus.Running); // return false if didn't start
                    }
                }
                else
                {
                    if (isRunning)
                    {
                        svcRP.Stop();
                        svcRP.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        return (svcRP.Status == ServiceControllerStatus.Stopped); // return false if didn't stop
                    }
                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Service: Could not start or stop service:");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }
            finally
            {
                svcRP = null;
            }

            return false;
        }
        #endregion


        #region Change Service Parameters (logon, startup type etc)
        // For RP Service
        public static bool SetRPServiceStartupType(bool startWithWindows, ref string ErrorText)
        {
            return SetServiceStartupType(ServiceName, startWithWindows, ref ErrorText);
        }
        public static bool SetRPServiceLogon(string AccountName, string Password, bool PredefinedAccount, ref string ErrorText)
        {
            return SetServiceLogon(ServiceName, AccountName, Password, PredefinedAccount, ref ErrorText);
        }
        public static bool RPServiceStartsAutomatically()
        {
            return (ServiceStartupType("Remote Potato Service").StartsWith("Auto"));
        }
        public static string RPServiceAccountName
        {
            get
            {
                string UN = ServiceAccountName(ServiceName);
                if (UN.StartsWith(".\\"))
                    UN = UN.Substring(2, UN.Length - 2);

                return UN;
            }
        }
        
        // Generic Methods - for a named service
        static bool SetServiceLogon(string ServiceName, string AccountName, string Password, bool PredefinedAccount, ref string ErrorText)
        {
            string objPath = string.Format("Win32_Service.Name='{0}'", ServiceName);
            using (ManagementObject service = new ManagementObject(new ManagementPath(objPath)))
            {
                object[] wmiParams = new object[11];

                if (PredefinedAccount)
                {
                    wmiParams[6] = "LocalSystem";
                    wmiParams[7] = "";
                }
                else
                {
                    // Must be passed local account syntax - adjust if need be
                    if (!AccountName.Contains("\\"))
                    {
                        AccountName = ".\\" + AccountName;
                    }

                    wmiParams[6] = AccountName; // provided by user
                    wmiParams[7] = Password; // provided by user
                }

                object invokeResult = null;
                try
                {
                    invokeResult = service.InvokeMethod("Change", wmiParams);  //http://msdn.microsoft.com/en-us/library/aa384901
                }
                catch (ManagementException mex)
                {
                    if (mex.ErrorCode.ToString().Equals("NotFound"))
                    {
                        RPMessageBox.ShowAlert("The Remote Potato Service could not be found - please try re-installing Remote Potato");
                    }
                    return false;
                }
                catch
                {
                    return false;
                }

                // Return true if result code is 0
                int resultCode;
                if (!int.TryParse(invokeResult.ToString(), out resultCode))
                {
                    ErrorText = "Non-numerical result code from Change() method.";
                    return false;
                }

                ErrorText = ChangeServiceErrorMessage(resultCode);
                return (resultCode == 0);
            }
        }
        static bool SetServiceStartupType(string ServiceName, bool startWithWindows, ref string ErrorText)
        {
            string objPath = string.Format("Win32_Service.Name='{0}'", ServiceName);
            using (ManagementObject service = new ManagementObject(new ManagementPath(objPath)))
            {
                object[] wmiParams = new object[1];

                wmiParams[0] = startWithWindows ? "Automatic" : "Manual";

                object invokeResult = null;
                try
                {
                    invokeResult = service.InvokeMethod("ChangeStartMode", wmiParams);  //http://msdn.microsoft.com/en-us/library/aa384901
                }
                catch (ManagementException mex)
                {
                    if (mex.ErrorCode.ToString().Equals("NotFound"))
                    {
                        RPMessageBox.ShowAlert("The Remote Potato Service could not be found - please try re-installing Remote Potato");
                    }
                    return false;
                }
                catch
                {
                    return false;
                }

                // Return true if result code is 0
                int resultCode;
                if (!int.TryParse(invokeResult.ToString(), out resultCode))
                {
                    ErrorText = "Non-numerical result code from Change() method.";
                    return false;
                }

                ErrorText = ChangeServiceErrorMessage(resultCode);
                return (resultCode == 0);
            }
        }
        static string ServiceStartupType(string ServiceName)
        {
             string objPath = string.Format("Win32_Service.Name='{0}'", ServiceName);
             using (ManagementObject service = new ManagementObject(new ManagementPath(objPath)))
             {
                 try
                 {
                     string value = (string)service.GetPropertyValue("StartMode");
                     return value;  // Auto, Manual or Disabled  (note Auto not Automatic)
                 }
                 catch 
                 {
                     return string.Empty;
                 }
             }
        }
        static string ServiceAccountName(string ServiceName)
        {
            string objPath = string.Format("Win32_Service.Name='{0}'", ServiceName);
            using (ManagementObject service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    return (string)service.GetPropertyValue("StartName");
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
        static string ChangeServiceErrorMessage(int resultCode)
            {
                switch (resultCode)
                {

                    /*
                     * 0 Success
                    1 Not Supported
                    2 Access Denied
                    3 Dependent Services Running
                    4 Invalid Service Control
                    5 Service Cannot Accept Control
                    6 Service Not Active
                    7 Service Request Timeout
                    8 Unknown Failure
                    9 Path Not Found
                    10 Service Already Running
                    11 Service Database Locked
                    12 Service Dependency Deleted
                    13 Service Dependency Failure
                    14 Service Disabled
                    15 Service Logon Failure
                    16 Service Marked For Deletion
                    17 Service No Thread
                    18 Status Circular Dependency
                    19 Status Duplicate Name
                    20 Status Invalid Name
                    21 Status Invalid Parameter
                    22 Status Invalid Service Account
                    23 Status Service Exists
                    24 Service Already Paused
                     */

                    case 0:
                        return "OK";

                    case 15:
                        return "Logon failure.";

                    case 22:
                        return "Invalid user account.";

                    case 9:
                        return "Path not found.";

                    case 2:
                        return "Access denied.";

                    case 14:
                        return "Service disabled.";

                    default:
                        return "Unknown result (code" + resultCode.ToString() + ").";


                }
            }
        #endregion


    }




    public enum RPServiceStatusTypes
    {
        Running,
        Stopped,
        NotInstalled
    }

}
