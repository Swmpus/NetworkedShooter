using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MessageBuilder;

public class Server : UDPServer
{
    private HashSet<IPEndPoint> clients;
    private Dictionary<IPEndPoint, string> clientNames;

    public Server(ServerUpdater updater) : base(updater)
    {
        // -- Init Server --
        clientNames = new Dictionary<IPEndPoint, string>();
        UDPClient = new UdpClient(GlobalInfo.ServerPort);
        clients = new HashSet<IPEndPoint>();

        // Start Server listening
        Debug.Log("Server Started on port " + GlobalInfo.ServerPort);
    }

    public override void RunTick()
    {
        var prevState = gameState;
        gameState = updater.UpdateWorld(gameState);

        if (prevState.Equals(GameState.LOBBY) && gameState.Equals(GameState.GAME)) {
            // TODO Implement timer before map starts proper to allow people to load in and prevent message race conditions
            SceneManager.LoadScene(updater.CurrentMap);
            byte[] message = BuildVoteMessage("Server", updater.CurrentMap);
            relayMessage(message);
        }
    }

    public override void CloseSocket()
    {
        // TODO tell clients that the server closed
        byte[] updateBytes = BuildServerCloseMessage();
        relayMessage(updateBytes);
        UDPClient.Close();
    }

    internal void ForceMapVote()
    {
        updater.addToQueue(gameState, MessageType.VOTE, new object[] { "Server", "none" });
    }


    private void relayMessage(IPEndPoint sender, byte[] message)
    {
        foreach (IPEndPoint client in clients) {
            if (!sender.Equals(client)) {
                UDPClient.Send(message, message.Length, client);
            }
        }
    }

    private void relayMessage(byte[] message)
    {
        foreach (IPEndPoint client in clients) {
            UDPClient.Send(message, message.Length, client);
        }
    }

    internal void sendKill(GameObject placeholder)
    {
        string name = updater.getPlayerName(placeholder);
        Debug.Log("Player Killed: " + name);
        updater.addToQueue(gameState, MessageType.KILL, new object[] { name });
        byte[] sendBytes = BuildKillMessage(name);
        relayMessage(sendBytes);
    }

    internal void sendCPChange(byte ID, string controllingTeam)
    {
        byte[] sendBytes = BuildCPChangeMessage(ID, controllingTeam);
        relayMessage(sendBytes);
    }

    protected override bool handleMessageLobby(IPEndPoint client, byte[] message)
    {
        if (message[0].Equals((byte)MessageType.JOIN)) { // TODO ensure no two players have the same name
            byte[] sendBytes = BuildPlayerListMessage(clientNames.Values);
            UDPClient.Send(sendBytes, sendBytes.Length, client);

            string name = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());

            byte[] updateBytes = BuildJoinMessage(name);
            relayMessage(client, updateBytes);

            lock (clients) {
                clients.Add(client);
                clientNames.Add(client, name);
            }
            updater.addToQueue(gameState, MessageType.JOIN, new object[] { name });
            // TODO test the code below down to the debug
            
            foreach (KeyValuePair<string, WorldUpdater.PlayerReference> playerRef in updater.Players) {
                byte[] updateBytes2 = BuildJoinTeamMessage(playerRef.Key, playerRef.Value.team);
                UDPClient.Send(updateBytes2, updateBytes2.Length, client);
            }
            foreach (KeyValuePair<string, string> vote in ((ServerUpdater)updater).playerVotes) {
                byte[] updateBytes2 = BuildVoteMessage(vote.Key, vote.Value);
                UDPClient.Send(updateBytes2, updateBytes2.Length, client);
            }
            Debug.Log(name + " joined from " + client.Address + ":" + client.Port);
        } else if (message[0].Equals((byte)MessageType.QUIT)) {
            string name = clientNames[client];
            lock (clients) {
                clients.Remove(client);
                clientNames.Remove(client);
            }
            relayMessage(message);

            updater.addToQueue(gameState, MessageType.QUIT, new object[] { name });
            Debug.Log(name + " from " + client.Address + ":" + client.Port + " quit");
        } else if (message[0].Equals((byte)MessageType.VOTE)) {
            string vote = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());
            byte[] updateBytes = BuildVoteMessage(clientNames[client], vote);
            relayMessage(client, updateBytes);

            updater.addToQueue(gameState, MessageType.VOTE, new object[] { clientNames[client], vote });
            Debug.Log(clientNames[client] + " voted for " + vote);
        } else if (message[0].Equals((byte)MessageType.JOINTEAM)) {
            string team = Encoding.ASCII.GetString(message.Skip(1).Take(message.Length).ToArray());

            byte[] updateBytes = BuildJoinTeamMessage(clientNames[client], team);
            relayMessage(client, updateBytes);

            updater.addToQueue(gameState, MessageType.JOINTEAM, new object[] { clientNames[client], team });
            Debug.Log(clientNames[client] + " joined team " + team);
        } else {
            Debug.Log("Message type supplied is not handled in this game state and will be ignored");
        }
        return true;
    }

    protected override bool handleMessageGame(IPEndPoint client, byte[] message)
    {
        // -- Normal Game Loop --
            // - Listen to Clients -
                // Spawning (client authoritative)
                // Movement (client authoritative)
                // Shooting (server authoritative)
                // Tech usage (server authoritative)
        if (message[0].Equals((byte)MessageType.FIRE)) {
            relayMessage(client, message);

            Vector3 pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            Vector3 rot = vector3FromByteArray(message.Skip(13).Take(12).ToArray());
            string name = Encoding.ASCII.GetString(message.Skip(25).Take(message.Length).ToArray());

            updater.addToQueue(gameState, MessageType.FIRE, new object[] { pos, rot, name });
        } else if (message[0].Equals((byte)MessageType.SPAWN)) {
            Vector3 pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            byte[] updateBytes = BuildSpawnMessage(pos, clientNames[client]);
            relayMessage(client, updateBytes);

            updater.addToQueue(gameState, MessageType.SPAWN, new object[] { pos, clientNames[client] });

            Debug.Log(clientNames[client] + " spawned at " + pos.ToString());
        } else if (message[0].Equals((byte)MessageType.MOVE)) {
            Vector3 pos = vector3FromByteArray(message.Skip(1).Take(12).ToArray());
            Vector3 rot = vector3FromByteArray(message.Skip(13).Take(12).ToArray());

            byte[] updateBytes = BuildMoveMessage(pos, rot, clientNames[client]);
            relayMessage(client, updateBytes);

            updater.addToQueue(gameState, MessageType.MOVE, new object[] { pos, rot, clientNames[client] });
        }
            // - Game Logic -
                // Spawn ai on a timer
                // Manage kill feed
                // Manage team score
            // - Send to Clients -
                // Take damage
                // Die
                // AI shoot / Tech
                // AI Pathing (largely predicted by clients)
        return true;
    }

    protected override bool handleMessageEndGame(IPEndPoint client, byte[] message)
    {
        // -- Game End Loop --
            // Send scoreboard to all
            // Wait some time
            // Return to Lobby
        return false;
    }
}
