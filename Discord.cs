using Discord.WebSocket;
using Discord;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Windows.Media.Protection.PlayReady;
using System.Windows.Interop;
using System.Threading.Channels;
using Color = Discord.Color;
using System.Windows.Controls;

namespace Power_Tracker
{
    public class Discord
    {
        public static async void Initialize()
        {
            StreamReader SR = new StreamReader(Application.StartupPath + "\\DiscordMessage.txt");
            MessageID = Convert.ToUInt64(SR.ReadLine());
            SR.Close();

            await new Discord().MainAsync();
        }
        public static double NextAllowedEdit = Controller.RetrieveCurrentTime().TotalMilliseconds + 15000;
        // Wait for main to finish, 5 seconds is enough I think (I hope)
        private static ulong MessageID = 0;

        private DiscordSocketClient? _client;
        private static SocketTextChannel ChannelMessageResidesIn;
        private static SocketTextChannel LogChannel;

        private static double TimeBetweenEdit = 30000;

        public async static void UpdateMessage(double PowerDraw, double Total_PowerDraw, double CPU_Average)
        {
            double CurrentTime = Controller.RetrieveCurrentTime().TotalMilliseconds;

            if (CurrentTime < NextAllowedEdit) return;
            NextAllowedEdit = CurrentTime + TimeBetweenEdit;

            var embed = new EmbedBuilder { };

            embed.AddField("Temperature", $"{CPU_Average}°C")
            .AddField("Power", $"{PowerDraw}w")
            .AddField("Total Power", $"{Total_PowerDraw}kWh")
            .WithTitle(System.Environment.MachineName)
            .WithDescription("Last updated <t:" + Convert.ToString((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) + ":R>\n\n`>` CPU Information")
            .WithFooter(footer => footer.Text = "VPS")
            .WithColor(Color.Blue);

            await ChannelMessageResidesIn.ModifyMessageAsync(MessageID, m => {
                m.Embed = embed.Build();
                m.Content = "";
            });
        }

        public async static void LogMessage(string Contents)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (Controller.RetrieveCurrentTime().TotalMilliseconds < Hardware.Epoch_ProgramStart + 5000) Thread.Sleep(1000);
                LogChannel.SendMessageAsync(System.Environment.MachineName + " " + Contents);
            }).Start();
        }
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.User.Username != "downloador")
            {
                await command.RespondAsync("No", ephemeral: true);
                return;
            }

            TimeSpan CurrentTime = Controller.RetrieveCurrentTime();

            File.AppendAllText(
                Application.StartupPath + "\\Logs.txt",
                "[" + new DateTime(CurrentTime.Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "User requested wattage data reset : " + Convert.ToString(Hardware.CPU_TotalPowerDraw) + Environment.NewLine
            );
            LogMessage("[" + new DateTime(CurrentTime.Ticks).ToString("dd/MM-yyyy HH:mm:ss] ") + "User requested wattage data reset : " + Convert.ToString(Hardware.CPU_TotalPowerDraw));

            Thread.Sleep(3000);

            Hardware.CPU_TotalPowerDraw = 0;

            await command.RespondAsync("Total Power has been reset");
        }

        public async Task Client_Ready()
        {
            ChannelMessageResidesIn = _client.GetChannel(1136737633620480092) as SocketTextChannel;
            LogChannel = _client.GetChannel(1137302812330569818) as SocketTextChannel;

            if (MessageID == 0)
            {
                IUserMessage SentMessage = await ChannelMessageResidesIn.SendMessageAsync("test");

                using (StreamWriter outputFile = new StreamWriter(Application.StartupPath + "\\DiscordMessage.txt"))
                {
                    outputFile.WriteLine(Convert.ToString(SentMessage.Id));
                    outputFile.Close();
                }
            }
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;

            await _client.LoginAsync(TokenType.Bot, "Token");
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
    }
}
