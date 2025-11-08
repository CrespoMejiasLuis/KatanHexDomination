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
        // Conectar el botón de "Terminar Turno" al GameManager
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonPressed);
        }

        // Asegurarse de que esté oculto al empezar
        HidePlayerUI();
    }

    // --- Suscripción a Eventos ---

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
        // Aquí también actualizarías los textos de recursos del jugador
        // UpdateResourceTexts(GameManager.Instance.PlayerInventory);
    }

    private void HidePlayerUI()
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }
        // Aquí podrías mostrar "Turno de la IA..."
    }

    /// <summary>
    /// Esta función es llamada por el botón de la UI.
    /// Simplemente le dice al GameManager que el jugador quiere terminar su turno.
    /// </summary>
    private void OnEndTurnButtonPressed()
    {
        // Llama a la función pública del Singleton GameManager
        GameManager.Instance.EndPlayerTurn();
    }
    
}