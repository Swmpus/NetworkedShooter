using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private Dictionary<GameObject, CommandPost> CPSelectors;
    private GameObject playerInstance;
    private Vector2 mousePos;
    private string team;
    private CoreController cc;

    void Start()
    {
        // Create player instance
        playerInstance = Instantiate(CoreController.PlayerInstance);
        playerInstance.SetActive(false);
    }

    public void Init(string team, List<CommandPost> CPs)
    {
        this.team = team;
        this.cc = GameObject.Find("GameMaster").GetComponent<CoreController>();

        CPSelectors = new Dictionary<GameObject, CommandPost>();
        foreach (CommandPost CP in CPs) {
            GameObject selector = Instantiate(CoreController.CPSelector, CP.transform.position + (15 * Vector3.up), Quaternion.identity);
            if (GameDefinitions.teams.ContainsKey(CP.GetController())) {
                selector.GetComponent<Renderer>().material.SetColor("_Color", GameDefinitions.teams[CP.GetController()]);
            } else {
                selector.GetComponent<Renderer>().material.SetColor("_Color", GameDefinitions.neutralTeam.Value);
            }
            CPSelectors.Add(selector, CP);
        }
    }

    public void UpdateCPs(List<CommandPost> CPs)
    { // TODO Feels like there is a better way to do this
        foreach (KeyValuePair<GameObject, CommandPost> selector in CPSelectors) {
            foreach (CommandPost CP in CPs) {
                if (CP.GetID().Equals(selector.Value.GetID())) {
                    if (GameDefinitions.teams.ContainsKey(CP.GetController())) {
                        selector.Key.GetComponent<Renderer>().material.SetColor("_Color", GameDefinitions.teams[CP.GetController()]);
                    } else {
                        selector.Key.GetComponent<Renderer>().material.SetColor("_Color", GameDefinitions.neutralTeam.Value);
                    }
                    //CPSelectors[selector.Key] = CP;
                    break;
                }
            }
        }
    }

    internal void SetMousePos(Vector2 pos)
    {
        mousePos = pos;
    }

    public void Click()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out hit, 100.0f)) {
            if (CPSelectors.ContainsKey(hit.collider.gameObject) && CPSelectors[hit.collider.gameObject].GetController().Equals(team)) {
                spawnPlayer(CPSelectors[hit.collider.gameObject]);
            }
        }
    }
    
    private void spawnPlayer(CommandPost CP)
    { // TODO ensure that this can be re enabled in the player's OnDestroy() method
        Vector3 spawn = CP.GetSpawns(1)[0];
        cc.SendPlayerSpawned(spawn);

        foreach (GameObject sphere in CPSelectors.Keys) { sphere.SetActive(false); }

        playerInstance.transform.position = spawn + CoreController.PlayerInstance.transform.position;
        gameObject.SetActive(false);
        playerInstance.SetActive(true);
    }

    public void KillPlayer()
    {
        playerInstance.SetActive(false);
        foreach (GameObject sphere in CPSelectors.Keys) { sphere.SetActive(true); }
        gameObject.SetActive(true);
    }
}
