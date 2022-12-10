using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public abstract class UDPServer
{
    protected UdpClient UDPClient;
    protected WorldUpdater updater;
    protected GameState gameState;
    private Dictionary<GameState, Delegate> messageHandlers;
    public enum GameState : byte
    { // TODO Should be protected, needs a refactor
        LOBBY = 0,
        GAME = 1,
        ENDGAME = 2,
    }

    public UDPServer(WorldUpdater updater)
    {
        this.updater = updater;
        this.gameState = GameState.LOBBY;

        messageHandlers = new Dictionary<GameState, Delegate>();
        messageHandlers[GameState.LOBBY] = new Func<IPEndPoint, byte[], bool>(handleMessageLobby);
        messageHandlers[GameState.GAME] = new Func<IPEndPoint, byte[], bool>(handleMessageGame);
        messageHandlers[GameState.ENDGAME] = new Func<IPEndPoint, byte[], bool>(handleMessageEndGame);

        new Thread(delegate () { getMessage(); }).Start();
    }

    abstract public void RunTick();

    internal GameState GetGameState()
    {
        return gameState;
    }

    private void getMessage()
    {
        while (UDPClient == null) {}
        while (true) {
            IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);

            byte[] receiveBytes = UDPClient.Receive(ref client);

            new Thread(delegate () { bool success = (bool)messageHandlers[gameState].DynamicInvoke(client, receiveBytes); }).Start();
        }
    }

    internal List<CommandPost> getEnemyCommandPosts(string team)
    {
        return updater.getEnemyCommandPosts(team);
    }

    internal List<CommandPost> getFriendlyCommandPosts(string team)
    {
        return updater.getFriendlyCommandPosts(team);
    }

    public abstract void CloseSocket();

    protected abstract bool handleMessageLobby(IPEndPoint client, byte[] message);

    protected abstract bool handleMessageGame(IPEndPoint client, byte[] message);

    protected abstract bool handleMessageEndGame(IPEndPoint client, byte[] message);
}
