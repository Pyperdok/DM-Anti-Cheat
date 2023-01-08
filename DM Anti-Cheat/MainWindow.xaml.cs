using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Steamworks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;

namespace DM_Anti_Cheat
{
    public partial class MainWindow : Window
    {
        //DMAC Version
       readonly string DMACVersion = "v0.3";
       string cookie = "";
       string gamepath = "";
       public List<string> SteamPaks = new List<string>();

        public MainWindow()
        {        
            try
            {
                InitializeComponent();
                new Thread(() =>
                {
                    Dispatcher.Invoke(() => TB_DMAC_Version.Text = DMACVersion);
                //Steamworks Initialization
                try
                    {
                        SteamClient.Init(1435000);
                    }
                //Steamworks fail
                catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        SteamClient.Shutdown();
                        Environment.Exit(-1);
                    }

                //DMAC Starting...
                Dispatcher.Invoke(() =>
                    {
                        TB_Auth.Visibility = Visibility.Visible;
                        TB_AuthStatus.Text = "Checking";
                        TB_AuthStatus.Foreground = Brushes.Yellow;
                        TB_AuthStatus.Visibility = Visibility.Visible;
                    });
                    try
                    {
                        cookie = DM_Anti_Cheat.Athorization.Start();
                        Dispatcher.Invoke(() =>
                                {
                                    TB_AuthStatus.Text = $"OK ({SteamClient.Name})";
                                    TB_AuthStatus.Foreground = Brushes.Lime;
                                });
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TB_AuthStatus.Text = $"Failed";
                            TB_AuthStatus.Foreground = Brushes.Red;
                        });
                        MessageBox.Show($"Authorization Failed\nError: {ex.Message}");
                        SteamClient.Shutdown();
                        Environment.Exit(-1);
                    }

                // DMAC_ServiceCheck // DMAC_VersionCheck
                Dispatcher.Invoke(() =>
                    {
                        TB_DMACService.Visibility = Visibility.Visible;
                        TB_DMACVersion.Visibility = Visibility.Visible;

                        TB_DMACServiceStatus.Text = "Checking";
                        TB_DMACServiceStatus.Foreground = Brushes.Yellow;
                        TB_DMACServiceStatus.Visibility = Visibility.Visible;

                        TB_DMACVersionStatus.Text = "Checking";
                        TB_DMACVersionStatus.Foreground = Brushes.Yellow;
                        TB_DMACVersionStatus.Visibility = Visibility.Visible;
                    });

                    try
                    {
                        string ServiceStatus = DM_Anti_Cheat.Athorization.ServiceVersionCheck();
                        JObject json = JObject.Parse(ServiceStatus);
                        if (json.GetValue("Service_Status").ToString() != "Alive") throw new Exception("Service is not Alive or Unknown Error");
                        if (json.GetValue("LastVersion").ToString() != DMACVersion) throw new Exception("Client has not same version with service or Unknown Error");
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TB_DMACServiceStatus.Text = "Failed";
                            TB_DMACServiceStatus.Foreground = Brushes.Red;
                            TB_DMACVersionStatus.Text = "Failed";
                            TB_DMACVersionStatus.Foreground = Brushes.Red;
                        });
                        MessageBox.Show($"Service Failed\nError: {ex.Message}");
                        SteamClient.Shutdown();
                        Environment.Exit(-1);
                    }
                    
                    Dispatcher.Invoke(() =>
                    {
                        TB_DMACServiceStatus.Text = "OK";
                        TB_DMACServiceStatus.Foreground = Brushes.Lime;
                        TB_DMACVersionStatus.Text = "OK";
                        TB_DMACVersionStatus.Foreground = Brushes.Lime;
                    });

                // DMAC_BanStatus
                Dispatcher.Invoke(() =>
                    {
                        TB_BanStatus.Visibility = Visibility.Visible;
                        TB_BanStatusStatus.Text = "Checking";
                        TB_BanStatusStatus.Foreground = Brushes.Yellow;
                        TB_BanStatusStatus.Visibility = Visibility.Visible;
                    });

