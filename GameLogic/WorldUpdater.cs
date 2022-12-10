using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MessageBuilder;
using static UDPServer;

public abstract class WorldUpdater : ScriptableObject
{
    public Dictionary<string, PlayerReference> Players;
    protected List<CommandPost> CPControllers;
    private List<BufferItem> buffer;
    private bool bufferFlag;
    public string CurrentMap;

    public WorldUpdater()
    {
        SceneManager.sceneLoaded += initMap; // TODO change to initMap() and add functionality?
        Players = new Dictionary<string, PlayerReference>();
        buffer = new List<BufferItem>();
        bufferFlag = false;
    }

    public GameState UpdateWorld(GameState state)
    { // TODO Handle on different threads for performance?
        // TODO check which methods defined in this class could actually be kept in this class to reduce code duplication
        if (bufferFlag) {
            BufferItem[] copy;
            lock (buffer) {
                copy = new BufferItem[buffer.Count];
                buffer.CopyTo(copy);
                buffer.Clear();
                bufferFlag = false;
            }
            foreach (BufferItem item in copy) {
                switch (item.type) {
                    case MessageType.JOIN:
                        playerJoined(item.state, (string)item.data[0]);
                        break;
                    case MessageType.QUIT:
                        playerQuit(item.state, (string)item.data[0]);
                        break;
                    case MessageType.VOTE:
                        state = playerVoted(item.state, (string)item.data[0], (string)item.data[1]);
                        break;
                    case MessageType.FIRE:
                        playerFired(item.state, (Vector3)item.data[0], (Vector3)item.data[1], (string)item.data[2]);
                        break;
                    case MessageType.JOINTEAM:
                        playerJoinedTeam(item.state, (string)item.data[0], (string)item.data[1]);
                        break;
                    case MessageType.SPAWN:
                        playerSpawned(item.state, (Vector3)item.data[0], (string)item.data[1]);
                        break;
                    case MessageType.MOVE:
                        playerMoved(item.state, (Vector3)item.data[0], (Vector3)item.data[1], (string)item.data[2]);
                        break;
                    case MessageType.CPChange:
                        CPChanged(item.state, (byte)item.data[0], (string)item.data[1]);
                        break;
                    case MessageType.KILL:
                        playerKilled(item.state, (string)item.data[0]);
                        break;
                    default:
                        throw new Exception("Incorrect type supplied to world updater buffer");
                }
            }
        }
        if (state == GameState.GAME) {
            foreach (PlayerReference player in Players.Values) {
                player.interpolate();
            }
        }
        return state;
    }

    internal string getPlayerName(GameObject placeholder)
    {
        foreach (KeyValuePair<string, PlayerReference> player in Players) {
            if (player.Value.ComparePlaceholder(placeholder)) {
                return player.Key;
            }
        }
        return null;
    }

    internal List<CommandPost> getFriendlyCommandPosts(string team)
    {
        var outvar = new List<CommandPost>();
        foreach (CommandPost CP in CPControllers) {
            if (CP.GetController().Equals(team)) {
                outvar.Add(CP);
            }
        }
        return outvar;
    }

    internal List<CommandPost> getEnemyCommandPosts(string team)
    {
        var outvar = new List<CommandPost>();
        foreach (CommandPost CP in CPControllers) {
            if (!CP.GetController().Equals(team)) {
                outvar.Add(CP);
            }
        }
        return outvar;
    }

    protected abstract void initMap(Scene scene, LoadSceneMode mode);

    protected abstract void playerJoined(GameState state, string name);

    protected abstract GameState playerQuit(GameState state, string name);

    protected abstract GameState playerVoted(GameState state, string name, string vote);

    protected abstract void playerJoinedTeam(GameState state, string name, string team);

    protected abstract void playerSpawned(GameState state, Vector3 location, string name);

    protected abstract void CPChanged(GameState state, byte ID, string name);

    protected abstract void playerMoved(GameState state, Vector3 location, Vector3 rotation, string name);

    protected abstract void playerFired(GameState state, Vector3 origin, Vector3 rotation, string name);

    protected abstract void playerKilled(GameState state, string name);

    public void addToQueue(GameState state, MessageType type, object[] data)
    {
        lock (buffer) {
            buffer.Add(new BufferItem(state, type, data));
            bufferFlag = true;
        }
    }

    private class BufferItem
    {
        public GameState state;
        public MessageType type;
        public object[] data;

        public BufferItem(GameState state, MessageType type, object[] data)
        {
            this.state = state;
            this.type = type;
            this.data = data;
        }
    }

    public class PlayerReference
    { // This is defined here to allow inheriting classes to call the interpret method that will be the same in servers and clients
        public string team;
        private GameObject placeHolder;
        private Rigidbody body;
        private Vector3 oldPos;
        private Vector3 newPos;
        private float moveSpeed = 5f;

        public PlayerReference() { }

        public void createObject()
        {
            if (placeHolder == null) {
                placeHolder = Instantiate(CoreController.PlayerPlaceholder, CoreController.PlayerPlaceholder.transform.position, CoreController.PlayerPlaceholder.transform.rotation);
                var renderer = placeHolder.GetComponent<Renderer>();
                placeHolder.tag = team;
                renderer.material.SetColor("_Color", GameDefinitions.teams[team]);
                body = placeHolder.GetComponent<Rigidbody>();
                placeHolder.SetActive(false);
            }
        }

        public void Kill()
        {
            placeHolder.SetActive(false);
        }

        public void Spawn(Vector3 location)
        {
            oldPos = location;
            newPos = location;
            placeHolder.transform.position = location;
            placeHolder.SetActive(true);
        }

        public void Move(Vector3 location, Vector3 rotation)
        {
            placeHolder.transform.eulerAngles = rotation;
            setPosition(location, rotation);
        }

        public void SetTeam(string team)
        {
            this.team = team;
        }

        public bool ComparePlaceholder(GameObject placeholder)
        { // TODO do this better?
            return placeholder == placeHolder;
        }

        internal void setPosition(Vector3 location, Vector3 rotation)
        {
            oldPos = newPos;
            newPos = location;
            placeHolder.transform.position = newPos;
            placeHolder.transform.eulerAngles = rotation;
        }

        internal void interpolate()
        {
            if (placeHolder != null) {
                body.velocity = calculateDirection() * moveSpeed;
            }
        }

        private Vector3 calculateDirection()
        {
            Vector3 actual = newPos - oldPos;
            //Debug.DrawRay(placeHolder.transform.position, actual);
            return actual.magnitude < 0.3f ? Vector3.zero : actual.normalized;
        }
    }
}
