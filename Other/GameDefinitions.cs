using System.Collections.Generic;
using UnityEngine;

public static class GameDefinitions
{ // TODO use this to populate the menu lists etc
    public static Dictionary<string, List<Vector3>> CPs = new Dictionary<string, List<Vector3>>
    { // Order of CPs is important as the last ones are more likely to be neutral at the start of the game
        { "TestArena", new List<Vector3> { new Vector3(-40, 0, -40), new Vector3(-40, 0, 40), new Vector3(40, 0, -40), new Vector3(40, 0, 40), new Vector3(5, 0, 0) } },
        { "War Torn Village", new List<Vector3> { new Vector3(14.641f, 0, -54.641f), new Vector3(-54.641f, 0, 14.641f), new Vector3(40, 0, 40), new Vector3(0, 0, 0) } }
    }; // MapName -> CPPositions TODO figure out how to incorporate spawn positions

    public static Dictionary<string, Color> teams = new Dictionary<string, Color>
    {
        { "Blue", Color.blue },
        { "Green", Color.green },
        { "Yellow", Color.yellow }
    };

    public static KeyValuePair<string, Color> neutralTeam = new KeyValuePair<string, Color>("none", Color.grey);
}