                    try
                    {
                        if (DM_Anti_Cheat.Athorization.BanStatusCheck(ref cookie))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TB_BanStatusStatus.Text = "Banned";
                                TB_BanStatusStatus.Foreground = Brushes.Red;
                            });
                            MessageBox.Show("You are banned by DM Anti-Cheat");
                            SteamClient.Shutdown();
                            Environment.Exit(-1);
                        }
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TB_BanStatusStatus.Text = "Failed";
                            TB_BanStatusStatus.Foreground = Brushes.Red;
                        });
                        MessageBox.Show($"Unknown Error with Ban Status\nError: {ex.Message}");
                        SteamClient.Shutdown();
                        Environment.Exit(-1);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        TB_BanStatusStatus.Text = "OK";
                        TB_BanStatusStatus.Foreground = Brushes.Lime;
                    });

                // DMAC_GameVersion
                    Dispatcher.Invoke(() =>
                    {
                        TB_GameVersion.Visibility = Visibility.Visible;

                        TB_GameVersionStatus.Text = "Checking";
                        TB_GameVersionStatus.Foreground = Brushes.Yellow;
                        TB_GameVersionStatus.Visibility = Visibility.Visible;
                    });
                    try
                    {
                        gamepath = SteamApps.AppInstallDir(381210) + @"\DeadByDaylight.exe";
                        Thread check = new Thread(async () =>
                        {
                            SteamPaks = await Athorization.GameVersionCheck(SteamPaks);
                        });
                        check.Start();
                        while (SteamPaks.Count != 2) ;
                        Dispatcher.Invoke(() =>
                        {
                            TB_GameVersionStatus.Text = "OK";
                            TB_GameVersionStatus.Foreground = Brushes.Lime;
                        });
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TB_GameVersionStatus.Text = "Failed";
                            TB_GameVersionStatus.Foreground = Brushes.Red;
                        });
                        MessageBox.Show($"Game Version Check Failed\nError: {ex.Message}");
                        SteamClient.Shutdown();
                        Environment.Exit(-1);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        TB_LaunchGame.Visibility = Visibility.Visible;

                        TB_LaunchGameStatus.Visibility = Visibility.Visible;
                        TB_LaunchGameStatus.Text = "Launching";
                        TB_LaunchGameStatus.Foreground = Brushes.Yellow;
                    });
                
                      Process dbd = new Process();
                      dbd.StartInfo.FileName = $"{gamepath}";
                     // dbd.StartInfo.Arguments = "-fullscreen -dx12 -dxlevel 81 -lv -novid -nopix -nojoy -d3d10 -lowmemory -USEALLAVAILABLECORES -sm4 -nomansky -novsync-malloc=system";
                      dbd.Start();
                    
                    Process[] dbd_check;

                    while (true)
                    {
                        dbd_check = Process.GetProcessesByName("DeadByDaylight-Win64-Shipping");
                        if (dbd_check.Length != 0) break;
                    }
                    IntPtr hwnd = dbd_check[0].MainWindowHandle;

                    //Paks Check
                    Thread PaksThread = new Thread(() => Service.PaksCheck(SteamPaks, ref cookie));
                    PaksThread.Start();

                    //Ping
                    Thread PingThread = new Thread(() => Service.Ping(ref cookie));
                    PingThread.Start();

                    //Config
                    Thread ConfigThread = new Thread(() => Service.Config(ref cookie));
                    ConfigThread.Start();

                    //Processes and Game Resolution
                  //  Thread ProcessesThread = new Thread(() => Service.Processes(ref cookie, hwnd));
                  //  ProcessesThread.Start();

                    //while (true);
                    Dispatcher.Invoke(() =>
                    {
                        TB_LaunchGameStatus.Text = "Launched";
                        TB_LaunchGameStatus.Foreground = Brushes.Lime;
                    });
                    Dispatcher.Invoke(() => TB_SecureSession.Visibility = Visibility.Visible);

                // DMAC_LaunchGame
                Console.WriteLine("dbd wait");
                    dbd_check[0].WaitForExit();
                    Console.WriteLine("dbd closed");
                    SteamClient.Shutdown();
                    Environment.Exit(0);
                }).Start();
            }
            catch(Exception ex)
            {
                Service.Crash($"Main Error: {ex.Message}");
            }
        }
        //Mouse Move Form
        private void Move_Form(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void Window_Minimize(object senderm, EventArgs e) {
            Action action = () =>
            {              
                Console.WriteLine("Minimizated");
                WindowState = WindowState.Minimized;
                if (WindowState == WindowState.Minimized)
                {
                    Console.WriteLine("Start hiding");
                    Hide();
                    Tray.Visibility = Visibility.Visible;
                    Tray.ShowBalloonTip("DM Anti-Cheat", "DM Anti-Cheat is working here", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    Console.WriteLine("Hiding end");
                }
            };
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private void Button_Click_TraytoWindow(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Button_Click_TraytoWindow");

            Action action = () =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    Console.WriteLine("Show");
                    Show();
                    Tray.Visibility = Visibility.Hidden;
                    WindowState = WindowState.Normal;
                }
            };
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }
        private void Window_Close(object sender, MouseButtonEventArgs e)
        {
            SteamClient.Shutdown();
            Environment.Exit(0);
        }
        private void Button_Minimize_Enter(object sender, MouseEventArgs e) =>
            BT_Minimize.Background = new SolidColorBrush(Color.FromRgb(36, 135, 36)); //#248724
        private void Button_Minimize_Leave(object sender, MouseEventArgs e) =>
            BT_Minimize.Background = Brushes.Purple;
        private void Button_Close_Enter(object sender, MouseEventArgs e) =>
            BT_Close.Background = new SolidColorBrush(Color.FromRgb(219, 22, 22)); //#db1616
        private void Button_Close_Leave(object sender, MouseEventArgs e) =>
            BT_Close.Background = Brushes.Purple;
    }
}
