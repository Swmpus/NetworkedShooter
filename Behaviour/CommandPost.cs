using System.Collections.Generic;
using UnityEngine;

public class CommandPost : MonoBehaviour
{ // TODO reference team currently owning it and who is contesting it
    // TODO allow variable sized capture zones (and varying shapes?)
    //[SerializeField]
    //private List<Vector3> spawns; // TODO
    private byte ID;
    private CoreController cc;
    private string controllingTeam;
    private Dictionary<string, int> contestingTeams;
    private Renderer ringRenderer;
    private bool serverSide;

    void Start()
    {
        contestingTeams = new Dictionary<string, int>();
        foreach (string team in new List<string>(GameDefinitions.teams.Keys)) {
            contestingTeams.Add(team, 0);
        }
    }

    public void Init(byte ID, string team)
    {
        this.serverSide = GlobalInfo.IsServer;
        this.ID = ID;
        cc = GameObject.Find("GameMaster").GetComponent<CoreController>();
        controllingTeam = team;

        ringRenderer = transform.GetChild(0).GetComponent<Renderer>();
        if (team.Equals(GameDefinitions.neutralTeam.Key)) {
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.neutralTeam.Value);
        } else {
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.teams[team]);
        }
    }

    internal byte GetID()
    {
        return ID;
    }

    internal void SetController(string team)
    {
        controllingTeam = team;
        if (GameDefinitions.teams.ContainsKey(team)) {
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.teams[team]);
        } else if (team.Equals(GameDefinitions.neutralTeam.Key)) {
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.neutralTeam.Value);
        }
    }

    internal string GetController()
    {
        return controllingTeam;
    }

    public List<Vector3> GetSpawns(int count)
    {
        var outVar = new List<Vector3>();
        for (int i = 0; i < count; i++) {
            Vector2 circlePos = Random.insideUnitCircle * 5;
            outVar.Add(transform.position + new Vector3(circlePos.x, 0, circlePos.y));
        }
        return outVar;
        /*
        if (count <= spawns.Count) {
            return spawns.GetRange(0, count);
        } else {
            return spawns;
        }
        */
    }

    private void OnTriggerEnter(Collider other)
    { // TODO make sure that people dying inside the CP doesnt break it (maybe try maintaining a list of gameObjects contesting and checking if they are active and still inside when others contest)
        if (!serverSide) { return; }
        if (GameDefinitions.teams.ContainsKey(other.tag)) {
            contestingTeams[other.tag] += 1;
            checkContested();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!serverSide) { return; }
        if (GameDefinitions.teams.ContainsKey(other.tag)) {
            contestingTeams[other.tag] -= 1;
            checkContested();
        }
    }

    private void checkContested()
    {
        KeyValuePair<string, int> max;
        if (!GameDefinitions.teams.ContainsKey(controllingTeam)) {
            max = new KeyValuePair<string, int>(controllingTeam, -1);
        } else {
            max = new KeyValuePair<string, int>(controllingTeam, contestingTeams[controllingTeam]);
        }
        foreach (KeyValuePair<string, int> team in contestingTeams) {
            if (team.Value == 0 || team.Key.Equals(controllingTeam)) { continue; }
            if (team.Value == max.Value) {
                max = new KeyValuePair<string, int>(GameDefinitions.neutralTeam.Key, team.Value);
            } else if (team.Value > max.Value) {
                max = team;
            }
        }
        if (max.Key.Equals(GameDefinitions.neutralTeam.Key)) {
            controllingTeam = max.Key;
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.neutralTeam.Value);
            cc.SendCPChange(ID, controllingTeam);
        } else if (!max.Key.Equals(controllingTeam)) {
            controllingTeam = max.Key;
            ringRenderer.material.SetColor("_TintColor", GameDefinitions.teams[max.Key]);
            cc.SendCPChange(ID, controllingTeam);
        }
    }
}
