using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

// Nota: Necesitas definir el enum ResourceType en un archivo separado.

public class UIManager : MonoBehaviour
{
    // Singleton para acceso global
    public static UIManager Instance { get; private set; }

    [Header("Panel Superior: Componentes Globales")]
    public Button endTurnButton;
    public TextMeshProUGUI victoryPointsText;

    [Header("Panel Superior: Recursos")]
    public TextMeshProUGUI woodAmountText;
    public TextMeshProUGUI stoneAmountText;
    public TextMeshProUGUI wheatAmountText;
    public TextMeshProUGUI clayAmountText;
    public TextMeshProUGUI sheepAmountText;

    // --- Panel Inferior: Unidad Seleccionada ---
    [Header("Panel Inferior: Unidad Seleccionada")]
    public GameObject unitPanelContainer;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitHealthText;
    public TextMeshProUGUI unitMovementText;

    // Botones de acción
    public Button actionAttackButton;
    public Button actionMoveButton;
    public Button actionSpecialButton;
    public TextMeshProUGUI actionSpecialButtonText;

    // Usaremos 'Component' temporalmente si UnitBase no existe.
    // Una vez que UnitBase exista, cambia 'Component' a 'UnitBase'.
    private UnitBase selectedUnit = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonPressed);
        }

        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(false);
        }

        // Inicialización de la UI al empezar el juego (si es el turno del jugador)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PlayerTurn)
        {
            ShowPlayerUI();
        }
    }

    // --- Suscripción a Eventos ---
    void OnEnable()
    {
        GameManager.OnPlayerTurnStart += ShowPlayerUI;
        GameManager.OnAITurnStart += HidePlayerUI;

        
        Player.OnPlayerResourcesUpdated += UpdateResourceTexts;
         Player.OnPlayerVictoryPointsUpdated += UpdateVictoryPointsText; // Descomentar al implementar

        GameManager.OnUnitSelected += ShowUnitPanel;
        GameManager.OnDeselected += HideUnitPanel;
    }

    void OnDisable()
    {
        GameManager.OnPlayerTurnStart -= ShowPlayerUI;
        GameManager.OnAITurnStart -= HidePlayerUI;

        Player.OnPlayerResourcesUpdated -= UpdateResourceTexts;
         Player.OnPlayerVictoryPointsUpdated -= UpdateVictoryPointsText;

        //GameManager.OnUnitSelected -= HideUnitPanel; // No es 'ShowUnitPanel'
        GameManager.OnDeselected -= HideUnitPanel;
    }

    // --- Métodos de Actualización de la UI Superior (Recursos y PV) ---

    public void UpdateResourceTexts(Dictionary<ResourceType, int> resources)
    {
        if (resources == null) return;

        if (woodAmountText != null && resources.ContainsKey(ResourceType.Madera))
            woodAmountText.text = resources[ResourceType.Madera].ToString();

        // ✅ CORRECCIÓN: Usar ResourceType.Piedra o el nombre correcto de tu enum.
        // Si tu enum usa 'Roca', está bien, pero 'Piedra' era el que discutimos como estándar.
        // Asumiendo que tu enum actualmente tiene 'Roca':
        if (stoneAmountText != null && resources.ContainsKey(ResourceType.Roca))
            stoneAmountText.text = resources[ResourceType.Roca].ToString();

        if (wheatAmountText != null && resources.ContainsKey(ResourceType.Trigo))
            wheatAmountText.text = resources[ResourceType.Trigo].ToString();

        if (clayAmountText != null && resources.ContainsKey(ResourceType.Arcilla))
            clayAmountText.text = resources[ResourceType.Arcilla].ToString();

        if (sheepAmountText != null && resources.ContainsKey(ResourceType.Oveja))
            sheepAmountText.text = resources[ResourceType.Oveja].ToString();
    }

    public void UpdateVictoryPointsText(int points)
    {
        if (victoryPointsText != null)
        {
            victoryPointsText.text = points.ToString() + " / 10 PV";
        }
    }

    // --- Controladores de Eventos de Turno ---
    private void ShowPlayerUI()
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
        }
    }

    private void HidePlayerUI()
    {
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }
        HideUnitPanel();
    }

    public void OnEndTurnButtonPressed()
    {
        GameManager.Instance.EndPlayerTurn();
    }

    // --- Lógica de Panel de Unidad (UI Inferior) ---

    public void ShowUnitPanel(UnitBase unit)
    {
        selectedUnit = unit;
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(true);
        }

     
        // --- 1. Actualizar Stats ---
        unitNameText.text = unit.UnitName;
        unitHealthText.text = $"Vida: {unit.CurrentHealth}/{unit.MaxHealth}";
        unitMovementText.text = $"Movimiento: {unit.MovementPointsRemaining}/{unit.MaxMovementPoints}";

        // --- 2. Configurar Botones Estándar ---
        actionAttackButton.interactable = unit.CanAttack();
        actionMoveButton.interactable = unit.MovementPointsRemaining > 0;

        actionSpecialButton.onClick.RemoveAllListeners();
        // -------------------------------------------------------------
        // ❌ TEMPORALMENTE COMENTADO: DESCOMENTAR AL TENER CLASE UNITBASE
        // -------------------------------------------------------------

        /*

        // --- 3. Configurar Acción Especial (Lógica de subclases) ---
        if (unit is ColonoUnit colono) 
        {
            actionSpecialButton.gameObject.SetActive(true);
            actionSpecialButtonText.text = "Fundar Poblado";

            bool canPlace = colono.CanPlaceSettlement() && GameManager.Instance.ActivePlayer.HasCostForSettlement();

            actionSpecialButton.interactable = canPlace;
            actionSpecialButton.onClick.AddListener(() => GameManager.Instance.ActionFundarPoblado(colono));
        }
        else if (unit is CaballeroUnit)
        {
            actionSpecialButton.gameObject.SetActive(true);
            actionSpecialButtonText.text = "Fortificar";
            actionSpecialButton.interactable = true;
            actionSpecialButton.onClick.AddListener(() => GameManager.Instance.ActionFortificar(unit));
        }
        else if (unit is ArtilleroUnit artillero) // Asume que ArtilleroUnit hereda de UnitBase
        {
            actionSpecialButton.gameObject.SetActive(true);
            actionSpecialButtonText.text = "Preparar Asedio"; // Nombre de la habilidad
    
            // Asumimos que Asedio solo se puede hacer si no ha movido
            bool canSiege = artillero.MovementPointsRemaining == artillero.MaxMovementPoints; 
    
            actionSpecialButton.interactable = canSiege;
            // Conectar el botón a la acción del GameManager
            actionSpecialButton.onClick.AddListener(() => GameManager.Instance.ActionPrepararAsedio(artillero));
        }
        else
        {
            actionSpecialButton.gameObject.SetActive(false);
        }
        */

        // -------------------------------------------------------------
        //  FIN DE BLOQUE COMENTADO TEMPORALMENTE
        // -------------------------------------------------------------
    }

    public void HideUnitPanel()
    {
        selectedUnit = null; // Limpiar la referencia
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(false);
        }
    }
}

