using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;
using System.Collections;

namespace Room_Plugin
{
    class RoomPluginMain : Plugin
    {
        public const byte ROOM_CREATE = 0;
        public const byte ROOM_JOIN = 1;
        public const byte ROOM_LEAVE = 2;

        public List<Room> RoomList  = new List<Room>();


        public override string author
        {
            get
            {
                return "Vikram Peddinti";
            }
        }

        public override Command[] commands
        {
            get
            {
                return new Command[0];
            }
        }

        public override string name
        {
            get
            {
                return "Room Plugin";
            }
        }

        public override string supportEmail
        {
            get
            {
                return "vikram.peddinti@gmail.com";
            }
        }

        public override string version
        {
            get
            {
                return "0.01a";
            }
        }

        public RoomPluginMain()
        {
            Interface.Log("Instilizing");
            //ConnectionService.onPlayerDisconnect += OnPlayerDisconnect;
            ConnectionService.onData += OnData; //Pass on to the OnData function
        }

        /// <summary>
        ///     Called when the player has connected and has been added to the list of players.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        void OnData(ConnectionService con, ref NetworkMessage data)
        { 
            if(data.tag == ROOM_CREATE)
            {
                Room temp = new Room(); //The room object 
                String name;
                int MaxPlayers;

                data.DecodeData(); //Decode the Data
                using (DarkRiftReader reader = data.data as DarkRiftReader)
                {
                    name = reader.ReadString(); //Name of Room is always first
                    MaxPlayers = reader.ReadUInt16(); //MaxPlayers read after

                }

                temp.SetName(name);
                temp.SetMaxPlayers(MaxPlayers);
                lock (RoomList)
                {
                    RoomList.Add(temp);
                }
                
            }
            if(data.tag == ROOM_JOIN)
            {
                data.DecodeData();
                using (DarkRiftReader reader = data.data as DarkRiftReader)
                {
                    ushort senderID = data.senderID;
                    String name = reader.ReadString();

                    lock (RoomList)
                    {
                        foreach (Room room in RoomList)
                        {
                            if (room.GetName().Equals(name))
                            {
                                room.AddPlayer(senderID);
                                break;
                            }
                        }
                    }
                }
            }
            
        }





        }
}
