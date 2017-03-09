using System;
using System.Collections.Generic;
using System.Linq;
using DarkRift;

namespace Room_Plugin
{
    class Room
    {
        private int MaxPlayers;
        public String Name;
        public List<ushort> Players = new List<ushort>();

        /// <summary>
        ///     Sets the max number of players.
        /// </summary>
        /// <param name="max"></param>
        public void SetMaxPlayers(int max)
        {
            MaxPlayers = max;
        }

        /// <summary>
        ///     Gets the max number of players.
        /// </summary>
        /// <returns>The max number of players allowed in the room.</returns>
        public int GetMaxPlayers()
        {
            return MaxPlayers;
        }

        /// <summary>
        ///     Sets the room name.
        /// </summary>
        /// <param name="name"></param>
        public void SetName(String name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets the room name.
        /// </summary>
        /// <returns>The name of the room.</returns>
        public String GetName()
        {
            return Name;
        }

        /// <summary>
        ///     Adds a player to the room if the number of players will not exceed the max player count.
        /// </summary>
        /// <param name="senderID"></param>
        public void AddPlayer(ushort senderID)
        {
            if(Players.Count() >= MaxPlayers)
            {
                Interface.Log("Too many Players");
            }
            else
            {
                Players.Add(senderID);
            }
        }

        /// <summary>
        ///     Removes a player from the room.
        /// </summary>
        /// <param name="senderID"></param>
        public void RemovePlayer(ushort senderID)
        {
            Players.Remove(senderID);
        }

        /// <summary>
        ///   Determines if a player exists in the room.
        /// </summary>
        /// <param name="senderID"></param>
        /// <returns>
        ///     True if player exists in the room, false otherwise.
        /// </returns>
        public bool PlayerExists(ushort senderID)
        {
            return Players.IndexOf(senderID) != -1;
        }
        
        public override String ToString()
        {
            return ("Name of room: " + Name + " and max players of : " + MaxPlayers);
        }
    }
}
