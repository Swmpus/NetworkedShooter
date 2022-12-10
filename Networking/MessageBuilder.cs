using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

// TODO There should really be methods for also deconstructing messages here
public static class MessageBuilder
{
    public enum MessageType : byte
    {
        JOIN = 0, // For players joining the server
        QUIT = 1, // For players leaving but also for the server closing
        VOTE = 2, // For players voting for a map choice
        JOINTEAM = 3, // For players joining a team
        SPAWN = 4, // For spawning both players and AI
        MOVE = 5, // For updating the movement of players TODO and AI?
        FIRE = 6, // For a bullet being created
        CPChange = 7, // For any CP update
        KILL = 8, // For any CP update
        SERVERCLOSED = 255, // For when the server closes at any point TODO implement
    }

    public static byte[] BuildFireMessage(Vector3 origin, Vector3 rotation, string player)
    {
        return new byte[] { (byte)MessageType.FIRE }
            .Concat(BitConverter.GetBytes(origin.x).Concat(BitConverter.GetBytes(origin.y).Concat(BitConverter.GetBytes(origin.z))
            .Concat(BitConverter.GetBytes(rotation.x).Concat(BitConverter.GetBytes(rotation.y).Concat(BitConverter.GetBytes(rotation.z))))))
            .Concat(Encoding.ASCII.GetBytes(player)).ToArray();
    }

    public static byte[] BuildJoinMessage()
    {
        return new byte[] { (byte)MessageType.JOIN }
            .Concat(Encoding.ASCII.GetBytes(GlobalInfo.UserName)).ToArray();
    }

    public static byte[] BuildJoinMessage(string name)
    {
        return new byte[] { (byte)MessageType.JOIN }
            .Concat(Encoding.ASCII.GetBytes(name)).ToArray();
    }

    public static byte[] BuildVoteMessage(string vote)
    {
        return new byte[] { (byte)MessageType.VOTE }
            .Concat(Encoding.ASCII.GetBytes(vote)).ToArray();
    }

    internal static byte[] BuildServerCloseMessage()
    {
        return new byte[] { (byte)MessageType.SERVERCLOSED };
    }

    public static byte[] BuildVoteMessage(string name, string vote)
    {
        return new byte[] { (byte)MessageType.VOTE }
            .Concat(Encoding.ASCII.GetBytes(name)).Concat(Encoding.ASCII.GetBytes(":")).Concat(Encoding.ASCII.GetBytes(vote)).ToArray();
    }

    public static byte[] BuildQuitMessage()
    {
        return new byte[] { (byte)MessageType.QUIT }
            .Concat(Encoding.ASCII.GetBytes(GlobalInfo.UserName)).ToArray();
    }

    internal static byte[] BuildKillMessage(string name)
    {
        return new byte[] { (byte)MessageType.KILL }
            .Concat(Encoding.ASCII.GetBytes(name)).ToArray();
    }

    internal static byte[] BuildMoveMessage(Vector3 newPosition, Vector3 newRotation)
    {
        return new byte[] { (byte)MessageType.MOVE }
            .Concat(BitConverter.GetBytes(newPosition.x).Concat(BitConverter.GetBytes(newPosition.y).Concat(BitConverter.GetBytes(newPosition.z))))
            .Concat(BitConverter.GetBytes(newRotation.x).Concat(BitConverter.GetBytes(newRotation.y).Concat(BitConverter.GetBytes(newRotation.z)))).ToArray();
    }

    internal static byte[] BuildMoveMessage(Vector3 newPosition, Vector3 newRotation, string name)
    {
        return new byte[] { (byte)MessageType.MOVE }
            .Concat(BitConverter.GetBytes(newPosition.x).Concat(BitConverter.GetBytes(newPosition.y).Concat(BitConverter.GetBytes(newPosition.z))))
            .Concat(BitConverter.GetBytes(newRotation.x).Concat(BitConverter.GetBytes(newRotation.y).Concat(BitConverter.GetBytes(newRotation.z))))
            .Concat(Encoding.ASCII.GetBytes(name)).ToArray();
    }

    internal static byte[] BuildJoinTeamMessage(string team)
    { // TODO Make a team byte enum
        return new byte[] { (byte)MessageType.JOINTEAM }
            .Concat(Encoding.ASCII.GetBytes(team)).ToArray();
    }

    internal static byte[] BuildJoinTeamMessage(string name, string team)
    {
        return new byte[] { (byte)MessageType.JOINTEAM }
            .Concat(Encoding.ASCII.GetBytes(name)).Concat(Encoding.ASCII.GetBytes(":")).Concat(Encoding.ASCII.GetBytes(team)).ToArray();
    }

    internal static byte[] BuildSpawnMessage(Vector3 spawnLoc)
    {
        return new byte[] { (byte)MessageType.SPAWN }
            .Concat(BitConverter.GetBytes(spawnLoc.x).Concat(BitConverter.GetBytes(spawnLoc.y).Concat(BitConverter.GetBytes(spawnLoc.z)))).ToArray();
    }

    internal static byte[] BuildSpawnMessage(Vector3 location, string name)
    {
        return new byte[] { (byte)MessageType.SPAWN }
            .Concat(BitConverter.GetBytes(location.x).Concat(BitConverter.GetBytes(location.y).Concat(BitConverter.GetBytes(location.z))))
            .Concat(Encoding.ASCII.GetBytes(name)).ToArray();
    }

    internal static byte[] BuildCPChangeMessage(byte ID, string controllingTeam)
    {
        return new byte[] { (byte)MessageType.CPChange, ID }
            .Concat(Encoding.ASCII.GetBytes(controllingTeam)).ToArray();
    }

    internal static byte[] BuildPlayerListMessage(Dictionary<IPEndPoint, string>.ValueCollection names)
    {
        Dictionary<IPEndPoint, string>.ValueCollection.Enumerator en = names.GetEnumerator();
        string nameGroup = "";
        nameGroup += en.Current;
        en.MoveNext();

        while (en.Current != null) {
            nameGroup += ":";
            nameGroup += en.Current;
            en.MoveNext();
        }
        return new byte[] { (byte)MessageType.JOIN }.Concat(Encoding.ASCII.GetBytes(nameGroup)).ToArray();
    }

    public static Vector3 vector3FromByteArray(byte[] input)
    {
        return new Vector3(BitConverter.ToSingle(input, 0), BitConverter.ToSingle(input, 4), BitConverter.ToSingle(input, 8));
    }
}
