/*
Copyright (c) 2007 Austin Wise

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Security.Principal;
using System.Security.AccessControl;

using Austin.HttpApi.Internal;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Austin.HttpApi
{
    public class UrlReservation
    {
        private const int GENERIC_EXECUTE = 536870912;

        public UrlReservation(string url)
        {
            _url = url;
        }

        public UrlReservation(string url, IList<SecurityIdentifier> securityIdentifiers)
        {
            _url = url;
            _securityIdentifiers.AddRange(securityIdentifiers);
        }

        private string _url;
        public string Url
        {
            get { return _url; }
        }

        private List<SecurityIdentifier> _securityIdentifiers = new List<SecurityIdentifier>();
        public ReadOnlyCollection<string> Users
        {
            get
            {
                List<string> users = new List<string>();
                foreach (SecurityIdentifier sec in _securityIdentifiers)
                {
                    users.Add(((NTAccount)sec.Translate(typeof(NTAccount))).Value);
                }
                return new ReadOnlyCollection<string>(users);
            }
        }

        public void AddUser(string user)
        {
            NTAccount account = new NTAccount(user);
            SecurityIdentifier sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
            AddSecurityIdentifier(sid);
        }

        public void AddSecurityIdentifier(SecurityIdentifier sid)
        {
            _securityIdentifiers.Add(sid);
        }

        public void ClearUsers()
        {
            this._securityIdentifiers.Clear();
        }

        public void Create()
        {
            UrlReservation.Create(this);
        }

        public void Delete()
        {
            UrlReservation.Delete(this);
        }

        #region Get All
        public static ReadOnlyCollection<UrlReservation> GetAll()
        {
            List<UrlReservation> revs = new List<UrlReservation>();

            uint retVal = (uint)ErrorCodes.NOERROR; // NOERROR = 0

            retVal = NativeMethods.HttpInitialize(HttpApiConstants.HTTPAPI_VERSION_1, HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if ((uint)ErrorCodes.NOERROR == retVal)
            {
                HTTP_SERVICE_CONFIG_URLACL_QUERY inputConfigInfoSet = new HTTP_SERVICE_CONFIG_URLACL_QUERY();
                inputConfigInfoSet.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext;

                int i = 0;
                while (retVal == 0)
                {
                    inputConfigInfoSet.dwToken = (uint)i;


                    IntPtr pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_QUERY)));
                    Marshal.StructureToPtr(inputConfigInfoSet, pInputConfigInfo, false);

                    HTTP_SERVICE_CONFIG_URLACL_SET outputConfigInfo = new HTTP_SERVICE_CONFIG_URLACL_SET();
                    IntPtr pOutputConfigInfo = Marshal.AllocCoTaskMem(0);

                    int returnLength = 0;
                    retVal = NativeMethods.HttpQueryServiceConfiguration(IntPtr.Zero,
                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                        pInputConfigInfo,
                        Marshal.SizeOf(inputConfigInfoSet),
                        pOutputConfigInfo,
                        returnLength,
                        out returnLength,
                        IntPtr.Zero);

                    if (retVal == ErrorCodes.ERROR_INSUFFICIENT_BUFFER)
                    {
                        Marshal.ZeroFreeCoTaskMemUnicode(pOutputConfigInfo);
                        pOutputConfigInfo = Marshal.AllocCoTaskMem(Convert.ToInt32(returnLength));

                        retVal = NativeMethods.HttpQueryServiceConfiguration(IntPtr.Zero,
                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                        pInputConfigInfo,
                        Marshal.SizeOf(inputConfigInfoSet),
                        pOutputConfigInfo,
                        returnLength,
                        out returnLength,
                        IntPtr.Zero);
                    }

                    if (ErrorCodes.NOERROR == retVal)
                    {
                        outputConfigInfo = (HTTP_SERVICE_CONFIG_URLACL_SET)Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_URLACL_SET));
                        UrlReservation rev = new UrlReservation(outputConfigInfo.KeyDesc.pUrlPrefix, securityIdentifiersFromSDDL(outputConfigInfo.ParamDesc.pStringSecurityDescriptor));
                        revs.Add(rev);
                    }

                    Marshal.FreeCoTaskMem(pOutputConfigInfo);
                    Marshal.FreeCoTaskMem(pInputConfigInfo);

                    i++;
                }

                retVal = NativeMethods.HttpTerminate(HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

            if ((uint)ErrorCodes.NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }

            return new ReadOnlyCollection<UrlReservation>(revs);
        }
        #endregion

        #region Create
        public static void Create(UrlReservation urlReservation)
        {
            string sddl = generateSddl(urlReservation._securityIdentifiers);
            reserveURL(urlReservation.Url, sddl);
        }

        private static void reserveURL(string networkURL, string securityDescriptor)
        {
            uint retVal = (uint)ErrorCodes.NOERROR; // NOERROR = 0

            retVal = NativeMethods.HttpInitialize(HttpApiConstants.HTTPAPI_VERSION_1, HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if ((uint)ErrorCodes.NOERROR == retVal)
            {
                HTTP_SERVICE_CONFIG_URLACL_KEY keyDesc = new HTTP_SERVICE_CONFIG_URLACL_KEY(networkURL);
                HTTP_SERVICE_CONFIG_URLACL_PARAM paramDesc = new HTTP_SERVICE_CONFIG_URLACL_PARAM(securityDescriptor);

                HTTP_SERVICE_CONFIG_URLACL_SET inputConfigInfoSet = new HTTP_SERVICE_CONFIG_URLACL_SET();
                inputConfigInfoSet.KeyDesc = keyDesc;
                inputConfigInfoSet.ParamDesc = paramDesc;

                IntPtr pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_SET)));
                Marshal.StructureToPtr(inputConfigInfoSet, pInputConfigInfo, false);

                retVal = NativeMethods.HttpSetServiceConfiguration(IntPtr.Zero,
                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                    pInputConfigInfo,
                    Marshal.SizeOf(inputConfigInfoSet),
                    IntPtr.Zero);

                if ((uint)ErrorCodes.ERROR_ALREADY_EXISTS == retVal)  // ERROR_ALREADY_EXISTS = 183
                {
                    retVal = NativeMethods.HttpDeleteServiceConfiguration(IntPtr.Zero,
                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                    pInputConfigInfo,
                    Marshal.SizeOf(inputConfigInfoSet),
                    IntPtr.Zero);

                    if ((uint)ErrorCodes.NOERROR == retVal)
                    {
                        retVal = NativeMethods.HttpSetServiceConfiguration(IntPtr.Zero,
                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                            pInputConfigInfo,
                            Marshal.SizeOf(inputConfigInfoSet),
                            IntPtr.Zero);
                    }
                }

                Marshal.FreeCoTaskMem(pInputConfigInfo);
                NativeMethods.HttpTerminate(HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

            if ((uint)ErrorCodes.NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }
        }
        #endregion

        #region Delete
        public static void Delete(UrlReservation urlReservation)
        {
            string sddl = generateSddl(urlReservation._securityIdentifiers);
            freeURL(urlReservation.Url, sddl);
        }

        private static void freeURL(string networkURL, string securityDescriptor)
        {
            uint retVal = (uint)ErrorCodes.NOERROR;

            retVal = NativeMethods.HttpInitialize(HttpApiConstants.HTTPAPI_VERSION_1, HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if ((uint)ErrorCodes.NOERROR == retVal)
            {
                HTTP_SERVICE_CONFIG_URLACL_KEY urlAclKey = new HTTP_SERVICE_CONFIG_URLACL_KEY(networkURL);
                HTTP_SERVICE_CONFIG_URLACL_PARAM urlAclParam = new HTTP_SERVICE_CONFIG_URLACL_PARAM(securityDescriptor);

                HTTP_SERVICE_CONFIG_URLACL_SET urlAclSet = new HTTP_SERVICE_CONFIG_URLACL_SET();
                urlAclSet.KeyDesc = urlAclKey;
                urlAclSet.ParamDesc = urlAclParam;

                IntPtr configInformation = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_SET)));
                Marshal.StructureToPtr(urlAclSet, configInformation, false);
                int configInformationSize = Marshal.SizeOf(urlAclSet);

                retVal = NativeMethods.HttpDeleteServiceConfiguration(IntPtr.Zero,
                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                    configInformation,
                    configInformationSize,
                    IntPtr.Zero);

                Marshal.FreeCoTaskMem(configInformation);

                NativeMethods.HttpTerminate(HttpApiConstants.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

            if ((uint)ErrorCodes.NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }
        }
        #endregion

        #region Helper
        private static List<SecurityIdentifier> securityIdentifiersFromSDDL(string securityDescriptor)
        {
            CommonSecurityDescriptor csd = new CommonSecurityDescriptor(false, false, securityDescriptor);
            DiscretionaryAcl dacl = csd.DiscretionaryAcl;

            List<SecurityIdentifier> securityIdentifiers = new List<SecurityIdentifier>(dacl.Count);

            foreach (CommonAce ace in dacl)
            {
                securityIdentifiers.Add(ace.SecurityIdentifier);
            }

            return securityIdentifiers;
        }

        private static DiscretionaryAcl getDacl(List<SecurityIdentifier> securityIdentifiers)
        {
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 16);

            foreach (SecurityIdentifier sec in securityIdentifiers)
            {
                dacl.AddAccess(AccessControlType.Allow, sec, GENERIC_EXECUTE, InheritanceFlags.None, PropagationFlags.None);
            }

            return dacl;
        }

        private static CommonSecurityDescriptor getSecurityDescriptor(List<SecurityIdentifier> securityIdentifiers)
        {
            DiscretionaryAcl dacl = getDacl(securityIdentifiers);

            CommonSecurityDescriptor securityDescriptor =
                new CommonSecurityDescriptor(false, false,
                        ControlFlags.GroupDefaulted |
                        ControlFlags.OwnerDefaulted |
                        ControlFlags.DiscretionaryAclPresent,
                        null, null, null, dacl);
            return securityDescriptor;
        }

        private static string generateSddl(List<SecurityIdentifier> securityIdentifiers)
        {
            return getSecurityDescriptor(securityIdentifiers).GetSddlForm(AccessControlSections.Access);
        }
        #endregion

        public byte[] ToDaclBytes()
        {

            DiscretionaryAcl dacl = getDacl(this._securityIdentifiers);
            byte[] bytes = new byte[dacl.BinaryLength];
            dacl.GetBinaryForm(bytes, 0);
            return bytes;
        }

        public byte[] ToSaclBytes()
        {
            SystemAcl sacl = new SystemAcl(false, false, 0);
            byte[] bytes = new byte[sacl.BinaryLength];
            sacl.GetBinaryForm(bytes, 0);
            return bytes;
        }
    }
}
