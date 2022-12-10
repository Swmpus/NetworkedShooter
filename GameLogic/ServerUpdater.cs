using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UDPServer;

public class ServerUpdater : WorldUpdater
{ // TODO Consider simplifying the player list by simply regenerating the whole text each time
    public Dictionary<string, string> playerVotes;

    public ServerUpdater() : base()
    {
        playerVotes = new Dictionary<string, string>();
    }

    protected override void playerJoined(GameState state, string name)
    { // TODO initialise with defaults here rather than in initMap()
        if (state.Equals(GameState.LOBBY)) {
            playerVotes.Add(name, "none");
            Players.Add(name, new PlayerReference());
            CoreController.LobbyPlayerList.text += name + "(none)(none)\n";
        } else {
            throw new System.NotImplementedException();
        }
    }

    protected override GameState playerQuit(GameState state, string name)
    {
        if (state.Equals(GameState.LOBBY)) {
            playerVotes.Remove(name);
            Players.Remove(name);
            CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)\n", "");

            return checkVotes(false);
        } else {
            throw new System.NotImplementedException();
        }
    }

    protected override GameState playerVoted(GameState state, string name, string vote)
    {
        // -- Lobby --
            // Wait for at least 1 player to establish a connection
            // Wait for every player to send a map vote
            // Select a map / send map decision to all players
        if (state.Equals(GameState.LOBBY)) {
            bool force = name.Equals("Server");
            if (!force) {
                playerVotes[name] = vote;
                string matchText = Regex.Match(CoreController.LobbyPlayerList.text, name + "\\([a-zA-Z]*\\)").Value;
                CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)", matchText + "(" + vote + ")");
            }
            return checkVotes(force);
        } else {
            throw new System.NotImplementedException();
        }
    }

    protected override void playerJoinedTeam(GameState state, string name, string team)
    {
        if (state.Equals(GameState.LOBBY)) {
            Players[name].SetTeam(team);
            string wholeplayerListing = Regex.Match(CoreController.LobbyPlayerList.text, name + "\\([a-zA-Z]*\\)\\([a-zA-Z]*\\)\n").Value;
            string matchText = Regex.Match(wholeplayerListing, "\\([a-zA-Z]*\\)\n").Value;
            CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)\n", name + "(" + team + ")" + matchText);
        } else {
            throw new System.NotImplementedException();
        }
    }

    protected override void playerFired(GameState state, Vector3 origin, Vector3 rotation, string player)
    {
        if (state.Equals(GameState.GAME)) {
            GameObject bullet = Instantiate(CoreController.Bullet, origin, Quaternion.Euler(CoreController.Bullet.transform.rotation.eulerAngles + rotation));

            bullet.GetComponent<Bullet>().initialise(Players[player].team);
        }
    }

    protected override void playerSpawned(GameState state, Vector3 location, string name)
    {
        if (state.Equals(GameState.GAME)) {
            Players[name].Spawn(location);
        }
    }

    protected override void playerMoved(GameState state, Vector3 location, Vector3 rotation, string name)
    {
        if (state.Equals(GameState.GAME)) {
            Players[name].Move(location, rotation);
        }
    }

    private GameState checkVotes(bool force)
    {
        var decision = countVotes();
        if ((!playerVotes.ContainsValue("none") && playerVotes.Keys.Count > 0) || force) {
            if (decision.Count > 0) {
                CurrentMap = decision[0];
            } else {
                CurrentMap = "TestArena";
            }
            return GameState.GAME;
        } else {
            return GameState.LOBBY;
        }
    }

    protected override void CPChanged(GameState state, byte ID, string name)
    {
        throw new System.Exception("Server should not have CPChanged() called");
    }

    protected override void playerKilled(GameState state, string name)
    {
        Players[name].Kill();
    }

    private List<string> countVotes()
    {
        Dictionary<string, int> count = new Dictionary<string, int>();
        foreach (KeyValuePair<string, string> vote in playerVotes) {
            if (!vote.Value.Equals("none") && !count.ContainsKey(vote.Value)) {
                count.Add(vote.Value, 1);
            } else if (!vote.Value.Equals("none")) {
                count[vote.Value] += 1;
            }
        }
        var outVar = new List<string>();
        int maxVotes = 0;
        foreach (KeyValuePair<string, int> vote in count) {
            if (vote.Value > maxVotes) {
                outVar = new List<string>();
                outVar.Add(vote.Key);
                maxVotes = vote.Value;
            } else if (vote.Value == maxVotes) {
                outVar.Add(vote.Key);
            }
        }
        return outVar;
    }

    protected override void initMap(Scene scene, LoadSceneMode mode)
    { // TODO decide whether more than 3 teams are needed
        if (!(GameDefinitions.CPs.ContainsKey(scene.name))) { return; }

        foreach (PlayerReference player in Players.Values) {
            player.createObject();
        }

        int CPsPerTeam = (int)GameDefinitions.CPs[CurrentMap].Count / (int)GameDefinitions.teams.Count;
        int i = 0;

        byte IDCounter = 0;
        CPControllers = new List<CommandPost>();
        foreach (Vector3 CP in GameDefinitions.CPs[CurrentMap]) {
            GameObject CPInstance = Instantiate(CoreController.CP, CP, Quaternion.identity);
            CPControllers.Add(CPInstance.GetComponent<CommandPost>());

            if (CPsPerTeam > 0) { // TODO ensure this is deterministic or tell clients the results
                CPInstance.GetComponent<CommandPost>().Init(IDCounter, new List<string>(GameDefinitions.teams.Keys)[i]);
                i += 1;
                if (i == GameDefinitions.teams.Count) {
                    i = 0;
                    CPsPerTeam -= 1;
                }
            } else {
                CPInstance.GetComponent<CommandPost>().Init(IDCounter, GameDefinitions.neutralTeam.Key);
            }
            IDCounter += 1;
        }
        // Spawn the server observer
        var spawnLoc = new Vector3(0, 100, 0);
        var rotLoc = new Vector3(90, 0, -90);
        GameObject serverCam = Instantiate(CoreController.ServerCamera, spawnLoc, Quaternion.Euler(rotLoc));
    }
}
