﻿using GTA;

using System.Drawing;

using LemonUI;
using LemonUI.Menus;

namespace RageCoop.Client.Menus
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    internal static class CoopMenu
    {
        public static ObjectPool MenuPool = new ObjectPool();
        public static NativeMenu Menu = new NativeMenu("RAGECOOP", "MAIN")
        {
            UseMouse = false,
            Alignment = Main.Settings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        public static NativeMenu LastMenu { get; set; } = Menu;
        #region ITEMS
        private static readonly NativeItem _usernameItem = new NativeItem("Username") { AltTitle = Main.Settings.Username };
        public static readonly NativeItem ServerIpItem = new NativeItem("Server IP") { AltTitle = Main.Settings.LastServerAddress };
        private static readonly NativeItem _serverConnectItem = new NativeItem("Connect");
        private static readonly NativeItem _aboutItem = new NativeItem("About", "~y~SOURCE~s~~n~" +
            "https://github.com/RAGECOOP~n~" +
            "~y~VERSION~s~~n~" +
            Main.CurrentVersion.Replace("_", ".")) { LeftBadge = new LemonUI.Elements.ScaledTexture("commonmenu", "shop_new_star") };
        

        #endregion

        /// <summary>
        /// Don't use it!
        /// </summary>
        static CoopMenu()
        {
            Menu.Banner.Color = Color.FromArgb(225, 0, 0, 0);
            Menu.Title.Color = Color.FromArgb(255, 165, 0);

            _usernameItem.Activated += UsernameActivated;
            ServerIpItem.Activated += ServerIpActivated;
            _serverConnectItem.Activated += (sender, item) => { Networking.DisconnectFromServer(Main.Settings.LastServerAddress); };


            Menu.Add(_usernameItem);
            Menu.Add(ServerIpItem);
            Menu.Add(_serverConnectItem);

            Menu.AddSubMenu(SettingsMenu.Menu);
            Menu.AddSubMenu(DevToolMenu.Menu);
            Menu.AddSubMenu(DebugMenu.Menu);


            MenuPool.Add(Menu);
            MenuPool.Add(SettingsMenu.Menu);
            MenuPool.Add(DevToolMenu.Menu);
            MenuPool.Add(DebugMenu.Menu);
            MenuPool.Add(DebugMenu.DiagnosticMenu);

            Menu.Add(_aboutItem);
        }

        public static void UsernameActivated(object a, System.EventArgs b)
        {
            string newUsername = Game.GetUserInput(WindowTitle.EnterMessage20, _usernameItem.AltTitle, 20);
            if (!string.IsNullOrWhiteSpace(newUsername))
            {
                Main.Settings.Username = newUsername;
                Util.SaveSettings();

                _usernameItem.AltTitle = newUsername;
            }
        }

        public static void ServerIpActivated(object a, System.EventArgs b)
        {
            string newServerIp = Game.GetUserInput(WindowTitle.EnterMessage60, ServerIpItem.AltTitle, 60);
            if (!string.IsNullOrWhiteSpace(newServerIp) && newServerIp.Contains(":"))
            {
                Main.Settings.LastServerAddress = newServerIp;
                Util.SaveSettings();

                ServerIpItem.AltTitle = newServerIp;
            }
        }

        public static void InitiateConnectionMenuSetting()
        {
            _usernameItem.Enabled = false;
            ServerIpItem.Enabled = false;
            _serverConnectItem.Enabled = false;
        }

        public static void ConnectedMenuSetting()
        {
            _serverConnectItem.Enabled = true;
            _serverConnectItem.Title = "Disconnect";
            Menu.Visible = false;
        }

        public static void DisconnectedMenuSetting()
        {
            _usernameItem.Enabled = true;
            ServerIpItem.Enabled = true;
            _serverConnectItem.Enabled = true;
            _serverConnectItem.Title = "Connect";
        }
    }
}
