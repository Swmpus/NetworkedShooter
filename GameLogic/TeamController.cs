using System.Collections.Generic;
using UnityEngine;

public class TeamController // TODO maybe use a 'TeamPlaceholder' class for clientside stuff
{ // TODO updates will be run from CoreController, this may need to be a MonoBehaviour anyway but still update from ClientController
    private string name;
    private Color teamColour;
    private int teamLives;
    private int maxActiveMembers;

    public TeamController(string name, Color teamColour, int teamLives, int maxActiveMembers)
    {
        this.name = name;
        this.teamColour = teamColour;
        this.teamLives = teamLives;
        this.maxActiveMembers = maxActiveMembers;
    }

    public void RunTick(List<CommandPost> friendlyCommandPosts, List<CommandPost> enemyCommandPosts)
    {
        // Spawn any ai or players that need spawning (players always take precedence)
        // Update all ai with current enemy command posts / friendly command posts (allow them to decide what they want to do themselves)
        // Check if this team should stop playing
    }

    internal string GetName()
    {
        return name;
    }
}
