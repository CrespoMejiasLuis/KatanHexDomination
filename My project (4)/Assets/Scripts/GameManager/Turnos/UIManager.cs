using UnityEngine;
using UnityEngine.UI; // Necesario para Button, Text, etc.

public class UIManager : MonoBehaviour
{
    [Header("Componentes de UI")]
    public Button endTurnButton;
    // public TextMeshProUGUI woodAmountText; // Ejemplo para mostrar recursos
    // public TextMeshProUGUI stoneAmountText; // Ejemplo para mostrar recursos

    void Start()
    {
        // Conectar el bot�n de "Terminar Turno" al GameManager
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonPressed);
        }

        GameManager.OnPlayerTurnStart += ShowPlayerUI;
        GameManager.OnAITurnStart += HidePlayerUI;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PlayerTurn)
        {
            ShowPlayerUI();
        }
    }

    // --- Suscripci�n a Eventos ---

    void OnEnable()
    {
        GameManager.OnPlayerTurnStart += ShowPlayerUI;
        GameManager.OnAITurnStart += HidePlayerUI;
    }

    void OnDisable()
    {
        GameManager.OnPlayerTurnStart -= ShowPlayerUI;
        GameManager.OnAITurnStart -= HidePlayerUI;
    }

    // --- Controladores de Eventos ---

    private void ShowPlayerUI()
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
        }
        // Aqu� tambi�n actualizar�as los textos de recursos del jugador
        // UpdateResourceTexts(GameManager.Instance.PlayerInventory);
    }

    private void HidePlayerUI()
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }
        // Aqu� podr�as mostrar "Turno de la IA..."
    }

    /// <summary>
    /// Esta funci�n es llamada por el bot�n de la UI.
    /// Simplemente le dice al GameManager que el jugador quiere terminar su turno.
    /// </summary>
    public void OnEndTurnButtonPressed()
    {
        // Llama a la funci�n p�blica del Singleton GameManager
        GameManager.Instance.EndPlayerTurn();
    }
    
}