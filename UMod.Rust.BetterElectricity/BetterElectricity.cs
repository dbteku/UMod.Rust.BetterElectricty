﻿using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Better Electricity", "dbteku", "1.0.4")]
    [Description("Allows more control over electricity.")]
    public class BetterElectricity : RustPlugin
    {
        private const string ADMIN_PERM = "betterelectricity.admin";

        private static ElectricityConfig config;

        #region Config

        private class ElectricityConfig
        {
            public SolarPanelConfig SolarPanelConfig { get; set; }
            public LargeBatteryConfig LargeBatteryConfig { get; set; }
            public SmallBatteryConfig SmallBatteryConfig { get; set; }

            public ElectricityConfig()
            {
                SolarPanelConfig = new SolarPanelConfig();
                LargeBatteryConfig = new LargeBatteryConfig();
                SmallBatteryConfig = new SmallBatteryConfig();
            }
        }

        private class SolarPanelConfig
        {
            public int MaxOutput { get; set; }

            public SolarPanelConfig()
            {
                MaxOutput = 100;
            }
        }

        private class LargeBatteryConfig
        {
            public int MaxOutput { get; set; }
            public float Efficiency { get; set; }
            public int MaxCapacitySeconds { get; set; }

            public LargeBatteryConfig()
            {
                MaxOutput = 100;
                Efficiency = 1.0f;
                MaxCapacitySeconds = 14400;
            }
        }

        private class SmallBatteryConfig
        {
            public int MaxOutput { get; set; }
            public float Efficiency { get; set; }
            public int MaxCapacitySeconds { get; set; }

            public SmallBatteryConfig()
            {
                MaxOutput = 50;
                Efficiency = 1.0f;
                MaxCapacitySeconds = 1800;
            }
        }


        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ElectricityConfig>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning(BetterElectricityLang.GetMessage(BetterElectricityLang.CONFIG_CREATE_OR_FIX));
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private ElectricityConfig GetDefaultConfig()
        {
            return new ElectricityConfig();
        }

        #endregion


        #region Oxide Hooks

        private void OnServerInitialized()
        {
            ChangeSolarPanels();
            ChangeBatteries();
        }

        private void Unload()
        {
            RevertSolarPanels();
            RevertBatteries();
        }

        private void OnEntitySpawned(BaseNetworkable networkObject)
        {
            ElectricBattery battery = networkObject.GetComponent<ElectricBattery>();
            if(battery != null)
            {
                AdjustBattery(battery);
            }
            SolarPanel panel = networkObject.GetComponent<SolarPanel>();
            if(panel != null)
            {
                AdjustSolarPanel(panel);
            }
        }

        #endregion

        #region lang

        private class BetterElectricityLang
        {
            public static Dictionary<string, string> lang = new Dictionary<string, string>();
            public static string FIND_SOLAR_PANELS_ADJUST = "FindSolarPanelsAdjust";
            public static string FIND_BATTERIES_ADJUST = "FindBatteriesAdjust";
            public static string FIND_SOLAR_PANELS_REVERT = "FindSolarPanelsRevert";
            public static string FIND_BATTERIES_REVERT = "FindBatteriesRevert";
            public static string HELP_PLAYER_MENU = "HelpMenu";
            public static string BE_RELOAD_HELP = "BeReloadHelp";
            public static string NO_PERMISSION = "NoPermission";
            public static string CONFIG_CREATE_OR_FIX = "ConfigUpdateOrFix";

            public static string GetMessage(string id)
            {
                string message = "";
                lang.TryGetValue(id, out message);
                if(message == null)
                {
                    message = "";
                }
                return message;
            }

        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [BetterElectricityLang.FIND_SOLAR_PANELS_ADJUST] = "Finding and adjusting all Solar Panels. (This may take some time)",
                [BetterElectricityLang.FIND_BATTERIES_ADJUST] = "Finding and adjusting all Batteries. (This may take some time)",
                [BetterElectricityLang.FIND_SOLAR_PANELS_REVERT] = "Finding and reverting all Solar Panels. (This may take some time)",
                [BetterElectricityLang.FIND_BATTERIES_REVERT] = "Finding and reverting all Batteries. (This may take some time)",
                [BetterElectricityLang.HELP_PLAYER_MENU] = "====== Player Commands ======",
                [BetterElectricityLang.BE_RELOAD_HELP] = "/belectric reload => Reloads the config.",
                [BetterElectricityLang.NO_PERMISSION] = "No Permission!",
                [BetterElectricityLang.CONFIG_CREATE_OR_FIX] = "Configuration file is corrupt (or doesn't exists), creating new one!",
            }, this);
            BetterElectricityLang.lang = lang.GetMessages("en", this);
        }

        #endregion

        #region Utils

        private void Reload()
        {
            RevertSolarPanels();
            RevertBatteries();
            LoadConfig();
            ChangeSolarPanels();
            ChangeBatteries();
        }

        private bool HasPermission(BasePlayer player, string perm)
        {
            return player.IsAdmin || permission.UserHasPermission(player.UserIDString, perm);
        }

        #endregion

        #region Core

        private void ChangeSolarPanels()
        {
            // Heavy Initial Load
            Puts(BetterElectricityLang.GetMessage(BetterElectricityLang.FIND_SOLAR_PANELS_ADJUST));
            foreach (SolarPanel panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
            {
                AdjustSolarPanel(panel);
            }
        }

        private void ChangeBatteries()
        {
            // Heavy Initial Load
            Puts(BetterElectricityLang.GetMessage(BetterElectricityLang.FIND_BATTERIES_ADJUST));
            foreach (ElectricBattery battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
            {
                AdjustBattery(battery);
            }
        }

        private void RevertBatteries()
        {
            Puts(BetterElectricityLang.GetMessage(BetterElectricityLang.FIND_BATTERIES_REVERT));
            foreach (ElectricBattery battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
            {
                RevertBattery(battery);
            }
        }

        private void RevertSolarPanels()
        {
            Puts(BetterElectricityLang.GetMessage(BetterElectricityLang.FIND_SOLAR_PANELS_REVERT));
            foreach (SolarPanel panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
            {
                RevertSolarPanel(panel);
            }
        }

        private void AdjustBattery(ElectricBattery battery)
        {
            if(battery.maxOutput == 100)
            {
                // Large Battery
                battery.maxOutput = config.LargeBatteryConfig.MaxOutput;
                battery.maxCapactiySeconds = config.LargeBatteryConfig.MaxCapacitySeconds;
                battery.chargeRatio = config.LargeBatteryConfig.Efficiency;
            }
            else if(battery.maxOutput == 10)
            {
                // Small Battery.
                battery.maxOutput = config.SmallBatteryConfig.MaxOutput;
                battery.maxCapactiySeconds = config.SmallBatteryConfig.MaxCapacitySeconds;
                battery.chargeRatio = config.SmallBatteryConfig.Efficiency;
            }
        }

        private void AdjustSolarPanel(SolarPanel panel)
        {
            panel.maximalPowerOutput = config.SolarPanelConfig.MaxOutput;
        }

        private void RevertBattery(ElectricBattery battery)
        {

            if (battery.maxOutput == config.LargeBatteryConfig.MaxOutput)
            {
                // Large battery;
                battery.maxCapactiySeconds = 14400;
                battery.chargeRatio = 0.8f;
                battery.maxOutput = 100;
            }
            else if(battery.maxOutput == config.SmallBatteryConfig.MaxOutput)
            {
                battery.maxCapactiySeconds = 900;
                battery.chargeRatio = 0.8f;
                battery.maxOutput = 10;
            }
        }

        private void RevertSolarPanel(SolarPanel panel)
        {
            panel.maximalPowerOutput = 20;
        }

        #endregion

        #region Commands
        [ChatCommand("belectric")]
        void OnElectricityCommand(BasePlayer player, string command, string[] args)
        {
            if(args.Length == 0)
            {
                SendReply(player, BetterElectricityLang.GetMessage(BetterElectricityLang.HELP_PLAYER_MENU));
                SendReply(player, BetterElectricityLang.GetMessage(BetterElectricityLang.BE_RELOAD_HELP));
            }
            else if(args.Length == 1)
            {
                if (args[0].ToLower() == "reload")
                {
                    if (HasPermission(player, ADMIN_PERM))
                    {
                        Reload();
                    }
                    else
                    {
                        SendReply(player, BetterElectricityLang.GetMessage(BetterElectricityLang.NO_PERMISSION));
                    }
                }
                else
                {
                    SendReply(player, BetterElectricityLang.GetMessage(BetterElectricityLang.HELP_PLAYER_MENU));
                    SendReply(player, BetterElectricityLang.GetMessage(BetterElectricityLang.BE_RELOAD_HELP));
                }
            }
        }
        #endregion

    }
}
