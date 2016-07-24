using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using UnityEngine;

namespace RowAC
{
    internal class AnticheatLocal
    {
        internal Dictionary<ulong, Vector3> playerCoordinates = new Dictionary<ulong, Vector3>();
        internal Dictionary<ulong, int> playerWarnings = new Dictionary<ulong, int>();

        private bool enabled = false;
        private int checkInterval = 0;
        private bool sayChat = false;
        private bool kick = false;
        private bool ban = false;
        private bool allowTP = false;
        private int sayChatSpeed = 0;
        private int kickSpeed = 0;
        private int banSpeed = 0;
        private int teleportSpeed = 0;
        private bool adminCheck = false;
        private int warnLimit = 3;

        private Timer takeCoordsTimer;

        private string consolePrefix = "[AC]";

        internal void Initialize()
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            Log("RowAC is loading...");

            ConfigInit();
            if (enabled)
            {
                takeCoordsTimer = new Timer(checkInterval * 1000);
                takeCoordsTimer.Elapsed += takeCoordsEvent;
                takeCoordsTimer.Start();
                Log("RowAC loaded!");
            }
            else Log("RowAC disabled! Check your config.");
        }

        private int GetIntSetting(string Section, string Name)
        {
            string Value = RowAnticheat.ini.Read(Name, Section);
            int INT = 0;
            if (int.TryParse(Value, out INT))
                return INT;
            return int.MinValue;
        }

        private bool GetBoolSetting(string Section, string Name)
        {
            return RowAnticheat.ini.Read(Name, Section).ToLower() == "true";
        }

        private void Log(string Msg)
        {
            RowAnticheat.Log(consolePrefix + " " + Msg);
        }

        private void takeCoordsEvent(object x, ElapsedEventArgs y)
        {
            var connections = RustAPI.GetConnections();
            foreach (var p in connections)
            {
                try
                {
                    var player = RustAPI.GetUser(p);
                    if (player == null)
                    {
                        Log("No user on join: " + p.id + " " + p.ipAddress);
                        continue;
                    }

                    if (RustAPI.IsUserConnected(player))
                        Log("NotConnected: " + RustAPI.GetUserName(player) + " - " + RustAPI.GetUserID(player));

                    if (adminCheck && RustAPI.IsUserAdmin(player))
                        continue;

                    Vector3 CurrentPosition = RustAPI.GetUserTransform(player).position;
                    Vector2 CurrentPlayerCoords = new Vector2(CurrentPosition.x, CurrentPosition.z); // Ignore Y

                    ulong playerID = RustAPI.GetUserID(player);
                    string playerName = RustAPI.GetUserName(player);

                    if (!playerCoordinates.ContainsKey(playerID))
                    {
                        playerCoordinates[playerID] = CurrentPosition;
                        continue;
                    }
                    Vector3 OldPlayerCoordsVector3 = playerCoordinates[playerID];
                    playerCoordinates[playerID] = CurrentPosition;

                    Vector2 OldPlayerCoords = new Vector2(OldPlayerCoordsVector3.x, OldPlayerCoordsVector3.z);

#if DEBUG           // TODO: fix
                    /*
                        Log("=== Pos Control ===");
                        Log(CurrentPosition.x + " " + CurrentPosition.z);
                        Log(Player.GetNetworkPosition().x + " " + Player.GetNetworkPosition().z);
                        Log(Player.GetPositionForChecks().x + " " + Player.GetPositionForChecks().z);
                        Log("=== =========== ===");
                    */
#endif
                    if (OldPlayerCoords != Vector2.zero && OldPlayerCoords != CurrentPlayerCoords)
                    {
                        float distance = Math.Abs(Vector2.Distance(OldPlayerCoords, CurrentPlayerCoords));
                        Log(playerName + " speed is " + (distance / (float)checkInterval).ToString());

                        if (!playerWarnings.ContainsKey(playerID))
                            playerWarnings[playerID] = 0;
                        int warnLevel = playerWarnings[playerID];

                        float speed = distance / checkInterval;
                        if (speed < sayChatSpeed) // decrease warning level for user
                            playerWarnings[playerID] = (warnLevel > 0 ? warnLevel - 1 : 0);
                        else if (warnLevel == warnLimit // Time to ban
                        && (speed > teleportSpeed && !allowTP)) // Not allow to TP
                        {
                            if (speed > sayChatSpeed && sayChat)
                                RustAPI.SayToChat("Moved with speed" + speed.ToString("F2"));
                            else if (speed > banSpeed && ban)
                                BanCheater(player, "Moved with speed" + speed.ToString("F2"));
                            else if (speed > kickSpeed && kick)
                            {
                                Log("Kick: " + playerName + ". SpeedHack. Maybe lag (Ping " + RustAPI.GetUserPing(player) + ")");
                                RustAPI.KickUser(player, NetError.Facepunch_Kick_Ban, true);
                            }
                        }

                        // Turn player back
                        if (warnLevel < warnLimit && speed > sayChatSpeed)
                        {
                            RustAPI.GetUserTransform(player).position = OldPlayerCoordsVector3;
                            if (speed > kickSpeed)
                                playerWarnings[playerID]++;
                            Log("Warning: " + playerName + " moved with speed " + speed.ToString("F2") + ". Warnings: " + warnLevel + ". Ping: " + RustAPI.GetUserPing(player));
                        }
                    }
                }
                catch (Exception ex) { Log(ex.ToString()); }
            }
        }

        private void BanCheater(object player, string reason)
        {
            var p = RustAPI.GetUser(player);
            if (p == null)
            {
                Log("Ban failed! No user. [Reason: " + reason + "]");
                return;
            }

            string date = DateTime.Now.ToShortDateString();
            string time = DateTime.Now.ToShortTimeString();
            string banMsg = string.Format("Nickname: {0}, Date: {1} {2} Reason: {3} Ping: {4}",
                RustAPI.GetUserName(p), date, time, reason, RustAPI.GetUserPing(p));

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(RowAnticheat.anticheatLogFolder, "Bans.txt"), true))
                    writer.WriteLine(banMsg);
            }
            catch (Exception ex) { Log(ex.ToString()); }

            Log("BAN: " + banMsg);
            RustAPI.KickUser(p, NetError.Facepunch_Kick_Ban, true);
        }
        private void ConfigInit()
        {
            try
            {
                enabled = GetBoolSetting("Anticheat", "Enable");
                checkInterval = GetIntSetting("Anticheat", "Timer");
                sayChat = GetBoolSetting("Anticheat", "Chat");
                kick = GetBoolSetting("Anticheat", "Kick");
                ban = GetBoolSetting("Anticheat", "Ban");
                allowTP = GetBoolSetting("Anticheat", "Teleport");
                sayChatSpeed = GetIntSetting("Anticheat", "ChatSpeed");
                kickSpeed = GetIntSetting("Anticheat", "KickSpeed");
                banSpeed = GetIntSetting("Anticheat", "BanSpeed");
                teleportSpeed = GetIntSetting("Anticheat", "TeleportSpeed");
                adminCheck = GetBoolSetting("Anticheat", "AdminCheck");
                warnLimit = GetIntSetting("Anticheat", "WarnLimit");
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }
    }
}