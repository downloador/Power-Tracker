using OpenHardwareMonitor.Hardware;
using System;
using System.Runtime.InteropServices;

namespace Power_Tracker
{
    public class Hardware
    {
        public static double CPU_TotalPowerDraw = 0;
        private static List<double> CPU_AverageTemperature = new List<double>();
        private static int TimeAverage = 15;
        
        public static double Epoch_ProgramStart; // Assignee by Controller in Main
        public static double Epoch_FileLastWritten;
        private static double Epoch_LastCheck;

        public static bool AllowedToWrite = false;

        private static Computer LocalComputer = new Computer()
        {
            CPUEnabled = true,
        };

        public static double[] GetProcessorInfo()
        {
            TimeSpan CurrentTime = Controller.RetrieveCurrentTime();

            double CPU_PowerDraw = 0;
            double CPU_Temperature = 0;
            double[] returnData = new double[3];

            foreach (var hardware in LocalComputer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    {
                        hardware.Update();
                        foreach (var sensor in hardware.Sensors)
                            if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("CPU Package"))
                            {
                                CPU_PowerDraw = Math.Round(sensor.Value.GetValueOrDefault());
                                CPU_TotalPowerDraw += (CPU_PowerDraw / 3600000) * (CurrentTime.TotalMilliseconds - Epoch_LastCheck);

                                using (StreamWriter outputFile = new StreamWriter(Application.StartupPath + "\\Wattage.txt"))
                                {
                                    string OutputData = Convert.ToString(CPU_TotalPowerDraw);
                                    OutputData = OutputData.Replace(".", ",");

                                    outputFile.WriteLine("Wattage : " + OutputData);
                                    outputFile.Close();					
                                }
                            }
                            else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("CPU Package"))
                            {
                                CPU_Temperature = sensor.Value.GetValueOrDefault();

                                if (CPU_AverageTemperature.Count >= TimeAverage) CPU_AverageTemperature.RemoveAt(0);
                                CPU_AverageTemperature.Add(CPU_Temperature);
                            }
                    }
                }
            }

            Epoch_LastCheck = CurrentTime.TotalMilliseconds;

            returnData[0] = CPU_PowerDraw;
            returnData[1] = Math.Round(CPU_TotalPowerDraw/1000, 2);
            returnData[2] = CPU_AverageTemperature.Count > 0 ? Math.Round(CPU_AverageTemperature.Average(), 1) : 0.0;

            if (AllowedToWrite == true) Console.Write("\r[" + new DateTime(CurrentTime.Ticks).ToString("HH:mm:ss") + "]    " + $"Power: {CPU_PowerDraw}w    Total Power: {returnData[1]}kWh    Temperature: {returnData[2]}°C               ");

            return returnData;
        }

        public static void Initialize()
        {
            LocalComputer.Open();

            Epoch_LastCheck = Epoch_ProgramStart = Controller.RetrieveCurrentTime().TotalMilliseconds;
        }
    }
}