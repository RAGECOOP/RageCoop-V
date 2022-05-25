﻿using System.Drawing;
using System;
using System.Windows.Forms;
using GTA;
using LemonUI.Menus;

namespace RageCoop.Client.Menus.Sub
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class SettingsMenu
    {
        public NativeMenu MainMenu = new NativeMenu("RAGECOOP", "Settings", "Go to the settings")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };

        private readonly NativeCheckboxItem _disableTrafficItem = new NativeCheckboxItem("Disable Traffic (NPCs/Vehicles)", "Local traffic only", Main.Settings.DisableTraffic);
        private readonly NativeCheckboxItem _flipMenuItem = new NativeCheckboxItem("Flip menu", Main.Settings.FlipMenu);
        private readonly NativeCheckboxItem _disablePauseAlt = new NativeCheckboxItem("Disable Alternate Pause", "Don't freeze game time when Esc pressed", Main.Settings.DisableTraffic);

        private readonly NativeCheckboxItem _showNetworkInfoItem = new NativeCheckboxItem("Show Network Info", Networking.ShowNetworkInfo);
        
        private static NativeItem _menuKey = new NativeItem("Menu Key","The key to open menu", Main.Settings.MenuKey.ToString());
        private static NativeItem _passengerKey = new NativeItem("Passenger Key", "The key to enter a vehicle as passenger", Main.Settings.PassengerKey.ToString());

        /// <summary>
        /// Don't use it!
        /// </summary>
        public SettingsMenu()
        {
            MainMenu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            MainMenu.Title.Color = Color.FromArgb(255, 165, 0);

            _disableTrafficItem.CheckboxChanged += DisableTrafficCheckboxChanged;
            _disablePauseAlt.CheckboxChanged+=_disablePauseAlt_CheckboxChanged;
            _flipMenuItem.CheckboxChanged += FlipMenuCheckboxChanged;
            _showNetworkInfoItem.CheckboxChanged += ShowNetworkInfoCheckboxChanged;
            _menuKey.Activated+=ChaneMenuKey;
            _passengerKey.Activated+=ChangePassengerKey;

            MainMenu.Add(_disableTrafficItem);
            MainMenu.Add(_disablePauseAlt);
            MainMenu.Add(_flipMenuItem);
            MainMenu.Add(_showNetworkInfoItem);
            MainMenu.Add(_menuKey);
            MainMenu.Add(_passengerKey);
        }

        private void _disablePauseAlt_CheckboxChanged(object sender, EventArgs e)
        {
            Main.Settings.DisableAlternatePause=_disablePauseAlt.Checked;
            Util.SaveSettings();
        }

        private void ChaneMenuKey(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.MenuKey =(Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.MenuKey.ToString(), 20));
                _menuKey.AltTitle=Main.Settings.MenuKey.ToString();
                Util.SaveSettings();
            }
            catch { }
        }

        private void ChangePassengerKey(object sender, EventArgs e)
        {
            try
            {
                Main.Settings.PassengerKey =(Keys)Enum.Parse(
                    typeof(Keys),
                    Game.GetUserInput(WindowTitle.EnterMessage20,
                    Main.Settings.PassengerKey.ToString(), 20));
                _passengerKey.AltTitle=Main.Settings.PassengerKey.ToString();
                Util.SaveSettings();
            }
            catch { }
        }

        public void DisableTrafficCheckboxChanged(object a, System.EventArgs b)
        {
            Main.Settings.DisableTraffic = _disableTrafficItem.Checked;
            Util.SaveSettings() ;
        }

        public void FlipMenuCheckboxChanged(object a, System.EventArgs b)
        {
            Main.MainMenu.MainMenu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;

            MainMenu.Alignment = _flipMenuItem.Checked ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left;
            Main.Settings.FlipMenu = _flipMenuItem.Checked;
            Util.SaveSettings();
        }

        public void ShowNetworkInfoCheckboxChanged(object a, System.EventArgs b)
        {
            Networking.ShowNetworkInfo = _showNetworkInfoItem.Checked;

            if (!Networking.ShowNetworkInfo)
            {
                Networking.BytesReceived = 0;
                Networking.BytesSend = 0;
            }
        }
    }
}
