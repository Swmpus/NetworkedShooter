using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    private string team;

    private void FixedUpdate()
    { // Do AI stuff in here
        // Pathfind
    }

    public void SetTeam(string team)
    {
        this.team = team;
    }

    public void UpdateWorld(List<CommandPost> friendlyCommandPosts, List<CommandPost> enemyCommandPosts)
    {
        
    }
}
