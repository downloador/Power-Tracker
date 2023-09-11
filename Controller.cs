using Discord;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Power_Tracker
{
    class Controller
    {
        public static bool Debugging = true;

        static NotifyIcon notifyIcon = new NotifyIcon();
        static Boolean Visible = true;

        public static TimeSpan RetrieveCurrentTime() => TimeSpan.FromTicks(DateTime.Now.Ticks);

        public static void KillProgramWithReason(string ErrorMessage, int Duration)
        {
            for (int i = 0; i < Duration; i++)
            {
                Console.Write("\r{0}", ErrorMessage);
                Thread.Sleep(1000);
            }
            Process.GetCurrentProcess().Kill();
        }
        static double ConvertStringToDouble(string Input)
        {
            double ParsedString = double.Parse(Input, CultureInfo.GetCultureInfo("en-US"));
            string ParsedDouble = Convert.ToString(ParsedString);

            if (ParsedDouble == Input) return ParsedString;

            ParsedString = double.Parse(Input, CultureInfo.GetCultureInfo("sv-SE"));

            return ParsedString;
        }
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("sv-SE");

            // Open up a console window
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AllocConsole();
            AllocConsole();


            // Prepration

            if ((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                KillProgramWithReason("Application was not ran as Administrator, closing in 3 seconds.", 3);
            }

            Hardware.Initialize();
            Discord.Initialize();

            // Application External Controls

            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit Application", null, (s, e) => { Process.GetCurrentProcess().Kill(); });
            contextMenu.Items.Add("Toggle Visibility", null, (s, e) => {
                [DllImport("kernel32.dll")]
                static extern IntPtr GetConsoleWindow();
                [DllImport("user32.dll")]
                static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

                var handle = GetConsoleWindow();

                Visible = !Visible;
                if (Visible == true)
                {
                    ShowWindow(handle, 5);
                }
                else
                {
                    ShowWindow(handle, 0);
                }
            });
            notifyIcon.ContextMenuStrip = contextMenu;

            // Reading data

            DateTime AwfulVariableAssigne = System.IO.File.GetLastWriteTime(Application.StartupPath + "\\Wattage.txt");
            Hardware.Epoch_FileLastWritten = TimeSpan.FromTicks(AwfulVariableAssigne.Ticks).TotalMilliseconds;

            File.AppendAllText(
                Application.StartupPath + "\\Logs.txt",
                "[" + AwfulVariableAssigne.ToString("dd/MM-yyyy HH:mm:ss] ") + "Last reported wattage data" + Environment.NewLine +
                "[" + new DateTime(RetrieveCurrentTime().Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "Application opened" + Environment.NewLine
            );
            Discord.LogMessage("[" + AwfulVariableAssigne.ToString("dd/MM-yyyy HH:mm:ss] ") + "Last reported wattage data");
            Discord.LogMessage("[" + new DateTime(RetrieveCurrentTime().Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "Application opened");

            StreamReader SR = new StreamReader(Application.StartupPath + "\\Wattage.txt");
            string WattageData = SR.ReadLine();
            SR.Close();

            try
            {
                string[] SplitData = WattageData.Split(" : ");
                string FuckThisCulturesShit = string.Join("", SplitData[1]).Replace(".", ",");
                Console.WriteLine(FuckThisCulturesShit + " " + SplitData[1]);
                Hardware.CPU_TotalPowerDraw = double.Parse(FuckThisCulturesShit, CultureInfo.GetCultureInfo("sv-SE"));
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Invalid parameter of file, reset to 0");
                Hardware.CPU_TotalPowerDraw = 0;
            }

            // Run the program and then run the application in foreground

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (true)
                {
                    double[] ProcessorInfo = Hardware.GetProcessorInfo();
                    Discord.UpdateMessage(ProcessorInfo[0], ProcessorInfo[1], ProcessorInfo[2]);

                    Thread.Sleep(1000);
                }
            }).Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine("Hiding Console window in " + Convert.ToString(5 - i));
                    Thread.Sleep(1000);
                }

                [DllImport("kernel32.dll")]
                static extern IntPtr GetConsoleWindow();
                [DllImport("user32.dll")]
                static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

                var handle = GetConsoleWindow();

                Visible = false;
                Hardware.AllowedToWrite = true;
                ShowWindow(handle, 0);
            }).Start();

            Application.Run();
        }
    }
}
