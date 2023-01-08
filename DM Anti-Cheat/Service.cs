using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using System.Net.Http;
using System.Text;
using System.Net.NetworkInformation;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DM_Anti_Cheat
{
    public static class Service
    {

       // [DllImport("user32.dll")]
       // [return: MarshalAs(UnmanagedType.Bool)]
       //public static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

       // [StructLayout(LayoutKind.Sequential)]
       // public struct RECT
       // {
       //     public int Left;        // x position of upper-left corner
       //     public int Top;         // y position of upper-left corner
       //     public int Right;       // x position of lower-right corner
       //     public int Bottom;      // y position of lower-right corner
       // }

      public static  void Crash(string msg)
        {
            FileStream fs = File.Open("crash.log", FileMode.Append, FileAccess.Write);
            msg = $"[{DateTime.Now.ToString()}] {msg}\r\n";
            fs.Write(Encoding.ASCII.GetBytes(msg), 0, Encoding.ASCII.GetBytes(msg).Length);

            fs.Close();
            fs.Dispose();

            SteamClient.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        public static void PaksCheck(List<string> SteamPaks, ref string cookie, int retryies = 0)
        {
            List<List<string>> header = new List<List<string>>()
                {
                    new List<string>(){"Session", cookie}
                };
            try {
                List<string> ClientPaks = new List<string>();
                string gamepath = SteamApps.AppInstallDir(381210);

                string pak0path = @"\DeadByDaylight\Content\Paks\pakchunk0-WindowsNoEditor.pak";
                string pak1path = @"\DeadByDaylight\Content\Paks\pakchunk1-WindowsNoEditor.pak";

                while (true)
                {
                    ClientPaks.Add(ShaCheck.GenerateSha1FromFile(gamepath + pak0path, 20971520)); //62914560 // 20971520 = 20 MB
                    ClientPaks.Add(ShaCheck.GenerateSha1FromFile(gamepath + pak1path, 20971520)); //15 MB = 15 728 640 B

                    string json = "{";
                    for (int i = 0; i < 2; i++)
                        if (SteamPaks[i] == ClientPaks[i])
                            json += $"\"pak{i}\":true{(i == 0 ? "," : "")}";
                        else
                            json += $"\"pak{i}\":false{(i == 0 ? "," : "")}";
                    json += "}";

                    string body = $"{{\"Data\":{json}}}";

                    Network.Http($"{Network.host}/api/paks", HttpMethod.Post, header, body);
                }
            }
            catch(Exception ex) {
                Service.Crash($"Paks Error: {ex.Message}");
            }
        }

        public static void Ping(ref string cookie, int retryies = 0)
        {
            try
            {
                Ping ping = new Ping();
                List<string> hosts = new List<string>()
            {
                "ap-south-1",
                "eu-west-1",
                "ap-southeast-1",
                "ap-southeast-2",
                "eu-central-1",
                "ap-northeast-1",
                "ap-northeast-2",
                "us-east-1",
                "sa-east-1",
                "us-west-2"
            };
                List<List<string>> header = new List<List<string>>()
                {
                    new List<string>(){"Session", cookie}
                };
                while (true)
                { 
                    string json = "{";
                    for (int i = 0; i < hosts.Count; i++)
                        json += $"\"{hosts[i]}\":{ping.Send($"gamelift.{hosts[i]}.amazonaws.com").RoundtripTime}{(i != hosts.Count - 1 ? "," : "")}";
                    json += "}";

                    string body = $"{{\"Data\":{json}}}";
                    //  Console.WriteLine(body);
                    // connection.Send(Encoding.UTF8.GetBytes(body));

                    Network.Http($"{Network.host}/api/ping", HttpMethod.Post, header, body);
                    retryies = 0;
                }
            }
            catch(Exception ex)
            {
                if (retryies > 7) Service.Crash($"Ping Error: {ex.Message}");
                Ping(ref cookie, ++retryies);
            }
        }

        public static void Config(ref string cookie, int retryies = 0)
        {
            try
            {
                string configpath = @"\DeadByDaylight\Saved\Config\WindowsNoEditor\";
                string localpath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                configpath = localpath + configpath;

                List<string> files = new List<string>()
                {
                "ApexDestruction", "Compat", "DataprepEditor", "DBDChunking", /*"Design",*/
                "DeviceProfiles", /*"EditorPerProjectUserSettings",*/ "EditorScriptingUtilities", "Engine", "Game",
                "GameplayTags", "GameUserSettings", "Hardware", "Input", /*"Lightmass",*/
               "MagicLeap", "MagicLeapLightEstimation", "MediaIOFramework", "Niagara", "Paper2D", "PhysXVehicles",
                "RuntimeOptions", "Scalability", "Synthesis", "VariantManagerContent"
                };
                List<List<string>> header = new List<List<string>>()
                {
                new List<string>(){"Session", cookie}
                };
                HttpResponseMessage result = new HttpResponseMessage();
                while (true)
                {
                    string config = "";
                    for (int i = 0; i < files.Count; i++)
                    {
                        //  Console.WriteLine(files[i]);
                        string line = string.Empty;
                        using (StreamReader stream = new StreamReader(configpath + files[i] + ".ini"))
                        {
                            while ((line = stream.ReadLine()) != null)
                            {
                                if (line != string.Empty)
                                    config += line.Replace(" ", "") + "\r\n";
                            }
                        }
                    }

                    string body = $"{config.Replace("\"", "\\\"")}";

                    Network.Http($"{Network.host}/api/config", HttpMethod.Post, header, body);
                    Thread.Sleep(5000);
                    retryies = 0;
                }
            }
            catch(Exception ex)
            {
                if (retryies > 2) Service.Crash($"Config Error: {ex.Message}");
                Config(ref cookie, ++retryies);
            }
        }
        public static void GetCpu(double[] cpu, int offset, Process process)
        {
            try
            {
                TimeSpan dt1 = process.TotalProcessorTime;
                Stopwatch Timer = Stopwatch.StartNew();
                Task delay = Task.Delay(125);
                delay.Wait();
                TimeSpan dt2 = process.TotalProcessorTime;
                Timer.Stop();
                Console.WriteLine($"Elapsed: {Timer.ElapsedMilliseconds}");
                double t = (dt2 - dt1).TotalMilliseconds / (Environment.ProcessorCount * Timer.ElapsedMilliseconds) * 100;
                cpu[offset] = Math.Round(t, 2);
            }
            catch
            {
                cpu[offset] = -1.0;
            }
        }

        public static void GetRam(double[] ram, int offset, Process process)
        {
            try
            {
                ram[offset] = Math.Round((double)process.WorkingSet64 / 1048576, 2);
            }
            catch
            {
                ram[offset] = -1.0;
            }
        }
 
        //Get Active Network Interface
        public static NetworkInterface GetActiveNetworkInterface()
        {
            NetworkInterface[] nt = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < nt.Length; i++)
            {
                IPAddressCollection addresses = nt[i].GetIPProperties().DnsAddresses;
                bool isVirtual = false;
                for(int k = 0; k < addresses.Count; k++)
                    if (addresses[k].IsIPv6SiteLocal) isVirtual = true;

                if (nt[i].OperationalStatus == OperationalStatus.Up && nt[i].NetworkInterfaceType != NetworkInterfaceType.Loopback && !isVirtual)
                    return nt[i];
            }

            throw new Exception("NetInterface Eror");
        }

        public static NetworkInterface GetActiveNetworkInterfaceName()
        {
            NetworkInterface[] nt = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < nt.Length; i++)
                if (nt[i].OperationalStatus == OperationalStatus.Up && nt[i].NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    return nt[i];
            throw new Exception("GetActiveNetworkInterfaceName Error");
        }

        public static double GetNet(NetworkInterface nt)
        {
            Stopwatch st = new Stopwatch();
            st.Start();
            var info = nt.GetIPv4Statistics();
            var d1 = info.BytesReceived + info.BytesSent;
            Task delay = Task.Delay(1000);
            delay.Wait();
            info = nt.GetIPv4Statistics();
            st.Stop();
            var d2 = info.BytesReceived + info.BytesSent;
            var delta = d2 - d1;

            double BytesInSec = (double)delta / (st.ElapsedMilliseconds / 1000);
            double result = BytesInSec / (nt.Speed / 8) * 100;

            return Math.Round(result, 2);
        }

        public static double GetCPUTotal()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(1000);
            return Math.Round(cpuCounter.NextValue(), 2);
        }

        //Processes Info
        public static void Processes(ref string cookie, IntPtr hwnd, int retryies = 0)
        {
            try
            {
               // Service.RECT rect = new Service.RECT();

                NetworkInterface NetworkCard = GetActiveNetworkInterface();
                //Processes LOOP
                while (true)
                {
                    //Get Game Resolution                  
                   // GetWindowRect(new HandleRef(null, hwnd), out rect);
                    
                    //Get All Processes
                    Process[] processes = Process.GetProcesses();

                    double[] cpu = new double[processes.Length];
                    double[] ram = new double[processes.Length];

                    for (int i = 0; i < processes.Length; i++)
                    {
                        GetCpu(cpu, i, processes[i]);
                        GetRam(ram, i, processes[i]);
                    }

                    string body = "{\"Processes\":[";

                    int PID;
                    string ProcName;
                    string Path;
                    string Cpu;
                    string Ram;

                    for (int i = 0; i < processes.Length; i++)
                    {
                        PID = processes[i].Id;
                        ProcName = processes[i].ProcessName;

                        Cpu = cpu[i].ToString().Replace(",", ".");
                        Ram = ram[i].ToString().Replace(",", ".");
                        try
                        {
                            Path = processes[i].MainModule.FileName.Replace("\\", "\\\\");
                        }
                        catch
                        {
                            Path = "DENIED";
                        }
                        body += $"{{\"PID\":{PID},\"Name\":\"{ProcName}\",\"Path\":\"{Path}\", \"Cpu\":{Cpu},\"Ram\":{Ram}}}{(i != processes.Length - 1 ? "," : "],")}";
                    }

                    body += $"\"Cpu_Net\":{{\"MaxSpeed\":{NetworkCard.Speed},\"Type\":\"{NetworkCard.NetworkInterfaceType}\",\"NetLoad\":{GetNet(NetworkCard).ToString().Replace(",", ".")},\"CpuLoad\":{GetCPUTotal().ToString().Replace(",", ".")}}},";
                    body += $"\"Resolution\":{{\"Width\":0,\"Height\":0}}}}";
                    // Console.WriteLine(body);
                    List<List<string>> header = new List<List<string>>()
                    {
                    new List<string>(){"Session", cookie}
                    };

                    HttpResponseMessage result = new HttpResponseMessage();
                    Network.Http($"{Network.host}/api/processes", HttpMethod.Post, header, body);
                    retryies = 0;
                }
            }
            catch(Exception ex)
            {
                if (retryies > 2) Service.Crash($"Processes Error: {ex.Message}");
                Processes(ref cookie, hwnd, ++retryies);
            }
        }      
    }
}
