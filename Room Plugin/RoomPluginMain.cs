using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        // Event subjects
        public const byte CREATED_ROOM_EVENT = 5;
        public const byte CREATED_ROOM_FAILED_EVENT = 6;
        public const byte JOINED_ROOM_EVENT = 7;
        public const byte JOINED_ROOM_FAILED_EVENT = 8;
        public const byte LEFT_ROOM_EVENT = 9;
        public const byte LEFT_ROOM_FAILED_EVENT = 10;
        public const byte PLAYER_JOINED_ROOM_EVENT = 11;
        public const byte PLAYER_LEFT_ROOM_EVENT = 12;

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
            ConnectionService.onData += OnData;
            ConnectionService.onPlayerDisconnect += OnPlayerDisconnect;
        }

        #region Subject handlers

        private void CreateRoomHandler (ConnectionService con, ref NetworkMessage data) {

            ushort senderId = con.id;

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
                // Otherwise, we check if the supplied name is in use, and respond with an error
                // if it is.
                else {
                    if (DoesRoomExist(roomName)) {
                        Interface.Log("That room name is already in use!");
                        using (DarkRiftWriter writer = new DarkRiftWriter()) {
                            writer.Write("Room name is in use");
                            con.SendReply(RoomTag, CREATED_ROOM_FAILED_EVENT, writer);
                        }
                        return;
                    }
                }

                roomToCreate.SetMaxPlayers(maxPlayers);
                roomToCreate.SetName(roomName);
                roomToCreate.AddPlayer(senderId);
                roomsByName.Add(roomName, roomToCreate);
            }

            senderIdToRoomName.Add(senderId, roomName);

            using (DarkRiftWriter writer = new DarkRiftWriter()) {
                writer.Write(roomName);
                con.SendReply(RoomTag, CREATED_ROOM_EVENT, writer);
                Interface.Log("replying to: " + senderId);
            }

            Interface.Log("Created room with name: " + roomName + ", max players: " + maxPlayers);
        }

        private void JoinRoomHandler (ConnectionService con, ref NetworkMessage data) {

            ushort senderId = con.id;

            data.DecodeData();

            using (DarkRiftReader reader = data.data as DarkRiftReader) {
                String roomName = reader.ReadString();
                Room roomToJoin;

                lock (roomsByName) {
                    // If room doesn't exist, reply with an error
                    if (!DoesRoomExist(roomName)) {
                        Interface.Log("Can't join - room does not exist!");
                        using (DarkRiftWriter writer = new DarkRiftWriter()) {
                            writer.Write("Room does not exist");
                            con.SendReply(RoomTag, JOINED_ROOM_FAILED_EVENT, writer);
                        }
                        return;
                    }

                    roomToJoin = roomsByName[roomName];

                    // If player is already in room, do nothing
                    if (roomToJoin.PlayerExists(senderId)) {
                        Interface.Log("Player is already in room.");
                        return;
                    }

                    roomToJoin.AddPlayer(senderId);
                    senderIdToRoomName[senderId] = roomToJoin.GetName();

                    Interface.Log("Player has joined room.");
                }

                using (DarkRiftWriter writer = new DarkRiftWriter()) {
                    writer.Write(roomName);
                    con.SendReply(RoomTag, JOINED_ROOM_EVENT, writer);
                    Interface.Log("replying to: " + senderId);
                }

                // Tell each player in the room that someone has joined, except for the person that
                // just joined.
                foreach (ushort id in roomToJoin.Players) {
                    if (id == senderId) {
                        continue;
                    }
                    DarkRiftServer.GetConnectionServiceByID(id).SendReply(RoomTag, PLAYER_JOINED_ROOM_EVENT, senderId);
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
                        Interface.Log("Can't leave - room does not exist!");
                        using (DarkRiftWriter writer = new DarkRiftWriter()) {
                            writer.Write("Room does not exist");
                            con.SendReply(RoomTag, LEFT_ROOM_FAILED_EVENT, writer);
                        }
                        return;
                    }

                    Room roomToLeave = roomsByName[roomName];

                    // Also reply with error if room exists but player is not in it
                    if (!roomToLeave.PlayerExists(senderId)) {
                        Interface.Log("Can't leave - player is not in room!");
                        using (DarkRiftWriter writer = new DarkRiftWriter()) {
                            writer.Write("Player is not in room");
                            con.SendReply(RoomTag, LEFT_ROOM_FAILED_EVENT, writer);
                        }
                        return;
                    }

                    RemovePlayerFromRoom(roomToLeave, senderId);
                }

                senderIdToRoomName.Remove(senderId);
                Interface.Log("Left room.");

                using (DarkRiftWriter writer = new DarkRiftWriter()) {
                    con.SendReply(RoomTag, LEFT_ROOM_EVENT, writer);
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

        #region ConnectionService delegates

        /// <summary>
        ///     Called when data is received over the network from a player.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        /// <param name="data">Data received.</param>
        private void OnData (ConnectionService con, ref NetworkMessage data) {

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
        ///     Called when a player disconnects from the server. Removes them from the room they
        ///     are in (if they are in one), and, if the room is now empty, removes the room.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        private void OnPlayerDisconnect (ConnectionService con) {

            ushort senderId = con.id;

            if (!senderIdToRoomName.ContainsKey(senderId)) {
                return;
            }

            // Player is in a room; find the room and, if it exists, remove them from it.
            string roomNameWithSender = senderIdToRoomName[senderId];

            lock (roomsByName) {
                if (roomsByName.ContainsKey(roomNameWithSender)) {
                    RemovePlayerFromRoom(roomsByName[roomNameWithSender], senderId);
                }
            }

            senderIdToRoomName.Remove(senderId);
        }

        #endregion

        #region Message helpers

        private void RemovePlayerFromRoom (Room room, ushort playerToRemoveId) {

            room.RemovePlayer(playerToRemoveId);

            // If the room is now empty, destroy the room. Otherwise, send a message to the
            // others in the room saying a player has left.
            if (room.Players.Count == 0) {
                roomsByName.Remove(room.GetName());
            } else {
                foreach (ushort id in room.Players) {
                    DarkRiftServer.GetConnectionServiceByID(id).SendReply(RoomTag, PLAYER_LEFT_ROOM_EVENT, playerToRemoveId);
                }
            }
        }

        #endregion

        #region Other helpers

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

        #endregion
    }
}
