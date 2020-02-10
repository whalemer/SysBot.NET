﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.WinForms
{
    public sealed partial class Main : Form
    {
        private static readonly string WorkingDirectory = Application.StartupPath;
        private static readonly string ConfigPath = Path.Combine(WorkingDirectory, "config.json");
        private readonly List<PokeBotConfig> Bots = new List<PokeBotConfig>();
        private readonly PokeTradeHubConfig Hub;

        private BotEnvironment? RunningEnvironment;

        public Main()
        {
            InitializeComponent();
            MinimumSize = Size;
            LV_Bots.Items.Clear();

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllText(ConfigPath);
                var cfg = JsonConvert.DeserializeObject<BotEnvironmentConfig>(lines);
                foreach (var c in cfg.Bots)
                    AddBot(c);
                Hub = cfg.Hub;
            }
            else
            {
                Hub = new PokeTradeHubConfig();
                Hub.CreateDefaults(WorkingDirectory);
            }

            PG_Hub.SelectedObject = Hub;

            var routines = (PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType));
            var list = routines.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
            CB_Routine.DisplayMember = nameof(ComboItem.Text);
            CB_Routine.ValueMember = nameof(ComboItem.Value);
            CB_Routine.DataSource = list;
            CB_Routine.SelectedIndex = 2; // default option
        }

        private BotEnvironmentConfig GetCurrentConfiguration()
        {
            return new BotEnvironmentConfig
            {
                Bots = Bots.ToArray(),
                Hub = Hub,
            };
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            var cfg = GetCurrentConfiguration();
            var lines = JsonConvert.SerializeObject(cfg);
            File.WriteAllText(ConfigPath, lines);
        }

        private void B_Start_Click(object sender, EventArgs e)
        {
            var cfg = GetCurrentConfiguration();
            var env = new BotEnvironment();
            B_Start.Enabled = false;
            B_Stop.Enabled = true;
            B_New.Enabled = false;
            B_Delete.Enabled = false;
            env.Start(cfg);
            RunningEnvironment = env;
        }

        private void B_Stop_Click(object sender, EventArgs e)
        {
            var env = RunningEnvironment;
            if (env == null)
                throw new ArgumentNullException(nameof(RunningEnvironment), "Should have an environment before calling stop!");
            if (!env.CanStop)
                throw new ArgumentOutOfRangeException(nameof(BotEnvironment.CanStop), "Should be running before calling stop!");
            env.Stop();
            B_Start.Enabled = true;
            B_Stop.Enabled = false;
            B_New.Enabled = true;
            B_Delete.Enabled = true;
        }

        private void B_New_Click(object sender, EventArgs e)
        {
            var cfg = CreateNewBotConfig();
            AddBot(cfg);
        }

        private void AddBot(PokeBotConfig cfg)
        {
            Bots.Add(cfg);

            var row = new[] {cfg.IP, cfg.Port.ToString(), cfg.NextRoutineType.ToString()};
            var lvi = new ListViewItem(row);
            LV_Bots.Items.Add(lvi);

            B_Start.Enabled = true;
            B_Delete.Enabled = true;
            System.Media.SystemSounds.Asterisk.Play();
        }

        private void B_Delete_Click(object sender, EventArgs e)
        {
            var indexes = LV_Bots.SelectedIndices;
            var items = indexes.Cast<int>().OrderByDescending(z => z);
            foreach (var item in items)
            {
                Bots.RemoveAt(item);
                LV_Bots.Items.RemoveAt(item);
            }

            if (Bots.Count == 0)
            {
                B_Start.Enabled = false;
                B_Delete.Enabled = false;
            }

            System.Media.SystemSounds.Asterisk.Play();
        }

        private PokeBotConfig CreateNewBotConfig()
        {
            var type = (PokeRoutineType)WinFormsUtil.GetIndex(CB_Routine);
            var ip = TB_IP.Text;
            var port = (int)NUD_Port.Value;

            var cfg = SwitchBotConfig.GetConfig<PokeBotConfig>(ip, port);
            cfg.NextRoutineType = type;
            return cfg;
        }
    }
}