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

        class Config
        {
            public bool enabled = false; // turn on later
            public int checkInterval = 1;
            public bool sayChat = true;
            public bool kick = true;
            public bool ban = true;
            public bool allowTP = false;
            public int sayChatSpeed = 0;
            public int kickSpeed = 0;
            public int banSpeed = 0;
            public int teleportSpeed = 0;
            public bool adminCheck = false;
            public int warnLimit = 3;
        }
        Config aconf = new Config();

        Timer takeCoordsTimer;
        RLog R = new RLog("Anticheat", RowacCore.logsFolderPath);

        internal void Initialize()
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            R.Log("[Anticheat] is loading...");

            aconf = RowacCore.LoadConfig<Config>(Path.Combine(RowacCore.rowacFolder, "anticheat.json"));
            if (aconf != null && aconf.enabled)
            {
                takeCoordsTimer = new Timer(aconf.checkInterval * 1000);
                takeCoordsTimer.Elapsed += takeCoordsEvent;
                takeCoordsTimer.Start();
                R.Log("[Anticheat] loaded!");
            }
            else R.Log("[Anticheat] disabled! Check your config.");
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
                        R.Log("No user on join: " + p.id + " " + p.ipAddress);
                        continue;
                    }

                    if (RustAPI.IsUserConnected(player))
                        R.Log("NotConnected: " + RustAPI.GetUserName(player) + " - " + RustAPI.GetUserID(player));

                    if (aconf.adminCheck && RustAPI.IsUserAdmin(player))
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
                        float speed = distance / (float)aconf.checkInterval;
                        R.Log("[Speed] " + playerName + " speed is " + speed.ToString());

                        if (!playerWarnings.ContainsKey(playerID))
                            playerWarnings[playerID] = 0;
                        int warnLevel = playerWarnings[playerID];

                        if (speed < aconf.sayChatSpeed) // decrease warning level for user
                            playerWarnings[playerID] = (warnLevel > 0 ? warnLevel - 1 : 0);
                        else if (warnLevel == aconf.warnLimit // Time to ban
                        && (speed > aconf.teleportSpeed && !aconf.allowTP)) // Not allow to TP
                        {
                            if (speed > aconf.sayChatSpeed && aconf.sayChat)
                                RustAPI.SayToChat("Moved with speed" + speed.ToString("F2"));
                            else if (speed > aconf.banSpeed && aconf.ban)
                                BanCheater(player, "Moved with speed" + speed.ToString("F2"));
                            else if (speed > aconf.kickSpeed && aconf.kick)
                            {
                                R.Log("Kick: " + playerName + ". SpeedHack. Maybe lag (Ping " + RustAPI.GetUserPing(player) + ")");
                                RustAPI.KickUser(player, NetError.Facepunch_Kick_Ban, true);
                            }
                        }

                        // Turn player back
                        if (warnLevel < aconf.warnLimit && speed > aconf.sayChatSpeed)
                        {
                            RustAPI.GetUserTransform(player).position = OldPlayerCoordsVector3;
                            if (speed > aconf.kickSpeed)
                                playerWarnings[playerID]++;
                            R.Log("Warning: " + playerName + " moved with speed " + speed.ToString("F2") + ". Warnings: " + warnLevel + ". Ping: " + RustAPI.GetUserPing(player));
                        }
                    }
                }
                catch (Exception ex) { R.LogEx("TakeCoordsLoop", ex); }
            }
        }

        private void BanCheater(object player, string reason)
        {
            var p = RustAPI.GetUser(player);
            if (p == null)
            {
                R.Log("Ban failed! No such user. [Reason: " + reason + "]");
                return;
            }

            string date = DateTime.Now.ToShortDateString();
            string time = DateTime.Now.ToShortTimeString();
            string banMsg = string.Format("Nickname: {0}, Date: {1} {2} Reason: {3} Ping: {4}",
                RustAPI.GetUserName(p), date, time, reason, RustAPI.GetUserPing(p));

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(RowacCore.rowacFolder, "bans.txt"), true))
                    writer.WriteLine(banMsg);
            }
            catch (Exception ex) { R.LogEx("BanEvent", ex); }

            R.Log("[BAN] " + banMsg);
            RustAPI.KickUser(p, NetError.Facepunch_Kick_Ban, true);
        }
    }
}