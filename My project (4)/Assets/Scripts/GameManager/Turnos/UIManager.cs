using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

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
    public TextMeshProUGUI unitRangeText;
    public TextMeshProUGUI unitAttackText;

    // Botones de acción
    public Button actionAttackButton;
    public Button actionMoveButton;
    public Button actionPassButton;
    public Button actionSpecialButton;
    public TextMeshProUGUI actionSpecialButtonText;

    // Referencia a la unidad seleccionada (del tipo correcto 'Unit')
    private Unit selectedUnit = null;

    [Header("Panel de Construcción (Poblado)")]
    public GameObject constructionPanelContainer; // Contenedor del menú del poblado
    public Button buildRoadButton;
    public Button buildSettlementButton;
    public Button upgradeCityButton;
    public Button recruitArtilleroButton;
    public Button recruitCaballeroButton;
    public Button recruitCaballeriaButton;
    public Button recruitColonoButton;

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

        if (constructionPanelContainer != null) constructionPanelContainer.SetActive(false);
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

        // Inicialización de la UI al empezar el juego
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PlayerTurn)
        {
            ShowPlayerUI();
        }
    }

    // --- Suscripción a Eventos ---
    void OnEnable()
    {
        // Eventos de Turno
        GameManager.OnPlayerTurnStart += ShowPlayerUI;
        GameManager.OnAITurnStart += HidePlayerUI;

        // Eventos de Recursos
        Player.OnPlayerResourcesUpdated += UpdateResourceTexts;
        Player.OnPlayerVictoryPointsUpdated += UpdateVictoryPointsText;

        // Eventos de Selección (¡Usando 'Unit'!)
        GameManager.OnUnitSelected += ShowUnitPanel;
        GameManager.OnDeselected += HideUnitPanel;
    }

    void OnDisable()
    {
        // Darse de baja de todos los eventos
        GameManager.OnPlayerTurnStart -= ShowPlayerUI;
        GameManager.OnAITurnStart -= HidePlayerUI;

        Player.OnPlayerResourcesUpdated -= UpdateResourceTexts;
        Player.OnPlayerVictoryPointsUpdated -= UpdateVictoryPointsText;

        GameManager.OnUnitSelected -= ShowUnitPanel;
        GameManager.OnDeselected -= HideUnitPanel;
    }

    // --- Métodos de Actualización de la UI Superior ---

    public void UpdateResourceTexts(Dictionary<ResourceType, int> resources)
    {
        if (resources == null) return;

        if (woodAmountText != null && resources.ContainsKey(ResourceType.Madera))
            woodAmountText.text = resources[ResourceType.Madera].ToString();
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
        HideConstructionPanel(); // Asegurarse de cerrar todo al pasar turno
    }

    public void OnEndTurnButtonPressed()
    {
        GameManager.Instance.EndPlayerTurn();
    }

    // --- Lógica de Panel de Unidad (UI Inferior) ---

    /// <summary>
    /// Esta función es llamada por el evento GameManager.OnUnitSelected
    /// y acepta una 'Unit'.
    /// </summary>
    public void ShowUnitPanel(Unit unit)
    {
        // 1. COMPROBAR SI ES UN POBLADO (Lógica especial)
        SettlementUnit pobladoLogic = unit.GetComponent<SettlementUnit>();
        if (pobladoLogic != null)
        {
            // Solo abrimos el menú si es NUESTRO poblado
            if (unit.ownerID == 0) // Asumimos 0 = Humano
            {
                pobladoLogic.OpenTradeMenu();
                Debug.Log("Poblado aliado seleccionado. Abriendo menú de construcción.");
                return; // No mostramos el panel de unidad estándar
            }
            // Si es un poblado enemigo, continuará y mostrará el panel de stats
        }

        // 2. ES UNA UNIDAD MÓVIL (O UN POBLADO ENEMIGO)
        selectedUnit = unit;
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(true);
        }

        // 3. ACTUALIZAR STATS (Se muestra para aliados y enemigos)
        if (unit.statsBase == null)
        {
            Debug.LogError($"¡La unidad {unit.name} no tiene UnitStats asignado!");
            return;
        }

        // ¡CORREGIDO! Leemos de 'statsBase', 'vidaActual' y 'movimientosRestantes'
        unitNameText.text = unit.statsBase.nombreUnidad.ToString();
        unitHealthText.text = $"Vida: {unit.vidaActual}/{unit.statsBase.vidaMaxima}";
        unitMovementText.text = $"Movimiento: {unit.movimientosRestantes}/{unit.statsBase.puntosMovimiento}";
        if (unitRangeText != null) unitRangeText.text = $"Rango: {unit.statsBase.rangoAtaque}";
        if (unitAttackText != null) unitAttackText.text = $"Ataque: {unit.statsBase.ataque}";

        // --- 4. Lógica de Botones Basada en el Dueño ---

        // Comprobar si la unidad es del jugador (asumimos ID 0)
        if (unit.ownerID == 0)
        {
            // ES NUESTRA: Mostrar y configurar botones
            actionAttackButton.gameObject.SetActive(true);
            actionMoveButton.gameObject.SetActive(true);
            if (actionPassButton != null) actionPassButton.gameObject.SetActive(true);

            // Configurar interactividad
            actionAttackButton.interactable = unit.statsBase.ataque > 0;
            actionMoveButton.interactable = unit.movimientosRestantes > 0;
            if (actionPassButton != null) actionPassButton.interactable = true;

            // Configurar Botón Especial (Colono)
            UnitBuilder colonoLogic = unit.GetComponent<UnitBuilder>();
            actionSpecialButton.onClick.RemoveAllListeners(); // Limpiar listeners antiguos

            if (colonoLogic != null)
            {
                actionSpecialButton.gameObject.SetActive(true);
                actionSpecialButtonText.text = "Construir";
                actionSpecialButton.interactable = true; // El UnitBuilder comprobará los recursos

                // Conectamos el botón al SimpleClickTester
                SimpleClickTester clickTester = FindObjectOfType<SimpleClickTester>();
                if (clickTester != null)
                {
                    actionSpecialButton.onClick.AddListener(clickTester.BotonConstruirPulsado);
                }
            }
            else
            {
                // No es un colono, ocultar botón especial
                actionSpecialButton.gameObject.SetActive(false);
            }
        }
        else
        {
            // ES ENEMIGA: Ocultar TODOS los botones de acción
            actionAttackButton.gameObject.SetActive(false);
            actionMoveButton.gameObject.SetActive(false);
            actionSpecialButton.gameObject.SetActive(false);
            if (actionPassButton != null)
            {
                actionPassButton.gameObject.SetActive(false);
            }
        }
    }

    public void HideConstructionPanel()
    {
        if (constructionPanelContainer != null)
        {
            constructionPanelContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Esta función es llamada por el evento GameManager.OnDeselected
    /// </summary>
    public void HideUnitPanel()
    {
        selectedUnit = null; // Limpiar la referencia
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(false);
        }
    }

    // ... (Tu función ConfigureButton (comentada) y el resto del código) ...
    // ... (Asegúrate de copiarla de tu archivo si la necesitas) ...

private void ConfigureButton(
        Button button,
        Dictionary<ResourceType, int> cost,
        Player player,
        Func<bool> positionCheck, // Delegate para lógica de posición (si aplica)
        Action action) // Delegate para la acción a ejecutar
    {
        if (button == null) return;

        bool canAfford = player.CanAfford(cost);
        bool isValidPosition = positionCheck.Invoke();

        // El botón es interactuable solo si se tienen recursos Y la posición es válida
        button.interactable = canAfford && isValidPosition;

        button.onClick.RemoveAllListeners();

        // Solo añadimos el listener si el botón es potencialmente interactuable (buena práctica)
        if (canAfford)
        {
            button.onClick.AddListener(() => {
                // Se puede añadir un chequeo final de posición aquí antes de llamar a la acción
                if (isValidPosition)
                {
                    action.Invoke();
                    HideConstructionPanel(); // Cerrar el panel después de la acción
                }
            });
        }
    }


}

