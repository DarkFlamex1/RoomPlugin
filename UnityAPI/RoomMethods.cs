using DarkRift;
using RoomPlugin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomMethods : MonoBehaviour {

    public void RoomCreate(string name, int MaxPlayers)
    {
        using (DarkRiftWriter write = new DarkRiftWriter())
        {
            write.Write(name);
            write.Write(MaxPlayers);

            DarkRiftAPI.SendMessageToServer(TagIndex.ROOM_CREATE, TagIndex.RoomSubjects.Room, write);
        }
    }
    public void RoomJoin(string name)
    {

        using (DarkRiftWriter write = new DarkRiftWriter())
        {
            write.Write(name);

            DarkRiftAPI.SendMessageToServer(TagIndex.ROOM_JOIN, TagIndex.RoomSubjects.Room, write);
        }

        
    }
    public void RoomLeave(string name)
    {

        using (DarkRiftWriter write = new DarkRiftWriter())
        {
            write.Write(name);

            DarkRiftAPI.SendMessageToServer(TagIndex.ROOM_LEAVE, TagIndex.RoomSubjects.Room, write);
        }


    }
}
