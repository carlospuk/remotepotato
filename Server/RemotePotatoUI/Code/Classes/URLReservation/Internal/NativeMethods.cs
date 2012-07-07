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
using System.Text;
using System.Runtime.InteropServices;

namespace Austin.HttpApi.Internal
{
    internal static class NativeMethods
    {
        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpQueryServiceConfiguration(
             IntPtr ServiceIntPtr,
             HTTP_SERVICE_CONFIG_ID ConfigId,
             IntPtr pInputConfigInfo,
             int InputConfigInfoLength,
             IntPtr pOutputConfigInfo,
             int OutputConfigInfoLength,
             [Optional()]
             out int pReturnLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpSetServiceConfiguration(
             IntPtr ServiceIntPtr,
             HTTP_SERVICE_CONFIG_ID ConfigId,
             IntPtr pConfigInformation,
             int ConfigInformationLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpDeleteServiceConfiguration(
             IntPtr ServiceIntPtr,
             HTTP_SERVICE_CONFIG_ID ConfigId,
             IntPtr pConfigInformation,
             int ConfigInformationLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpInitialize(
             HTTPAPI_VERSION Version,
             uint Flags,
             IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpTerminate(
             uint Flags,
             IntPtr pReserved);
    }
}
