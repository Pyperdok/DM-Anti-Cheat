using System;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace DM_Anti_Cheat
{
    public static class ShaCheck
    {
        public static string GenerateSha1FromFile(string filepath, int blocksize)
        {
            FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            SHA1 sha1 = new SHA1Managed();

            byte[] buffer = new byte[blocksize];

            int readbytes;
            while (true)
            {
                Thread.Sleep(1000);
                readbytes = fs.Read(buffer, 0, blocksize);

                if (readbytes == 0)
                {
                    sha1.TransformFinalBlock(buffer, 0, 0);
                    break;
                }
                if (readbytes <= blocksize)
                    sha1.TransformBlock(buffer, 0, readbytes, buffer, 0);
            }

            fs.Close();
            fs.Dispose();

            string hash = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLower().Replace("0", "");

            sha1.Dispose();
            return hash;
        }
    }
}