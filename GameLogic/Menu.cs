using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void JoinServer()
    {
        if (GlobalInfo.ServerIP == null || GlobalInfo.ServerIP.Length == 0) {
            return;
        }
        SceneManager.LoadScene("ClientLobby");
    }

    public void StartServer()
    {
        GlobalInfo.IsServer = true;
        SceneManager.LoadScene("ServerLobby");
    }

    public void UpdateUserName(string name)
    {
        GlobalInfo.UserName = name;
    }

    public void UpdateServerIP(string newIP)
    {
        GlobalInfo.ServerIP = newIP;
    }
}
