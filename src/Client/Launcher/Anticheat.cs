//#define DEMO

using System;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;
//using Microsoft.Win32; // for guid throw windows id
using System.Net.NetworkInformation; // for guid throw mac address

namespace RowClient
{
    public class RGuard
    {
        static bool loaded = false;
        private static string assemblyLocation;
        private static string logFile = "rowac_" + DateTime.Now.ToString("dd_MM_yyyy") + ".txt";
        private static StreamWriter writer = new StreamWriter(logFile, true);

        private string serverHost = "127.0.0.1:28015";
        private int anticheatPort = 28165;
        private Thread ACThread;

        public void Main()
        {
            if (loaded) return; // prevent multiple loading

            Log("[RGuard] loading...");
            assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            logFile = Path.Combine(assemblyLocation, logFile);

            ACThread = new Thread(AnticheatLoop);
            ACThread.Start();
            Log("[RGuard] loaded! Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            loaded = true;
        }

        private void ErrorLog(Exception ex)
        {
            string msg = "[=EX=] " + ex + Environment.NewLine + Environment.NewLine;
            Log(msg);
        }

        enum Header
        {
            ID = 0,
            Ping = 1,
            Guid = 2,
            Tasklist = 3,
            Screenshot = 4
        }

        private void SendTasklist()
        {
            string processList = "";
            try
            {
#if DEMO
                int processCount = 0;
#endif
                Process[] processes = Process.GetProcesses();
                foreach (System.Diagnostics.Process userProcess in processes)
                {
                    string line = "";
                    // catches cause some processes may be started by system and we can't access it's info
                    try { line += userProcess.ProcessName + " - "; } catch { }
                    try { line += userProcess.MainWindowTitle; } catch { }
                    processList += line + Environment.NewLine;

#if DEMO
                    processCount++;
                    if (processCount == 5)
                    {
                        ProcessList += "DEMO: Only 5 processes!";
                        break;
                    }
#endif
                }

                string GET = "&" + (uint)Header.Tasklist + "=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(processList));
                SendCommand(GET);
#if DEBUG
                Log("Tasklist sended");
#endif
            }
            catch (Exception ex) { Log("==[SendTasklistEX]" + ex); }
        }


        public static byte[] screenArray = null;
        public static bool screenNeeded = false;
        public void ScreenHook(float delta)
        {
            if (!screenNeeded)
                return;
#if DEBUG
            Log("[LateUpdate] ScreenHook event");
#endif
            Texture2D screen = new Texture2D(Screen.width, Screen.height);
            screen.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screen.Apply();

            byte[] jpg = screen.EncodeToJPG(20); // Text still readable on 20

            screenArray = jpg;
            screenNeeded = false;
        }

        private void SendScreenshot()
        {
            try
            {
                if (screenArray == null)
                {
                    screenNeeded = true;
                    return;
                }

                string screenshot = "&" + (uint)Header.Screenshot + "=" + Convert.ToBase64String(screenArray);
                screenArray = null;
                SendCommand(screenshot);
#if DEBUG
                Log("Screenshot sended");
#endif
            }
            catch (Exception ex) { Log("==[SendScreenshotEX]" + ex); }
        }

        private void SendPing()
        {
            SendCommand("");
        }

        // TODO: send in other thread
        private void SendCommand(string GET)
        {
            try
            {
                string ping = new System.Random().Next() + "|" + Guid() + "|" + DateTime.Now.ToShortTimeString() + "|" + new System.Random().Next();

                string checkGET = (uint)Header.ID + "=" + RustAPI.GetUserID()
                    + "&" + (uint)Header.Guid + "=" + Guid()
                    + "&" + (uint)Header.Ping + "=" + ping
                    + GET;

#if !DEBUG
                checkGET = Rijndael.Encrypt(checkGET, Rijndael.pubKey);
#else
                Log("[SendRequest] " + checkGET);
#endif

                var client = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(IPAddress.Parse(serverHost.Split(':')[0]), anticheatPort);

                byte[] byteData = Encoding.ASCII.GetBytes(checkGET);
                client.Send(byteData, 0, byteData.Length, 0);
                client.Close();

#if DEBUG
                Log("Send event. Request length: " + byteData.Length);
#endif
            }
            catch (Exception ex) { ErrorLog(ex); }
        }

        public static void Log(string text)
        {
            try
            {
                text = "[" + DateTime.Now + "] " + text;
                UnityEngine.Debug.Log(text);
                writer.WriteLine(text);
                writer.Flush();
            }
            catch { }
        }

        private bool IsRustFocused() // Для скриншотилки
        {
            int chars = 256;
            StringBuilder buff = new StringBuilder(chars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, chars) > 0)
            {
#if DEBUG
                Log("Focus on: " + buff.ToString());
                Log("Focus handle: " + handle.ToString());
                Log("Rust window: " + Process.GetCurrentProcess().MainWindowHandle);
#endif
                return handle == Process.GetCurrentProcess().MainWindowHandle;
            }
            return false;
        }

        private void AnticheatLoop()
        {
            try
            {
#if DEBUG
                Log("Anticheat loop started");
#endif
#if DEMO
                int screenshotCount = 0;
#endif

#if DEBUG
                Log("Adding [LateUpdate] for screenshots");
#endif
                RustAPI.AddLateUpdate(RustAPI.GetPlayer(), 0, ScreenHook);
#if DEBUG
                Log("[LateUpdate] added");
#endif

                int secondCounter = 0;
                while (true)
                {
                    if (RustAPI.IsUserConnected() && RustAPI.GetUserID() != 0)
                    {
                        try { serverHost = string.Format("{0}:{1}", RustAPI.GetServerAddress(), RustAPI.GetServerPort()); }
                        catch (Exception ex) { Log("[NoServerInLoop] " + ex.ToString()); }
                    }
                    else { Thread.Sleep(1000); continue; }



                    try
                    {
#if DEMO
                        if (screenshotCount == 3)
                            RustAPI.Disconnect("Trial: 3 screenshots limit");
#endif
#if !DEBUG
                        if (secondCounter % 300 == 0 || screenArray != null) // screenshot every ~5 min
#else
                        if (secondCounter % 60 == 0 || screenArray != null) // for debugging send more often
#endif
                        {
                            SendScreenshot();
#if DEMO
                            if (screenArray != null) screenshotCount++;
#endif
                        }

                        if (secondCounter % 10 == 0) // send ping
                            SendPing();

                        if (secondCounter % 180 == 0) // send tasklist every ~3 min
                            SendTasklist();

                        secondCounter++;
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        ErrorLog(ex);
                        SendPing(); // try to ping anyway
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed to init anticheat!");
                ErrorLog(ex);
            }
        }

        private string Guid()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up).Select(nic => nic.GetPhysicalAddress().ToString()).FirstOrDefault().ToString();
            //return (string)Registry.GetValue(
            //    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\", "ProductId", "null");
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}