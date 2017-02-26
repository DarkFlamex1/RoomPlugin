using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;

namespace Room_Plugin
{
    class Room
    {
        private int MaxPlayers;
        public String Name;
        public List<ushort> Players = new List<ushort>();

        public void SetMaxPlayers(int max)
        {
            MaxPlayers = max;
        }

        public int GetMaxPlayers()
        {
            return MaxPlayers;
        }

        public void SetName(String max)
        {
            Name = max;
        }

        public String GetName()
        {
            return Name;
        }

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

        public void RemovePlayer(ushort senderID)
        {
            Players.Remove(senderID);
        }

        public bool PlayerExists(ushort senderID)
        {
            if(Players.IndexOf(senderID) == -1) //Checks if the specific id exists within room
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
