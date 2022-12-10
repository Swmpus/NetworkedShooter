using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MessageBuilder;

public class Client : UDPServer
{
    public Client(ClientUpdater updater) : base(updater)
    {
        // -- Init Connection --
        IPEndPoint serverIP;
        try {
            serverIP = new IPEndPoint(IPAddress.Parse(GlobalInfo.ServerIP), GlobalInfo.ServerPort);
            Debug.Log("Server Found");
        } catch (Exception) {
            // TODO Remove this once complex lobby behaviour is finished
            serverIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), GlobalInfo.ServerPort);
            Debug.Log("No server found at that IP, defaulting to local host");
        }

        // Establish connection to server
        UDPClient = new UdpClient(0);
        UDPClient.Connect(serverIP);

        byte[] sendBytes = BuildJoinMessage();
        UDPClient.Send(sendBytes, sendBytes.Length);
        Debug.Log("Message sent to " + UDPClient.Client.RemoteEndPoint.ToString());
    }

    internal void sendPlayerSpawned(Vector3 spawn)
    {
        byte[] sendBytes = BuildSpawnMessage(spawn);
        UDPClient.Send(sendBytes, sendBytes.Length);
    }

    public void SubmitVote(string vote)
    {
        byte[] sendBytes = BuildVoteMessage(vote);
        UDPClient.Send(sendBytes, sendBytes.Length);
    }

    internal void SendFire(Vector3 origin, Vector3 rotation)
    {
        byte[] sendBytes = BuildFireMessage(origin, rotation, GlobalInfo.UserName);
        UDPClient.Send(sendBytes, sendBytes.Length);

        updater.addToQueue(gameState, MessageType.FIRE, new object[] { origin, rotation, GlobalInfo.UserName });
    }

    internal void sendJoinTeam(string team)
    {
        byte[] sendBytes = BuildJoinTeamMessage(team);
        UDPClient.Send(sendBytes, sendBytes.Length);
        ((ClientUpdater)updater).ClientTeam = team;
    }

    public void sendVote(string vote)
    {
        byte[] sendBytes = BuildVoteMessage(vote);
        UDPClient.Send(sendBytes, sendBytes.Length);
    }

    internal void sendMove(Vector3 newPosition, Vector3 newRotation)
    {
        byte[] sendBytes = BuildMoveMessage(newPosition, newRotation);
        UDPClient.Send(sendBytes, sendBytes.Length);
    }

    public override void RunTick()
    {
        var prevState = gameState;
        gameState = updater.UpdateWorld(gameState);
        if (prevState.Equals(GameState.LOBBY) && gameState.Equals(GameState.GAME)) {
            SceneManager.LoadScene(updater.CurrentMap);
            // when spawned enable the player character and disable the spawn controller


            //byte[] sendBytes = BuildSpawnMessage(spawnLoc);
            //Task.Delay(1000).ContinueWith(t => UDPClient.Send(sendBytes, sendBytes.Length)); // TODO test TODO remove when spawning menu implemented

            //updater.addToQueue(gameState, MessageType.SPAWN, new object[] { spawnLoc, GlobalInfo.UserName }); // TODO implement spawn system
        }
    }

    public override void CloseSocket()
    {
        byte[] sendBytes = BuildQuitMessage();
        UDPClient.Send(sendBytes, sendBytes.Length);
        UDPClient.Close();
    }

    protected override bool handleMessageLobby(IPEndPoint client, byte[] message)
    {
        // -- Lobby --
            // Send map vote when ready
            // Optionally revote / cancel vote
            // Wait for map decision
        if (message[0].Equals((byte)MessageType.JOIN)) {
            string name = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.JOIN, new object[] { name });

            if (((ClientUpdater)updater).ClientTeam != null) {
                sendJoinTeam(((ClientUpdater)updater).ClientTeam);
            } else {
                sendJoinTeam(new List<string>(GameDefinitions.teams.Keys)[0]);
            }
            Debug.Log(name + " players received from " + client.Address + ":" + client.Port);
        } else if (message[0].Equals((byte)MessageType.QUIT)) {
            string name = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());

            updater.addToQueue(gameState, MessageType.QUIT, new object[] { name });
            Debug.Log(name + " quit");
        } else if (message[0].Equals((byte)MessageType.VOTE)) {
            string[] data = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray()).Split(':');

            updater.addToQueue(gameState, MessageType.VOTE, new object[] { data[0], data[1] });
            Debug.Log(data[0] + " voted for " + data[1]);
        } else if (message[0].Equals((byte)MessageType.JOINTEAM)) {
            string[] data = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray()).Split(':');

            updater.addToQueue(gameState, MessageType.JOINTEAM, new object[] { data[0], data[1] });
            Debug.Log(data[0] + " joined team " + data[1]);
        } else {
            // TODO test when this line actually gets hit
            Debug.Log("Message type supplied is not handled in this game state and will be ignored");
        }
        return true;
        throw new NotImplementedException();
    }

    protected override bool handleMessageGame(IPEndPoint client, byte[] message)
    {
        // -- Normal Game Loop --
            // - Listen to Server -
                // Select spawn / request spawn from server
                // Wait for server to allow spawn
                // Wait for server to kill
                // Wait for server to end game
            // - Send to Server -
                // Movement (decide how later)
                // Shooting
                // Tech
        if (message[0].Equals((byte)MessageType.FIRE)) {
            var pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            var rot = vector3FromByteArray(message.Skip(13).Take(12).ToArray());
            var name = Encoding.ASCII.GetString(message.Skip(25).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.FIRE, new object[] {
                                                                                pos,
                                                                                rot,
                                                                                name });
        } else if (message[0].Equals((byte)MessageType.SPAWN)) {
            var pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            var name = Encoding.ASCII.GetString(message.Skip(13).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.SPAWN, new object[] {
                                                                                pos,
                                                                                name });
        } else if (message[0].Equals((byte)MessageType.MOVE)) {
            var pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            var rot = vector3FromByteArray(message.Skip(13).Take(12).ToArray());
            var name = Encoding.ASCII.GetString(message.Skip(25).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.MOVE, new object[] {
                                                                                pos,
                                                                                rot,
                                                                                name });
        } else if (message[0].Equals((byte)MessageType.CPChange)) {
            byte ID = message.Skip(1).Take(1).First();
            var team = Encoding.ASCII.GetString(message.Skip(2).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.CPChange, new object[] {
                                                                            ID,
                                                                            team });
        } else if (message[0].Equals((byte)MessageType.KILL)) {
            var player = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());
            updater.addToQueue(gameState, MessageType.KILL, new object[] {
                                                                            player });
        }
        return true;
    }

    protected override bool handleMessageEndGame(IPEndPoint client, byte[] message)
    {
        // -- Game End --
            // Show Scoreboard
            // Wait for lobby
        throw new NotImplementedException();
    }
}
