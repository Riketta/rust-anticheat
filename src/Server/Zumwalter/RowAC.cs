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
    internal class RowacCore
    {
        internal static Dictionary<ulong, int> pingTimeTable = new Dictionary<ulong, int>();
        internal static Dictionary<ulong, string> userGuids = new Dictionary<ulong, string>();

        class Config
        {
            public bool enabled = true;
            public bool debug = false;
            public int minConnectionTime = 60;
            public int maxNoPingTime = 30;
            public int threadSleepTime = 5;
        }
        private static Config rconf = new Config();

        internal static string rowacFolder = @"rowac\";
        internal static string screenshotsFolderPath = rowacFolder + @"screenshots\";
        internal static string taskListsFolderPath = rowacFolder + @"tasklists\";
        internal static string logsFolderPath = rowacFolder + @"logs\";

        internal static Thread Anticheat;
        internal static Thread AnticheatRemote;
        internal static Thread ServerListener;

        internal static RLog R = new RLog("RowAC", logsFolderPath);

        internal static void Init()
        {
            try
            {
                R.Log("[RowAC] loading...");
                
                if (!Directory.Exists(rowacFolder))
                    Directory.CreateDirectory(rowacFolder);
                if (!Directory.Exists(screenshotsFolderPath))
                    Directory.CreateDirectory(screenshotsFolderPath);
                if (!Directory.Exists(taskListsFolderPath))
                    Directory.CreateDirectory(taskListsFolderPath);
                if (!Directory.Exists(logsFolderPath))
                    Directory.CreateDirectory(logsFolderPath);

                rconf = LoadConfig<Config>(Path.Combine(RowacCore.rowacFolder, "rowac.json"));
                if (rconf == null || !rconf.enabled)
                {
                    R.Log("[RowAC] disabled! Check your config.");
                    return;
                }
                
                AnticheatLocal ACLocal = new AnticheatLocal();
                Anticheat = new Thread(ACLocal.Initialize); // server-side
                Anticheat.Start();

                AnticheatRemote = new Thread(AntiCheat); // ping checking
                AnticheatRemote.Start();

                Listener server = new Listener();
                ServerListener = new Thread(server.StartListening); // client-side listener
                ServerListener.Start();

                R.Log("[RowAC] loaded! Version: " + 
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            catch (Exception ex) { R.LogEx("Init", ex); }
        }

        public static T LoadConfig<T>(string file)
        {
            try
            {
                if (File.Exists(file))
                    return RConfig<T>.ReadFromFile(file);
                else
                {
                    T def = (T)Activator.CreateInstance(typeof(T));
                    RConfig<T>.WriteToFile(def, file);
                    return def;
                }
            }
            catch (Exception ex) { R.LogEx("LoadConfig", ex); }
            return default(T);
        }

        private static void AntiCheat()
        {
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            R.Log("[RowAC] main thread initiated");

            while (true)
            {
                try
                {
                    var connections = RustAPI.GetConnections();
                    foreach (var p in connections)
                    {
                        var player = RustAPI.GetUser(p);
                        if (RustAPI.IsUserConnected(player) && RustAPI.GetUserConnectionTime(player) >= rconf.minConnectionTime)
                            CheckPlayer(player);
                    }

                    Thread.Sleep(rconf.threadSleepTime * 1000);
                }
                catch (Exception ex) { R.LogEx("LoopCrash", ex); }
            }

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
                    R.Log(string.Format("Kicked: {0}. Connection time: {1}", 
                        userName, RustAPI.GetUserConnectionTime(player)), true); // username includes ID
                    RustAPI.KickUser(player, NetError.Facepunch_Connector_AuthException, true); // No client-side anticheat
                }
#if DEBUG
                else R.Log(string.Format("NoKick: {0}. Connection time: {1}",
                        userName, RustAPI.GetUserConnectionTime(player)), true);
#endif
            }
            catch (Exception ex) { R.LogEx("CheckPlayer", ex); }
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
                R.Log(string.Format("[IsKickNeeded] Ping time: {0}; Time: {1}; Diff: {2}", 
                    pingTimeTable[ID], GetTimeInSeconds(), diff));
#endif
                if (diff > rconf.maxNoPingTime)
                    return true;
            }
            catch (Exception ex) { R.LogEx("IsKickNeeded", ex); return true; }
            return false;
        }
    }
}