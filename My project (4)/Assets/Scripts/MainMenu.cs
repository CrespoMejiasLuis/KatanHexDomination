using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void Salir()
    {
        Application.Quit();
    }
}
