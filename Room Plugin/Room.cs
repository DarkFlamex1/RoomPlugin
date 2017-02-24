using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Players.Add(senderID);
        }
    }
}
