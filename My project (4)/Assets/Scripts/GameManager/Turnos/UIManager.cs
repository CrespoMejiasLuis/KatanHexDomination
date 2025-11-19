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
    public TextMeshProUGUI unitRangeText;
    public TextMeshProUGUI unitAttackText;

    // Botones de acción
    public Button actionAttackButton;    
    public Button actionMoveButton;        
    public Button actionPassButton;
    public Button actionSpecialButton;
    public Button accionSaquear;
    public TextMeshProUGUI actionSpecialButtonText;

    // Usaremos 'Component' temporalmente si UnitBase no existe.
    // Una vez que UnitBase exista, cambia 'Component' a 'UnitBase'.
    private Unit selectedUnit = null;

    [Header("Panel de Construcción")]
    public GameObject constructionPanelContainer; // Contenedor del nuevo panel
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
        
        if(constructionPanelContainer!=null) constructionPanelContainer.SetActive(false);
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
        HideConstructionPanel();
    }

    public void OnEndTurnButtonPressed()
    {
        GameManager.Instance.EndPlayerTurn();
    }

    // --- Lógica de Panel de Unidad (UI Inferior) ---

    public void ShowUnitPanel(Unit unit)
    {
        SettlementUnit pobladoLogic = unit.GetComponent<SettlementUnit>();
        if (pobladoLogic != null)
        {
            if (unit.ownerID == 0) // Asumimos 0 = Humano
            {
                pobladoLogic.OpenTradeMenu();
                Debug.Log("Poblado aliado seleccionado. Abriendo menú de construcción.");
                return; // No mostramos el panel de unidad estándar
            }
        }
        selectedUnit = unit;
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(true);
        }

        if (unit.statsBase == null)
        {
            Debug.LogError($"¡La unidad {unit.name} no tiene UnitStats asignado!");
            return;
        }

        unitNameText.text = unit.statsBase.nombreUnidad.ToString();
        unitHealthText.text = $"Vida: {unit.vidaActual}/{unit.statsBase.vidaMaxima}";
        unitMovementText.text = $"Movimiento: {unit.movimientosRestantes}/{unit.statsBase.puntosMovimiento}";

        if (unitRangeText != null)
            unitRangeText.text = $"Rango: {unit.statsBase.rangoAtaque}";

        if (unitAttackText != null)
            unitAttackText.text = $"Ataque: {unit.statsBase.ataque}";

        if (unit.ownerID == 0)
        {
            actionAttackButton.gameObject.SetActive(true);
            actionMoveButton.gameObject.SetActive(true);
            if (actionPassButton != null) actionPassButton.gameObject.SetActive(true);

            // Configurar interactividad
            actionAttackButton.interactable = unit.statsBase.ataque > 0;
            actionMoveButton.interactable = unit.movimientosRestantes > 0;
            if (actionPassButton != null) actionPassButton.interactable = true;

            // Configurar Botón Especial (Colono)
            UnitBuilder colonoLogic = unit.GetComponent<UnitBuilder>();
            Ability militares = unit.GetComponent<Ability>();

            actionSpecialButton.onClick.RemoveAllListeners(); // Limpiar listeners antiguos

            if (colonoLogic != null)
            {
                actionSpecialButton.gameObject.SetActive(true);
               // accionSaquear.gameObject.SetActive(true);
                actionSpecialButtonText.text = "Construir";
                actionSpecialButton.interactable = true; // El UnitBuilder comprobará los recursos

                // Conectamos el botón al SimpleClickTester
                SimpleClickTester clickTester = FindFirstObjectByType<SimpleClickTester>();
                if (clickTester != null)
                {
                    actionSpecialButton.onClick.AddListener(clickTester.BotonConstruirPulsado);
                }
            }
            else if(militares!= null)
            {
                accionSaquear.gameObject.SetActive(true);
                actionSpecialButton.gameObject.SetActive(false);
            }
            else
            {
                // No es un colono, ocultar botón especial
                actionSpecialButton.gameObject.SetActive(false);
               // accionSaquear.gameObject.SetActive(false);
            }
        }
        else
        {
            // Si no es Colono, ocultar el botón especial
            // ES ENEMIGA: Ocultar TODOS los botones de acción
            actionAttackButton.gameObject.SetActive(false);
            actionMoveButton.gameObject.SetActive(false);
            actionSpecialButton.gameObject.SetActive(false);
            accionSaquear.gameObject.SetActive(false);//false
            actionPassButton.gameObject.SetActive(false);

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
    public void HideUnitPanel()
    {
        selectedUnit = null; // Limpiar la referencia
        if (unitPanelContainer != null)
        {
            unitPanelContainer.SetActive(false);
        }
    }
    // Dentro de UIManager.cs

    /*
    private void ConfigureConstructionButtons(ColonoUnit colono)
    {
        HideUnitPanel();

        // Asume que tienes una referencia al jugador activo
       // Player activePlayer = GameManager.Instance.ActivePlayer;

        // --- 1. Definición de Costos (Basado en tu lista) ---

        // Estructuras
        var roadCost = new Dictionary<ResourceType, int> { { ResourceType.Madera, 1 }, { ResourceType.Arcilla, 1 } };
        var settlementCost = new Dictionary<ResourceType, int> { { ResourceType.Madera, 1 }, { ResourceType.Arcilla, 1 }, { ResourceType.Oveja, 1 }, { ResourceType.Trigo, 1 } };
        var cityUpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Roca, 3 }, { ResourceType.Trigo, 2 } };

        // Unidades
        var soldierCost = new Dictionary<ResourceType, int> { { ResourceType.Oveja, 1 }, { ResourceType.Trigo, 1 } };
        var artilleroCost = new Dictionary<ResourceType, int> { { ResourceType.Oveja, 2 }, { ResourceType.Trigo, 1 } };
        var caballeroCost = new Dictionary<ResourceType, int> { { ResourceType.Oveja, 2 }, { ResourceType.Trigo, 2 } };
        var caballeriaCost = new Dictionary<ResourceType, int> { { ResourceType.Oveja, 3 }, { ResourceType.Trigo, 3 } };
        var colonoRecruitCost = new Dictionary<ResourceType, int> { { ResourceType.Oveja, 1 }, { ResourceType.Trigo, 1 } }; // Asumiendo que 'Soldado' es la unidad genérica de infantería.

        // --- 2. Lógica y Conexión de Botones ---
        
        // Botones de Estructura
        ConfigureButton(buildRoadButton, roadCost, activePlayer,
                        () => colono.CanPlaceRoad(),
                        () => GameManager.Instance.ActionBuildRoad(colono, roadCost));

        ConfigureButton(buildSettlementButton, settlementCost, activePlayer,
                        () => colono.CanPlaceSettlement(),
                        () => GameManager.Instance.ActionBuildSettlement(colono, settlementCost));

        // El botón de Ciudad se debe hacer en una ciudad, no por el colono, pero lo conectamos:
        ConfigureButton(upgradeCityButton, cityUpgradeCost, activePlayer,
                        () => false, // Cambiar a lógica de 'adyacente a ciudad' si aplica
                        () => Debug.Log("Upgrade City action called."));


        // Botones de Reclutamiento
        ConfigureButton(recruitSoldierButton, soldierCost, activePlayer,
                        () => true, // Reclutar no necesita chequeo de posición, solo costos
                        () => GameManager.Instance.ActionRecruitUnit(UnitType.Soldier, soldierCost));

        ConfigureButton(recruitArtilleroButton, artilleroCost, activePlayer,
                        () => true,
                        () => GameManager.Instance.ActionRecruitUnit(UnitType.Artillero, artilleroCost));

        ConfigureButton(recruitCaballeroButton, caballeroCost, activePlayer,
                        () => true,
                        () => GameManager.Instance.ActionRecruitUnit(UnitType.Caballero, caballeroCost));

        ConfigureButton(recruitCaballeriaButton, caballeriaCost, activePlayer,
                        () => true,
                        () => GameManager.Instance.ActionRecruitUnit(UnitType.Caballeria, caballeriaCost));

        ConfigureButton(recruitColonoButton, colonoRecruitCost, activePlayer,
                        () => true,
                        () => GameManager.Instance.ActionRecruitUnit(UnitType.Colono, colonoRecruitCost));
   
    }
    */

    /// <summary>
    /// Helper para configurar un botón de acción/construcción.
    /// </summary>
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

