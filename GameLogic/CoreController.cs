using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UDPServer;

public class CoreController : MonoBehaviour
{ // TODO make many teams work properly
    private List<TeamController> teams;
    private static CoreController instance;
    private UDPServer server;
    public static TMP_Text LobbyPlayerList;
    public static TMP_Text VoteLabel;
    public static TMP_Text TeamLabel;
    public static GameObject CP;
    public static GameObject Bullet;
    public static GameObject CPSelector;
    public static GameObject ServerCamera;
    public static GameObject PlayerSpawner;
    public static GameObject PlayerInstance;
    public static GameObject PlayerPlaceholder;

    void Start()
    {
        getLobbyStatics();
        getPrefabs();
        if (GlobalInfo.IsServer) {
            server = new Server((ServerUpdater)ScriptableObject.CreateInstance("ServerUpdater"));
            teams = new List<TeamController>();
            foreach (string team in GameDefinitions.teams.Keys) {
                teams.Add(new TeamController(team, GameDefinitions.teams[team], 100, 10));
            }
        } else {
            server = new Client((ClientUpdater)ScriptableObject.CreateInstance("ClientUpdater"));
        }
    }

    private void FixedUpdate()
    {
        if (server == null) { return; }
        server.RunTick();
        /*
        if (teams == null || server.GetGameState() != GameState.GAME) { return; }
        foreach (TeamController team in teams) {
            team.RunTick(server.getFriendlyCommandPosts(team.GetName()), server.getEnemyCommandPosts(team.GetName()));
        }
        */
    }

    private void Awake()
    { // https://www.youtube.com/watch?v=5p2JlI7PV1w
        if (instance != null) {
            Destroy(gameObject);
        } else {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        server.CloseSocket();
    }

    internal void SendKill(GameObject gameObject)
    {
        ((Server)server).sendKill(gameObject);
    }

    public void ForceMapVote()
    {
        ((Server)server).ForceMapVote();
    }

    internal void SendPlayerSpawned(Vector3 spawn)
    {
        ((Client)server).sendPlayerSpawned(spawn);
    }

    public void SendMapVote()
    {
        ((Client)server).sendVote(VoteLabel.text);
    }

    public void SendJoinTeam()
    {
        ((Client)server).sendJoinTeam(TeamLabel.text);
    }

    public void SendMove(Vector3 newPosition, Vector3 newRotation)
    {
        ((Client)server).sendMove(newPosition, newRotation);
    }

    public void SendCPChange(byte ID, string controllingTeam)
    {
        ((Server)server).sendCPChange(ID, controllingTeam);
    }

    public void SendFire(Vector3 origin, Vector3 rotation)
    {
        // TODO remove the server check as server should not spawn any player characters to be played
        if (!GlobalInfo.IsServer) {
            ((Client)server).SendFire(origin, rotation);
        }
    }

    private void getLobbyStatics()
    {
        if (!GlobalInfo.IsServer) {
            VoteLabel = GameObject.Find("VoteOptionLabel").GetComponent<TMP_Text>();
            TeamLabel = GameObject.Find("TeamOptionLabel").GetComponent<TMP_Text>();
        }
        LobbyPlayerList = GameObject.Find("PlayerList").GetComponent<TMP_Text>();
    }

    private void getPrefabs()
    {
        Bullet = (GameObject)Resources.Load("Prefabs/Bullet");
        CP = (GameObject)Resources.Load("Prefabs/CommandPost");
        PlayerInstance = (GameObject)Resources.Load("Prefabs/Player");
        PlayerPlaceholder = (GameObject)Resources.Load("Prefabs/PlayerPlaceholder");
        PlayerSpawner = (GameObject)Resources.Load("Prefabs/PlayerSpawnController");
        ServerCamera = (GameObject)Resources.Load("Prefabs/ServerCamera");
        CPSelector = (GameObject)Resources.Load("Prefabs/CPSelector");
    }
}
