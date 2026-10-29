using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void loadLobby()
    {
        // Load the lobby scene
        SceneManager.LoadScene("Lobby");
    }
}
