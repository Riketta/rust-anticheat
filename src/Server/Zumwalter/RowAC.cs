using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RowAC
{
    internal class RowAnticheat
    {

        internal static Dictionary<ulong, int> pingTimeTable = new Dictionary<ulong, int>();
        internal static Dictionary<ulong, string> userGuids = new Dictionary<ulong, string>();

        private static StreamWriter writer;

        private static bool enabled = true;
        private static bool debug = false;
        private static int minConnectionTime = 60;
        private static int maxNoPingTime = 30;
        private static int threadSleepTime = 5;

        internal static string anticheatLogFolder = @"ACLog\";
        internal static string screenshotsFolderPath = anticheatLogFolder + @"Screenshots\";
        internal static string taskListsFolderPath = anticheatLogFolder + @"Tasklists\";
        internal static string logsFolderPath = anticheatLogFolder + @"Logs\";
        internal static string configPath = "";

        internal static RowAC.IniFile ini = null;

        internal static Thread Anticheat;
        internal static Thread AnticheatRemote;
        internal static Thread ServerListener;

        internal static void Init()
        {
            try
            {
                Log("[RowAC] loading...");
                
                if (!Directory.Exists(anticheatLogFolder))
                    Directory.CreateDirectory(anticheatLogFolder);
                if (!Directory.Exists(screenshotsFolderPath))
                    Directory.CreateDirectory(screenshotsFolderPath);
                if (!Directory.Exists(taskListsFolderPath))
                    Directory.CreateDirectory(taskListsFolderPath);
                if (!Directory.Exists(logsFolderPath))
                    Directory.CreateDirectory(logsFolderPath);

                writer = new StreamWriter(Path.Combine(logsFolderPath, "rowac_" + DateTime.Now.ToString("dd_MM_yyyy") + ".txt"), true);
                writer.AutoFlush = true;

                LoadConfig();
                if (!enabled)
                {
                    Log("Anticheat disabled!");
                    return;
                }

                AnticheatLocal ACLocal = new AnticheatLocal();
                Anticheat = new Thread(ACLocal.Initialize); // server-side
                Anticheat.Start();

                AnticheatRemote = new Thread(AntiCheat); // ping checking
                AnticheatRemote.Start();

                // client-side listener
                Listener server = new Listener();
                ServerListener = new Thread(server.StartListening);
                ServerListener.Start();

                UnityEngine.Debug.Log("RowAC loaded! Version: " + 
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        private static void LoadConfig()
        {
            Log("Config Loading...");
            try
            {
                configPath = Path.Combine(anticheatLogFolder, "RowAC.ini");
                Log("ConfigPath: " + configPath);
                ini = new RowAC.IniFile(configPath);

                if (!File.Exists(configPath))
                {
                    Log("RowAC.ini does not exist! Default created");

                    ini.Write("Enabled", (enabled ? 1 : 0).ToString(), "RowAC");
                    ini.Write("Debug", (debug ? 1 : 0).ToString(), "RowAC");
                    ini.Write("MinConnectionTime", minConnectionTime.ToString(), "RowAC");
                    ini.Write("MaxNoPingTime", maxNoPingTime.ToString(), "RowAC");
                    ini.Write("ThreadSleepTime", threadSleepTime.ToString(), "RowAC");
                }
                else
                {
                    enabled = (int.Parse(ini.Read("Enabled", "RowAC")) == 1 ? true : false);
                    debug = (int.Parse(ini.Read("Debug", "RowAC")) == 1 ? true : false);
                    minConnectionTime = int.Parse(ini.Read("MinConnectionTime", "RowAC"));
                    maxNoPingTime = int.Parse(ini.Read("MaxNoPingTime", "RowAC"));
                    threadSleepTime = int.Parse(ini.Read("ThreadSleepTime", "RowAC"));
                }

                Log("[Config] ===============");
                Log("MinConnectTime: " + minConnectionTime);
                Log("MaxNoPingTime: " + maxNoPingTime);
                Log("ThreadSleepTime: " + threadSleepTime);
                Log("[Config] =========== END");
            }
            catch (Exception ex) { Log("[Config] " + ex); }
            Log("Config Loaded!", true);
        }

        private static void AntiCheat()
        {
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            Log("RowAC thread inited");
            while (true)
            {
                try
                {
                    var connections = RustAPI.GetConnections();
                    foreach (var p in connections)
                    {
                        var player = RustAPI.GetUser(p);
                        if (RustAPI.IsUserConnected(player) && RustAPI.GetUserConnectionTime(player) >= minConnectionTime)
                            CheckPlayer(player);
                    }

                    Thread.Sleep(threadSleepTime * 1000);
                }
                catch (Exception ex) { Log("[LOOP_CRASH] " + ex); }
            }

        }

        internal static void Log(string message, bool alert = true)
        {
            try
            {
                message = "[RowAC][" + DateTime.Now.ToString("HH:mm:ss") + "] " + message;
                if (alert)
                {
                    UnityEngine.Debug.Log(message);
                    Console.WriteLine(message);
                }
                lock (writer)
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
            catch { }
        }

        internal static int GetTimeInSeconds()
        {
            DateTime centuryBegin = new DateTime(2001, 1, 1);
            DateTime currentDate = DateTime.Now;

            long elapsedTicks = currentDate.Ticks - centuryBegin.Ticks;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            return (int)elapsedSpan.TotalSeconds;
        }

        internal static void CheckPlayer(NetUser player)
        {
            try
            {
                ulong userID = RustAPI.GetUserID(player);
                string userName = RustAPI.GetUserName(player);
                if (IsKickNeeded(userID))
                {
                    Log(string.Format("Kicked: {0}. Connection time: {1}", 
                        userName, RustAPI.GetUserConnectionTime(player)), true); // username includes ID
                    RustAPI.KickUser(player, NetError.Facepunch_Connector_AuthException, true); // No client-side anticheat
                }
#if DEBUG
                else Log(string.Format("NoKick: {0}. Connection time: {1}",
                        userName, RustAPI.GetUserConnectionTime(player)), true);
#endif
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        private static bool IsKickNeeded(ulong ID)
        {
            try
            {
                if (!pingTimeTable.ContainsKey(ID) || !userGuids.ContainsKey(ID)
                        || userGuids[ID] == "" || userGuids[ID] == "null")
                    return true;

                int diff = GetTimeInSeconds() - pingTimeTable[ID];
#if DEBUG
                Log(string.Format("[IsKickNeeded] Ping time: {0}; Time: {1}; Diff: {2}", 
                    pingTimeTable[ID], GetTimeInSeconds(), diff));

#endif
                if (diff > maxNoPingTime)
                    return true;
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return false;
        }
    }
}