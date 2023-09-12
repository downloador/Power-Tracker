using OpenHardwareMonitor.Hardware;
using System;
using System.Runtime.InteropServices;

namespace Power_Tracker
{
    public class Hardware
    {
        public int UpdateRateInMs = 100;
        public int TimeAverage = 15000;
        public int ListSize = TimeAverage / UpdateRateInMs;

        public List<double> CPU_AverageTempList = new List<double>();
        //

        public List<double> CPU_AverageDrawList = new List<double>();
        public double CPU_TotalDraw = 0;
        // Watt is a measurement in hour, check how high it is, how long since last check which
        // tells us how many watts it used in that millisecond timespan
        
        public double Epoch_ProgramStart; // Assignee by Controller in Main
        public double Epoch_FileLastWritten;
        public double Epoch_LastCheck;

        public bool AllowedToWrite = false;

        private Computer LocalComputer = new Computer()
        {
            CPUEnabled = true,
        };

        public void UpdateInfo() 
        {
            TimeSpan CurrentTime = Controller.RetrieveCurrentTime();

            foreach (var hardware in LocalComputer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    {
                        hardware.Update();
                        foreach (var sensor in hardware.Sensors)
                            if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("CPU Package"))
                            {
                                double Scope_CPU_Draw = Math.Round(sensor.Value.GetValueOrDefault());
                                double ActiveDrawPerMs = Scope_CPU_Draw / (60 * 60 * 1000);
                                double DurationInMs = CurrentTime.TotalMilliseconds - Epoch_LastCheck;

                                Console.WriteLine(DurationInMs);

                                CPU_TotalDraw += Scope_CPU_Draw / ActiveDrawPerMs * DurationInMs
                                // Draw divided by 3,6m ms * since last check to get total draw

                                if (CPU_AverageDrawList.Count >= ListSize) CPU.CPU_AverageDrawList.RemoveAt(0);
                                CPU_AverageDrawList.Add(Scope_CPU_Draw);
                            }
                            else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("CPU Package"))
                            {
                                double Scope_CPU_Temperature = sensor.Value.GetValueOrDefault();

                                if (CPU_AverageTempList.Count >= ListSize) CPU_AverageTempList.RemoveAt(0);
                                CPU_AverageTempList.Add(Scope_CPU_Temperature);
                            }
                    }
                }
            }

            Epoch_LastCheck = CurrentTime.TotalMilliseconds;
        }

        public double[] RetrieveInfo()
        {
            double[] ReturnData = new double[3];

            ReturnData[0] = CPU_AverageDrawList.Count > 0 ? Math.Round(CPU_AverageDrawList.Average(), 1) : 0.0;
            ReturnData[1] = Math.Round(CPU_TotalPowerDraw/1000, 2); // in kWh
            ReturnData[2] = CPU_AverageTemperature.Count > 0 ? Math.Round(CPU_AverageTemperature.Average(), 1) : 0.0;

            return ReturnData;
        }

        public void Start()
        {
            LocalComputer.Open()

            Thread.Sleep(5000);
            // wait a while just incase (i havent tried)

            Epoch_LastCheck = Epoch_ProgramStart = Controller.RetrieveCurrentTime().TotalMilliseconds;

            while (true)
            {
                UpdateInfo()

                Thread.Sleep(UpdateRateInMs);
            }
        }
    }
}