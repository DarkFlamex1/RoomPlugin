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

        // Note that we have to keep these two dicts in sync.
        public Dictionary<string, Room> roomsByName = new Dictionary<string, Room>();
        public Dictionary<ushort, string> senderIdToRoomName = new Dictionary<ushort, string>();

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

        private void CreateRoomHandler (ConnectionService con, ref NetworkMessage data) {

            Room roomToCreate = new Room();
            String roomName;
            int maxPlayers;

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                roomName = reader.ReadString();
                maxPlayers = reader.ReadUInt16();
            }

            lock (roomsByName) {
                // If the name is blank, generate a unique room name by creating a UUID and then
                // seeing if it exists in the list of current rooms.
                if (roomName == "") {
                    while (true) {
                        string uniqueName = Guid.NewGuid().ToString();
                        if (!DoesRoomExist(uniqueName)) {
                            roomName = uniqueName;
                            break;
                        }
                    }
                }
                // Otherwise, we check if the supplied name is in use.
                else {
                    if (DoesRoomExist(roomName)) {
                        // TODO -- reply to user with subject CreateRoomFailed 
                        Interface.Log("That room name is already in use!");
                        return;
                    }
                }

                ushort senderId = con.id;

                roomToCreate.SetMaxPlayers(maxPlayers);
                roomToCreate.SetName(roomName);
                roomsByName.Add(roomName, roomToCreate);
                senderIdToRoomName.Add(senderId, roomName);

                // TODO -- reply to user with 2 messages, subjects: CreateRoom, JoinRoom
                roomToCreate.AddPlayer(senderId);
            }

            Interface.Log("Created room with name: " + roomName + ", max players: " + maxPlayers);
        }

        private void JoinRoomHandler (ConnectionService con, ref NetworkMessage data) {

            ushort senderId = con.id;

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                String roomName = reader.ReadString();

                lock (roomsByName) {
                    if (!DoesRoomExist(roomName)) {
                        // TODO -- reply to user swith subject JoinRoomFailed
                        Interface.Log("Can't join - room does not exist!");
                        return;
                    }

                    Room roomToJoin = roomsByName[roomName];

                    // If player is already in room, do nothing
                    if (roomToJoin.PlayerExists(senderId)) {
                        Interface.Log("Player is already in room.");
                        return;
                    }

                    roomToJoin.AddPlayer(senderId);
                    senderIdToRoomName[senderId] = roomToJoin.GetName();

                    // TODO -- reply to user with subject JoinRoom
                    Interface.Log("Player has joined room.");
                }
            }
        }

        private void LeaveRoomHandler (ConnectionService con, ref NetworkMessage data) {

            ushort senderId = con.id;

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                String roomName = reader.ReadString();

                lock (roomsByName) {

                    // If the room doesn't exist, reply with error
                    if (!DoesRoomExist(roomName)) {
                        // TODO -- reply to user with subject LeaveRoomFailed
                        Interface.Log("Can't leave - room does not exist!");
                        return;
                    }

                    Room roomToLeave = roomsByName[roomName];

                    // Also reply with error if room exists but player is not in it
                    if (!roomToLeave.PlayerExists(senderId)) {
                        // TODO -- reply to user with subject LeaveRoomFailed
                        Interface.Log("Can't leave - player is not in room!");
                        return;
                    }

                    roomToLeave.RemovePlayer(senderId);
                    senderIdToRoomName.Remove(senderId);

                    // TODO -- reply to user with subject LeaveRoom
                    Interface.Log("Left room.");
                }
            }
        }

        private void ListRoomHandler (ConnectionService con, ref NetworkMessage data) {

            lock (roomsByName) {
                using (DarkRiftWriter writer = new DarkRiftWriter()) {
                    foreach (var roomNamePair in roomsByName) {
                        writer.Write(roomNamePair.Key); //Write name and max players of each room and send it to the senderID
                        writer.Write(roomNamePair.Value.GetMaxPlayers());
                    }

                    con.SendReply(RoomTag, ROOM_LIST, writer);
                }
            }
        }

        private void DefaultSubjectHandler (ref NetworkMessage data) {

            data.DecodeData();
            ushort senderId = data.senderID;

            // If the player is not a room, do nothing
            if (!IsPlayerInAnyRoom(senderId)) {
                return;
            }

            Room roomToMessage;

            lock (roomsByName) {
                roomToMessage = roomsByName[senderIdToRoomName[senderId]];
            }

            if (data.distributionType == DistributionType.Custom) {
                // To avoid duplicates use type 5 as all
                foreach (ushort id in roomToMessage.Players) {
                    DarkRiftServer.GetConnectionServiceByID(id).SendNetworkMessage(data);
                    Interface.Log("Sending Msg");
                }
            } else if (data.distributionType.Equals(7)) {
                // To avoid duplicates use type 7 as others
                foreach (ushort id in roomToMessage.Players) {
                    if (id == senderId) {
                        continue;
                    }
                    DarkRiftServer.GetConnectionServiceByID(id).SendNetworkMessage(data);
                    Interface.Log("Sending Msg others");
                }
            } else {
                foreach (ushort id in roomToMessage.Players) {
                    if (id == senderId) {
                        continue;
                    }
                    DarkRiftServer.GetConnectionServiceByID(id).SendNetworkMessage(data);
                }
            }
        }

        #endregion

        /// <summary>
        ///     Called when the player has connected and has been added to the list of players.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        void OnData(ConnectionService con, ref NetworkMessage data) {

            // We only care about our "Room" tag
            if (data.tag != RoomTag) {
                return;
            }

            // Keep the original encoded data stored to pass on later
            object originalData = data.data;

            switch (data.subject) {
                case ROOM_CREATE:
                    CreateRoomHandler(con, ref data);
                    break;

                case ROOM_JOIN:
                    JoinRoomHandler(con, ref data);
                    break;

                case ROOM_LEAVE:
                    LeaveRoomHandler(con, ref data);
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
        /// <param name="roomName"></param>
        /// <returns></returns>
        private bool DoesRoomExist (string roomName) {

            return roomsByName.ContainsKey(roomName);
        }

        private bool IsPlayerInAnyRoom (ushort senderId) {

            return senderIdToRoomName.ContainsKey(senderId);
        }

        /// <summary>
        ///     Searches for the room and returns the index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        /// <summary>
        /// Command to List Rooms right now
        /// </summary>
        public void ListRooms_Command (string[] placeholder) {
            lock (roomsByName) {
                foreach (var roomNamePair in roomsByName) {
                    Interface.Log(roomNamePair.Value.ToString());
                }
            }
        }
    }
}
