using System;
using System.Collections.Generic;
using DarkRift;

namespace Room_Plugin
{
    class RoomPluginMain : Plugin {

        public const byte RoomTag = 147;

        public const byte ROOM_CREATE = 0;
        public const byte ROOM_JOIN = 1;
        public const byte ROOM_LEAVE = 2;
        public const byte ROOM_DELETE = 3;
        public const byte ROOM_LIST = 4;

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
                return new Command[]{
                         new Command("ListRooms","List all the rooms at the moment", ListRooms_Command)
                };
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

        #region Subject handlers

        private void CreateRoomHandler (ref NetworkMessage data) {

            Room roomToCreate = new Room();
            String name;
            int MaxPlayers;

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                name = reader.ReadString();
                MaxPlayers = reader.ReadUInt16();
            }

            lock (RoomList) {
                // If the name is blank, generate a unique room name by creating a UUID and then
                // seeing if it exists in the list of current rooms.
                if (name == "") {
                    while (true) {
                        string uniqueName = Guid.NewGuid().ToString();
                        if (!IsRoomNameInUse(uniqueName)) {
                            name = uniqueName;
                            break;
                        }
                    }
                }
                // Otherwise, we check if the supplied name is in use.
                else {
                    if (IsRoomNameInUse(name)) {
                        Interface.Log("That room name is already in use!");
                        return;
                    }
                }

                roomToCreate.SetMaxPlayers(MaxPlayers);
                roomToCreate.SetName(name);
                RoomList.Add(roomToCreate);
            }
            Interface.Log("Created room with name: " + name + ", max players: " + MaxPlayers);
        }

        private void JoinRoomHandler (ref NetworkMessage data) {

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                ushort senderId = data.senderID;
                String roomName = reader.ReadString();

                lock (RoomList) {
                    foreach (Room room in RoomList) {
                        if (room.GetName().Equals(roomName) && !room.PlayerExists(senderId)) {
                            room.AddPlayer(senderId);
                            Interface.Log("Joined room");
                            break;
                        }
                    }
                }
            }
        }

        private void LeaveRoomHandler (ref NetworkMessage data) {

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                ushort senderId = data.senderID;
                String roomName = reader.ReadString();

                lock (RoomList) {
                    foreach (Room room in RoomList) {
                        if (room.GetName().Equals(roomName) && room.PlayerExists(senderId)) {
                            room.RemovePlayer(senderId);
                            Interface.Log(senderId + " Left the Room");
                        }
                    }
                }
            }
        }

        private void ListRoomHandler (ConnectionService con, ref NetworkMessage data) {

            using (DarkRiftWriter writer = new DarkRiftWriter()) {
                foreach (Room room in RoomList) {
                    writer.Write(room.GetName()); //Write name and max players of each room and send it to the senderID
                    writer.Write(room.GetMaxPlayers());
                }

                con.SendReply(RoomTag, ROOM_LIST, writer);
            }
        }

        private void DefaultSubjectHandler (ref NetworkMessage data) {

            data.DecodeData();
            ushort senderId = data.senderID;

            foreach (Room room in RoomList) {
                if (room.PlayerExists(senderId)) {
                    foreach (ushort id in room.Players) {
                        if (data.distributionType == DistributionType.Custom) //To avoid duplicates use type 5 as all
                        {
                            ConnectionService tempCon = DarkRiftServer.GetConnectionServiceByID(id); //Gets the connection service between service and id
                            tempCon.SendNetworkMessage(data);
                            Interface.Log("Sending Msg");
                        } else if (data.distributionType.Equals(7)) //To avoid duplicates use type 7 as others
                          {
                            if (id != senderId) //Make sure message is not sent to the original sender
                            {
                                ConnectionService tempCon = DarkRiftServer.GetConnectionServiceByID(id); //Gets the connection service between service and id
                                tempCon.SendNetworkMessage(data);
                                Interface.Log("Sending Msg others");
                            }
                        } else {
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

        #endregion

        /// <summary>
        ///     Called when the player has connected and has been added to the list of players.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        void OnData(ConnectionService con, ref NetworkMessage data) {

            int subject = data.subject;

            // We only care about our "Room" tag
            if (data.tag != RoomTag)
            {
                return;
            }

            // Keep the original encoded data stored to pass on later
            object originalData = data.data;

            switch (data.subject)
            {
                case ROOM_CREATE:
                    CreateRoomHandler(ref data);
                    break;

                case ROOM_JOIN:
                    JoinRoomHandler(ref data);
                    break;

                case ROOM_LEAVE:
                    LeaveRoomHandler(ref data);
                    break;

                case ROOM_LIST:
                    ListRoomHandler(con, ref data);
                    break;

                default:
                    DefaultSubjectHandler(ref data);
                    break;
            }

            // Re-encode the data by setting the data property to its original value
            data.data = originalData;
        }

        /// <summary>
        ///     Returns true if Room name is in use
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        /// <summary>
        ///     Searches for the room and returns the index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        /// <summary>
        /// Command to List Rooms right now
        /// </summary>
        public void ListRooms_Command(String[] abc)
        {
            foreach(Room room in RoomList)
            {
                Interface.Log(room.ToString());
            }
        }
    }
}
