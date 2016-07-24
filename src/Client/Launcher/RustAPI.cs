#define legacy
//#define alpha

/* 
   References for rust.legacy:
    Assembly-CSharp.dll    
    Assembly-CSharp-firstpass.dll
    Facepunch.ID.dll
    uLink.dll
    UnityEngine.dll

   References for rust.alpha:
    Assembly-CSharp.dll
    Assembly-CSharp-firstpass.dll
    Facepunch.Network.dll
    UnityEngine.dll
*/

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RowClient
{
    public class RustAPI
    {
#if legacy // rust.legacy API
        public static void Connect(string url, int port)
        {
            ConsoleSystem.Run("net.connect " + url + ":" + port.ToString(), false);
            //ClientConnect.Instance().DoConnect(url, port);
            //NetCull.Connect()
        }

        public static void Disconnect(string reason, bool sendReasonToServer = true)
        {
            NetCull.Disconnect();
            //NetCull.DisconnectImmediate();
        }

        public static bool IsUserConnected()
        {
            return NetCull.isClientRunning;
        }

        public static string GetServerAddress()
        {
            return PlayerPrefs.GetString("net.lasturl").Split(':')[0];
            //return PlayerClient.GetLocalPlayer().netPlayer.ipAddress;
        }

        public static int GetServerPort()
        {
            return int.Parse(PlayerPrefs.GetString("net.lasturl").Split(':')[1]);
            //return PlayerClient.GetLocalPlayer().netPlayer.port;
        }

        public static PlayerClient GetPlayer()
        {
            return PlayerClient.GetLocalPlayer();
        }

        public static string GetUserName()
        {
            if (PlayerClient.GetLocalPlayer() != null)
                return PlayerClient.GetLocalPlayer().userName;
            return null;
        }

        public static ulong GetUserID()
        {
            if (PlayerClient.GetLocalPlayer() != null)
                return PlayerClient.GetLocalPlayer().userID;
            return 0;
        }

        public static void AddLateUpdate(MonoBehaviour mb, int updateOrder, UpdateManager.OnUpdate func)
        {
            UpdateManager.AddLateUpdate(mb, updateOrder, func);
        }
#endif

        // TODO: checks for (BasePlayer.LocalPlayer != null)
#if alpha // rust.alpha API
        public static void Connect(string url, int port)
        {
            SingletonComponent<Client>.Instance.Connect(url, port);
            //return Network.Net.cl.Connect(url, port);
        }

        public static void Disconnect(string reason, bool sendReasonToServer = true)
        {
            //Network.Net.cl.Disconnect(reason, sendReasonToServer);
        }

        public static bool IsUserConnected()
        {
            return SingletonComponent<Client>.Instance != null;
            //return Network.Net.cl.IsConnected();
        }

        public static string GetServerAddress()
        {
            return Network.Net.cl.connectedAddress;
        }

        public static int GetServerPort()
        {
            return Network.Net.cl.connectedPort;
        }

        public static string GetUsedName()
        {
            return BasePlayer.LocalPlayer.name;
            //return SteamClient.localName;
        }

        public static ulong GetUsedID()
        {
            return BasePlayer.LocalPlayer.userID;
            //return SteamClient.localSteamID;
        }
#endif
    }
}
