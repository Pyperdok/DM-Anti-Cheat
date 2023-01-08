using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Steamworks;
using System.Net;
using System.Net.Http;
using System.Management;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace DM_Anti_Cheat
{
   static public class Athorization
    {
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            //creating hash
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            var sBuilder = new StringBuilder();

            //Converting to string
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
        public static string GetHardwareID()
        {
            ManagementObjectSearcher searcherbase = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            ManagementObjectSearcher searcherproc = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            ManagementObjectSearcher searcherdisk = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");

            ManagementObjectCollection searcherbasecoll = searcherbase.Get();
            ManagementObjectCollection searcherproccoll = searcherproc.Get();
            ManagementObjectCollection searcherdiskcoll = searcherdisk.Get();
            
            string baseboard = "";
            foreach (ManagementObject mo in searcherbasecoll)
                foreach (PropertyData prop in mo.Properties)
                    baseboard += prop.Value;

            string processor = "";
            foreach (ManagementObject mo in searcherproccoll)
                foreach (PropertyData prop in mo.Properties)
                    processor += prop.Value;

            string disk = "";
            foreach (ManagementObject mo in searcherdiskcoll)
                foreach (PropertyData prop in mo.Properties)
                    disk += prop.Value;

            string hwid = (baseboard + processor + disk).Replace(" ", "");
            
            SHA256 sha = SHA256.Create();

            return GetHash(sha, hwid);
        }

        static public string Start()
        {
            //Get Cookie
            Console.WriteLine("Athorization Start...");
            //Generate ticket
            AuthTicket ticket = SteamUser.GetAuthSessionTicket();
            string AuthSesionTicket = BitConverter.ToString(ticket.Data).Replace("-", string.Empty);
          
            HttpResponseMessage result = new HttpResponseMessage();
            List<List<string>> headers = new List<List<string>>()
            {
                new List<string>(){"Ticket", AuthSesionTicket },
                new List<string>(){"Hwid", GetHardwareID()}
            };
            result = Network.Http($"{Network.host}/api/auth", HttpMethod.Post, headers, "");

           IEnumerable<string> cookie = result.Headers.GetValues("Set-Cookie");
           return cookie.First();
        }
        
        static public string ServiceVersionCheck()
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result = Network.Http($"{Network.host}/api/service", HttpMethod.Get, null, "");
            return result.Content.ReadAsStringAsync().Result;
        }
        static public bool BanStatusCheck(ref string cookie)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            List<List<string>> header = new List<List<string>>()
            {
                new List<string>(){"Session", cookie}
            };
            result = Network.Http($"{Network.host}/api/banstatus", HttpMethod.Post, header, "");
            JObject json = JObject.Parse(result.Content.ReadAsStringAsync().Result);
            if ((bool)json.GetValue("IsBanned"))
                return true;
            else
                return false;
        }

      async static public Task<List<string>> GameVersionCheck(List<string> SteamPaks)
        {
                if (!SteamApps.IsAppInstalled(381210)) throw new Exception("DBD is not installed");
                string gamepath = SteamApps.AppInstallDir(381210) + @"\DeadByDaylight.exe";

                //Swithing to DBD
                SteamClient.Shutdown();
                SteamClient.Init(381210);

                Steamworks.Data.FileDetails? info = await SteamApps.GetFileDetailsAsync(@"DeadByDaylight.exe");
                             
                if (info.Value.Sha1.Replace("0", "") != ShaCheck.GenerateSha1FromFile(gamepath, 62914560)) throw new Exception("Pls update the game");

                info = await SteamApps.GetFileDetailsAsync(@"DeadByDaylight\Content\Paks\pakchunk0-WindowsNoEditor.pak");
               
                SteamPaks.Add(info.Value.Sha1.Replace("0", ""));

                info = await SteamApps.GetFileDetailsAsync(@"DeadByDaylight\Content\Paks\pakchunk1-WindowsNoEditor.pak");
               
                SteamPaks.Add(info.Value.Sha1.Replace("0", ""));

                return SteamPaks;           
        }
    }
}
