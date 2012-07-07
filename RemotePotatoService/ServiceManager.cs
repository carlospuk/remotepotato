using System;
using System.Threading;
using System.ServiceProcess;
using System.Management;

namespace RemotePotatoServer
{
    public static class ServiceManager
    {
        const string ServiceName="Remote Potato Service";

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
            return (ServiceStartupType("Remote Potato Service") == "Automatic");
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
                        ErrorText = "The Remote Potato Service could not be found - please try re-installing Remote Potato";
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
                        ErrorText = "The Remote Potato Service could not be found - please try re-installing Remote Potato";
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
                    return (string)service.GetPropertyValue("StartMode");
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


}
