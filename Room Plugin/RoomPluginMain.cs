using System;
using System.Collections.Generic;
using DarkRift;

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
                return "hax@gmail.com";
            }
        }

        public override string version
        {
            get
            {
                return "0.02a";
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

                lock (RoomList)
                {
                    // If the name is blank, generate a unique room name by creating a UUID and then
                    // seeing if it exists in the list of current rooms.
                    if (name == "")
                    {
                        while (true)
                        {
                            string uniqueName = Guid.NewGuid().ToString();
                            if (!IsRoomNameInUse(uniqueName))
                            {
                                name = uniqueName;
                                break;
                            }
                        }
                    }
                    // Otherwise, we check if the supplied name is in use.
                    else {
                        if (IsRoomNameInUse(name))
                        {
                            Interface.Log("That room name is already in use!");
                            return;
                        }
                    }
                }

                temp.SetMaxPlayers(MaxPlayers);
                temp.SetName(name);

                RoomList.Add(temp); //Add if it doesnt exist
                Interface.Log("Created room with name " + name + "max players" + MaxPlayers);
            }
            else if(data.tag == ROOM_JOIN)
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
                                Interface.Log("Joined room");
                                break;
                            }
                        }
                    }
                }
            }
            else if(data.tag == ROOM_LEAVE)
            {
                data.DecodeData();
                using (DarkRiftReader reader = data.data as DarkRiftReader)
                {
                    ushort senderId = data.senderID;
                    lock (RoomList)
                    {
                        foreach (Room room in RoomList)
                        {
                            if (room.PlayerExists(senderId))
                            {
                                room.RemovePlayer(senderId);
                                Interface.Log(senderId + " Left the Room");
                            }
                        }
                    }
                }
            }
            else
            {
                data.DecodeData();
                ushort senderId = data.senderID;
                
                foreach(Room room in RoomList)
                {
                    if (room.PlayerExists(senderId))
                    {
                        foreach(ushort id in room.Players)
                        {
                            if(data.distributionType == DistributionType.Custom) //To avoid duplicates use type 5 as all
                            {
                                ConnectionService tempCon = DarkRiftServer.GetConnectionServiceByID(id); //Gets the connection service between service and id
                                tempCon.SendNetworkMessage(data);
                                Interface.Log("Sending Msg");
                            }
                            else if (data.distributionType.Equals(7)) //To avoid duplicates use type 7 as others
                            {
                                if (id != senderId) //Make sure message is not sent to the original sender
                                {
                                    ConnectionService tempCon = DarkRiftServer.GetConnectionServiceByID(id); //Gets the connection service between service and id
                                    tempCon.SendNetworkMessage(data);
                                    Interface.Log("Sending Msg others");
                                }
                            }
                            else
                            {
                                if (id != senderId) //Make sure message is not sent to the original sender
                                {
                                    ConnectionService tempCon = DarkRiftServer.GetConnectionServiceByID(id); //Gets the connection service between service and id
                                    tempCon.SendNetworkMessage(data);
                                }
                            }
                        }
                    }
                }
            }       
        }

        private bool IsRoomNameInUse (string name) {

            bool nameIsInUse = false;

            foreach (Room room in RoomList) {
                if (room.GetName() == name) {
                    nameIsInUse = true;
                    break;
                }
            }

            return nameIsInUse;
        }
    }
}
