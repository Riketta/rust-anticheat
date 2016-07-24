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
    
*/

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RowAC
{
    public class RustAPI
    {
#if legacy // rust.legacy API
        public static uLink.NetworkPlayer[] GetConnections()
        {
            return NetCull.connections;
        }

        public static NetUser GetUser(uLink.NetworkPlayer player)
        {
            object localData = player.GetLocalData();
            return GetUser(localData);
        }

        public static NetUser GetUser(object player)
        {
            if (player is NetUser) return (NetUser)player;
            return null;
        }

        public static string GetUserName(NetUser player)
        {
            return player.playerClient.name;
            //return player.playerClient.userName;
        }

        public static ulong GetUserID(NetUser player)
        {
            return player.playerClient.userID;
        }

        public static int GetUserConnectionTime(NetUser player)
        {
            return player.SecondsConnected();
        }

        public static int GetUserPing(NetUser player)
        {
            return player.networkPlayer.averagePing;
            //return player.networkPlayer.lastPing;
        }

        public static void KickUser(NetUser player, NetError reason, bool notify)
        {
            // reson = NetError.Facepunch_Kick_Violation
            player.Kick(reason, notify);
            //player.playerClient.netUser.Kick(reason, notify);
        }

        public static void BanUser(NetUser player)
        {
            player.Ban();
            //player.playerClient.netUser.Ban();
        }

        public static bool IsUserConnected(NetUser player)
        {
            return NetUser.IsUserConnected(player.userID);
            //return player.connected;
            //return player.isConnectedClient;
        }

        public static bool IsUserAdmin(NetUser player)
        {
            return player.admin;
            //return player.CanAdmin(); // returns this.admin
        }

        public static Transform GetUserTransform(NetUser player)
        {
            return player.playerClient.transform;
        }

        internal static void SayToChat(string message)
        {
            ConsoleNetworker.Broadcast("chat.add \"[RowAC]\" " + message);
        }

        public static NetUser Find(uLink.NetworkPlayer player)
        {
            return NetUser.Find(player);
            //return (player.GetLocalData() as NetUser);
        }

        public static NetUser FindByUserID(ulong userid)
        {
            return NetUser.FindByUserID(userid);
        }
#endif
    }
}
