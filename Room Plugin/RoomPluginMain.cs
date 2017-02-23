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
        //public const byte SYNC_TAG = 2;
        public ArrayList Room1 = new ArrayList();
        public ArrayList Room2 = new ArrayList();
        public ArrayList Room3 = new ArrayList();
        public ArrayList Room4 = new ArrayList();
        public ArrayList Room5 = new ArrayList();
        public ArrayList Room6 = new ArrayList();
        public ArrayList Room7 = new ArrayList();
        public ArrayList Room8 = new ArrayList();
        public ArrayList Room9 = new ArrayList();
        public ArrayList Room10 = new ArrayList();




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
            ConnectionService.onPlayerDisconnect += OnPlayerDisconnect;
            ConnectionService.onData += OnData;
        }

        /// <summary>
        ///     Called when the player has connected and has been added to the list of players.
        /// </summary>
        /// <param name="con">The connection to the player.</param>
        void OnData(ConnectionService con, ref NetworkMessage data)
        { 
            if(data.tag == ROOM_CREATE)
            {

            }
            if(data.tag == ROOM_JOIN)
            {
                data.DecodeData();
                using (DarkRiftReader reader = data.data as DarkRiftReader)
                {
                    ushort senderID = data.senderID;
                    ushort room = reader.ReadUInt16();

                    switch (room)
                    {
                        case 1:
                            Room1.Add(senderID);
                            break;
                        case 2:
                            Room2.Add(senderID);
                            break;
                        case 3:
                            Room3.Add(senderID);
                            break;
                        case 4:
                            Room4.Add(senderID);
                            break;
                        case 5:
                            Room5.Add(senderID);
                            break;
                        case 6:
                            Room6.Add(senderID);
                            break;
                        case 7:
                            Room7.Add(senderID);
                            break;
                        case 8:
                            Room8.Add(senderID);
                            break;
                        case 9:
                            Room9.Add(senderID);
                            break;
                        case 10:
                            Room10.Add(senderID);
                            break;
                        default:
                            Interface.Log("Dude Send Room info!");
                            break;
                    }
                }
            }
            
        }





        }
}
