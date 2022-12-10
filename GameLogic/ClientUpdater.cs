using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UDPServer;

public class ClientUpdater : WorldUpdater
{
    private PlayerSpawner spawner;
    public string ClientTeam;

    public ClientUpdater() : base() {}

    protected override void playerJoined(GameState state, string wholeNames)
    {
        string[] names = wholeNames.Split(':');
        if (state.Equals(GameState.LOBBY)) {
            foreach (string name in names) {
                if (name.Length > 0 && !Regex.IsMatch(CoreController.LobbyPlayerList.text, name + "(.*)(.*)\n")) {
                    CoreController.LobbyPlayerList.text += name + "(none)(none)\n";
                    Players.Add(name, new PlayerReference());
                }
            }
        } else if (state.Equals(GameState.LOBBY)) {
            return; // TODO maybe this is bad
        } else {
            throw new NotImplementedException();
        }
    }

    protected override GameState playerQuit(GameState state, string name)
    {
        if (state.Equals(GameState.LOBBY)) {
            CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)\n", "");
            Players.Remove(name);
        } else {
            throw new NotImplementedException();
        }
        return state;
    }

    protected override GameState playerVoted(GameState state, string name, string vote)
    { // TODO not use a magic string for the server name
        if (state.Equals(GameState.LOBBY) && name.Equals("Server")) {
            CurrentMap = vote;
            return GameState.GAME;
        } else if (state.Equals(GameState.LOBBY)) {
            string matchText = Regex.Match(CoreController.LobbyPlayerList.text, name + "\\([a-zA-Z]*\\)").Value;
            CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)", matchText + "(" + vote + ")");
            return GameState.LOBBY; // TODO show other player's votes in lobby
        }
        throw new Exception("Unreachable");
    }

    protected override void playerJoinedTeam(GameState state, string name, string team)
    {
        if (state.Equals(GameState.LOBBY)) {
            string wholeplayerListing = Regex.Match(CoreController.LobbyPlayerList.text, name + "\\([a-zA-Z]*\\)\\([a-zA-Z]*\\)\n").Value;
            string matchText = Regex.Match(wholeplayerListing, "\\([a-zA-Z]*\\)\n").Value;
            CoreController.LobbyPlayerList.text = Regex.Replace(CoreController.LobbyPlayerList.text, name + "(.*)(.*)\n", name + "(" + team + ")" + matchText);
            Players[name].SetTeam(team);
        } else {
            throw new NotImplementedException();
        }
    }

    protected override void playerFired(GameState state, Vector3 origin, Vector3 rotation, string player)
    {
        if (state.Equals(GameState.GAME)) {
            GameObject bullet = Instantiate(CoreController.Bullet, origin, Quaternion.Euler(CoreController.Bullet.transform.rotation.eulerAngles + rotation));

            if (player.Equals(GlobalInfo.UserName)) {
                bullet.GetComponent<Bullet>().initialise(ClientTeam);
            } else {
                bullet.GetComponent<Bullet>().initialise(Players[player].team);
            }
        }
    }

    protected override void playerSpawned(GameState state, Vector3 location, string name)
    {   // TODO Simply enable and disable objects and move them to the correct location without creating new ones all the time
        // This may have the added benefit of maintaining some player state information contained within the object's scripts later on
        if (state.Equals(GameState.GAME) && name.Equals(GlobalInfo.UserName)) {
            Debug.Log("BIG BOOBOO");
            GameObject placeHolder = Instantiate(
                                        CoreController.PlayerInstance,
                                        location + CoreController.PlayerPlaceholder.transform.position,
                                        CoreController.PlayerPlaceholder.transform.rotation);
        } else
        if (state.Equals(GameState.GAME)) {
            Players[name].Spawn(location);
        }
    }

    protected override void playerKilled(GameState state, string name)
    {
        if (name.Equals(GlobalInfo.UserName)) {
            spawner.KillPlayer();
        } else {
            Players[name].Kill();
        }
    }

    protected override void playerMoved(GameState state, Vector3 location, Vector3 rotation, string name)
    {
        if (state.Equals(GameState.GAME)) {
            Players[name].Move(location, rotation);
        }
    }

    protected override void CPChanged(GameState state, byte ID, string team)
    {
        foreach (CommandPost CP in CPControllers) {
            if (CP.GetID() == ID) {
                CP.SetController(team);
            }
        }
        spawner.UpdateCPs(CPControllers);
    }

    protected override void initMap(Scene scene, LoadSceneMode mode)
    { // TODO implement this function
        // TODO make bullets shoot the correct team colour
        CPControllers = new List<CommandPost>();
        if (!(GameDefinitions.CPs.ContainsKey(scene.name))) { return; }

        foreach (PlayerReference player in Players.Values) {
            if (player.team == null) { // TODO test and try to remove
                throw new Exception("Player did not decide on a team");
            }
            player.createObject();
        }

        int CPsPerTeam = (int)GameDefinitions.CPs[CurrentMap].Count / (int)GameDefinitions.teams.Count;
        int i = 0;

        byte IDCounter = 0;
        //CPs = new List<GameObject>();
        foreach (Vector3 CP in GameDefinitions.CPs[CurrentMap]) {
            GameObject CPInstance = Instantiate(CoreController.CP, CP, Quaternion.identity);
            CPControllers.Add(CPInstance.GetComponent<CommandPost>());

            if (CPsPerTeam > 0) {
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
        // Instantiate the spawn controller
        var spawnLoc = new Vector3(0, 100, 0);
        var rotLoc = new Vector3(90, 0, -90);
        spawner = Instantiate(CoreController.PlayerSpawner, spawnLoc, Quaternion.Euler(rotLoc)).GetComponent<PlayerSpawner>();
        spawner.Init(ClientTeam, CPControllers);
    }
}
