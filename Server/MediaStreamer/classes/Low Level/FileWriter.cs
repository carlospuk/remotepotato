using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FatAttitude.Functions
{
    public static class FileWriter
    {


        public static bool WriteTextFileToDisk(string filePath, string txtContent, Encoding encoding)
        {
            /*

            byte[] contents = Encoding.UTF8.GetBytes(txtContent);
            byte[] newContents;
            if (encoding != Encoding.UTF8)
                newContents = Encoding.Convert(Encoding.UTF8, encoding, contents);
            else
                newContents = contents;
            */

            try
            {
                // Delete if exists
                if (File.Exists(filePath))
                    File.Delete(filePath);

                /*using (BinaryWriter bw = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    bw.Write(newContents);
                }*/

                

                //TextWriter tw = new StreamWriter(filePath, false, encoding );
                TextWriter tw = new StreamWriter(filePath);
                tw.Write(txtContent);
                tw.Close();

                return true;
            }
            catch 
            {
                return false;
            }
        }


        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength
                 );

        public static string GetShortPathName(string fileName)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(fileName, shortPath, shortPath.Capacity);
            return shortPath.ToString();
        }
       
    }
}
