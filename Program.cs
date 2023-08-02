using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows;
using System.Drawing;
using System.Diagnostics;
using System.Security.Principal;

using OpenHardwareMonitor.Hardware;

namespace PowerTracker
{
    class Program
    {
        /**
         *  Variables
         **/

        static double CPU_PowerDraw;
        static double Total_CPU_PowerDraw = 0;

        static double Epoch_ProgramStart;
        static double Epoch_LastCheck;
        static double Epoch_LastWattageReport;

        const int Watt_kWhtoMilliseconds = 60 * 60 * 1000;
        const double Watt_Cost = 1.6 / 1000;

        static string ResidingFolder = AppDomain.CurrentDomain.BaseDirectory;
        static Boolean AllowedToWrite = false;

        /**
         *  Console and NotifyIcon
         **/

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static NotifyIcon notifyIcon = new NotifyIcon();
        static Boolean Visible = true;

        /**
         *  Retrieving Begin
         **/

        static Computer c = new Computer()
        {
            CPUEnabled = true,
        };

        static TimeSpan RetrieveCurrentTime() => TimeSpan.FromTicks(DateTime.Now.Ticks);

        static void GetShittyProcessorInfo()
        {
            foreach (var hardware in c.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {

                    hardware.Update();

                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("CPU Package"))
                        {
                            TimeSpan CurrentTime = RetrieveCurrentTime();

                            CPU_PowerDraw = Math.Round(sensor.Value.GetValueOrDefault(), 1);
                            Total_CPU_PowerDraw += (CPU_PowerDraw / Watt_kWhtoMilliseconds) * (CurrentTime.TotalMilliseconds - Epoch_LastCheck);

                            if (AllowedToWrite == true) Console.Write("\r[" + new DateTime(CurrentTime.Ticks).ToString("HH:mm:ss") + "] " + (CPU_PowerDraw));

                            File.AppendAllText(
                                ResidingFolder + "\\WattageData.txt",
                                Convert.ToString(CurrentTime.TotalMilliseconds - Epoch_LastWattageReport) + " : " + Total_CPU_PowerDraw + Environment.NewLine
                            );

                            Epoch_LastCheck = CurrentTime.TotalMilliseconds;
                            Epoch_LastWattageReport = Epoch_LastCheck;
                        }
                }

            }
        }
        static void Main(string[] args)
        {
            /**
             *  Console and NotifyIcon Init
             **/

            AllocConsole();
            if (ResidingFolder == "C\\Windows\\system32") ResidingFolder = "C:\\Users\\Peliexpress\\Documents\\Power Tracker";

            static bool IsAdministrator()
            {
                return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                          .IsInRole(WindowsBuiltInRole.Administrator);
            }

            if (IsAdministrator() == false)
            {
                Process.GetCurrentProcess().Kill();
            }

            //notifyIcon.DoubleClick += (s, e) =>
            //{
            //    Console.WriteLine("Double clicked");
            //};
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Reset Wattage", null, (s, e) => {
                TimeSpan CurrentTime = RetrieveCurrentTime();

                File.AppendAllText(
                    ResidingFolder + "\\Logs.txt",
                    "[" + new DateTime(CurrentTime.Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "User requested wattage data reset : " + Convert.ToString(Total_CPU_PowerDraw) + Environment.NewLine
                );
                Total_CPU_PowerDraw = 0;
            });
            //contextMenu.Items.Add("Copy to Clipboard", null, (s, e) => { Console.WriteLine("Copy to Clipboard"); });
            contextMenu.Items.Add("Exit Application", null, (s, e) => { Process.GetCurrentProcess().Kill(); });
            contextMenu.Items.Add("Toggle Visibility", null, (s, e) => {
                var handle = GetConsoleWindow();

                Visible = !Visible;
                if (Visible == true)
                {
                    ShowWindow(handle, SW_SHOW);
                }
                else
                {
                    ShowWindow(handle, SW_HIDE);
                }
            });
            notifyIcon.ContextMenuStrip = contextMenu;

            /**
             *  Start
             **/

            // Writing when the last report was written

            TimeSpan CurrentTime = RetrieveCurrentTime();
            Epoch_ProgramStart = Epoch_LastCheck = (double)CurrentTime.TotalMilliseconds;

            StreamReader SR = new StreamReader(ResidingFolder + "\\WattageData.txt");
            string Offset = (SR.ReadLine() ?? "START : -1").Split(" : ")[1];
            string[] LastReportedData = (File.ReadLines(ResidingFolder + "\\WattageData.txt").Last() ?? "0 : 0").Split(" : ");
            if (LastReportedData[0] == "START") LastReportedData = new string[2] { "0", "0" };
            SR.Close();

            if (Offset == "-1")
            {
                Offset = Convert.ToString(Epoch_ProgramStart);
                string[] Lines = File.ReadAllLines(ResidingFolder + "\\WattageData.txt");
                Lines[0] = "START : " + Offset;
                File.WriteAllLines(ResidingFolder + "\\WattageData.txt", Lines);
            }

            Epoch_LastWattageReport = Convert.ToDouble(Offset) + Convert.ToDouble(LastReportedData[0]);
            Total_CPU_PowerDraw = Convert.ToDouble(LastReportedData[1]);

            File.AppendAllText(
                ResidingFolder + "\\Logs.txt",
                "[" + new DateTime(TimeSpan.FromMilliseconds(Epoch_LastWattageReport).Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "Last reported wattage data" + Environment.NewLine
            );

            // Log opening time

            File.AppendAllText(
                ResidingFolder + "\\Logs.txt",
                "[" + new DateTime(CurrentTime.Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "Application opened" + Environment.NewLine
            );

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                c.Open();

                while (true)
                {
                    GetShittyProcessorInfo();
                    Thread.Sleep(1000);
                }
            }).Start();

            // Application begins now

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.Sleep(1000);

                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine("Hiding Console window in " + Convert.ToString(5-i) + Environment.NewLine);
                    Thread.Sleep(1000);
                }

                var handle = GetConsoleWindow();
                Visible = false;
                AllowedToWrite = true;
                ShowWindow(handle, SW_HIDE);
            }).Start();

            Application.Run();
        }
    }
}