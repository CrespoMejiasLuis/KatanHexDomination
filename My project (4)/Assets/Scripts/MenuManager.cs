using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject sizeSelectionPanel;

    // Guardaremos la opci�n elegida
    public static int selectedBoardRadius = 10; // default

    public void OpenSizeSelectionPanel()
    {
        sizeSelectionPanel.SetActive(true);
    }

    public void SelectBoardSize(int radius)
    {
        selectedBoardRadius = radius;
        sizeSelectionPanel.SetActive(false);

        // Cambiar a la escena del MainScene
        SceneManager.LoadScene("Escena Luis");
    }

    public void ClosePanel()
    {
        sizeSelectionPanel.SetActive(false);
    }
}
