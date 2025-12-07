using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

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
    public Button buildSettlementButton;
    public Button upgradeCityButton;
    public Button recruitArtilleroButton;
    public Button recruitCaballeroButton;
    public Button recruitColonoButton;

    [Header("Panel Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    public Button exitButton;

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
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitPressed);
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

    public void UpdateResourceTexts(int playerID, Dictionary<ResourceType, int> resources)
    {
        if (playerID == -1) return; // Solo actualizamos la UI para el jugador humano (0)
        
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

        // Actualizar los costes en los botones cuando cambian los recursos
        UpdateAllCosts();
    }

    public void UpdateVictoryPointsText(int playerID, int points)
    {
        if (playerID != 0) return; // Solo actualizamos la UI para el jugador humano (0)

        if (victoryPointsText != null)
        {
            victoryPointsText.text = points.ToString() + " / 10 PV";
        }
    }

    /// <summary>
    /// Actualiza los textos de coste en los botones del panel de construcción
    /// basándose en el número actual de poblados del jugador
    /// </summary>
    [Header("Ajustes Visuales 'Acciones'")]
    [Tooltip("Posición X de la segunda columna (ej: 80)")]
    public float costXSpacing = 40f;
    [Tooltip("Desplazamiento vertical de cada línea (ej: 10 para líneas normales, 0 para la última)")]
    public float costYOffset = 10f;

    /// <summary>
    /// Actualiza el texto centralizado de "Acciones" con los costes de todas las unidades/edificios
    /// </summary>
    public void UpdateAllCosts()
    {
        if (GameManager.Instance == null || GameManager.Instance.humanPlayer == null) return;
        if (constructionPanelContainer == null) return;

        Transform accionesTransform = constructionPanelContainer.transform.Find("Acciones");
        if (accionesTransform == null)
        {
             foreach(Transform child in constructionPanelContainer.transform)
             {
                 if(child.name == "Acciones")
                 {
                     accionesTransform = child;
                     break;
                 }
             }
        }

        if (accionesTransform == null) return;

        TextMeshProUGUI accionesText = accionesTransform.GetComponent<TextMeshProUGUI>();
        if (accionesText == null) accionesText = accionesTransform.GetComponentInChildren<TextMeshProUGUI>();

        if (accionesText == null) return;

        Player humanPlayer = GameManager.Instance.humanPlayer;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Helper local para formatear con alineación precisa usando tags de TMP
        // overrideY: permite sobreescribir el offset vertical por defecto
        string GetFormattedRow(Unit prefab, Player player, ResourceType r1, ResourceType r2, float? overrideY = null)
        {
            if (prefab == null || prefab.statsBase == null) return "-";
            
            Dictionary<ResourceType, int> baseCost = prefab.statsBase.GetProductCost();
            Dictionary<ResourceType, int> finalCost = baseCost;
            
            if (player.numPoblados > 1) 
            {
                finalCost = prefab.actualizarCostes(baseCost, player);
            }

            int val1 = finalCost.ContainsKey(r1) ? finalCost[r1] : 0;
            int val2 = finalCost.ContainsKey(r2) ? finalCost[r2] : 0;

            float yVal = overrideY.HasValue ? overrideY.Value : 10f;

            // <voffset> desplaza verticalmente la línea
            // <pos> fija la posición horizontal absoluta de la segunda columna
            return $"<voffset={yVal}>{val1}x<pos=40>{val2}x</voffset>";
        }

        // --- 1. Ciudad (Roca, Trigo) ---
        SimpleClickTester clickTester = FindFirstObjectByType<SimpleClickTester>();
        Unit ciudadPrefab = (clickTester != null && clickTester.ciudadPrefab != null) ? clickTester.ciudadPrefab.GetComponent<Unit>() : null;
        // Asumo Roca y Trigo basado en la imagen (Gris -> Roca, Amarillo -> Trigo)
        sb.AppendLine(GetFormattedRow(ciudadPrefab, humanPlayer, ResourceType.Roca, ResourceType.Trigo));

        // --- Recolectar Prefabs de Reclutamiento ---
        UnitRecruiter[] recruiters = FindObjectsByType<UnitRecruiter>(FindObjectsSortMode.None);
        UnitRecruiter recruiter = (recruiters.Length > 0) ? recruiters[0] : null;

        // --- 2. Artillero (Oveja, Trigo) ---
        Unit artilleroPrefab = (recruiter != null && recruiter.artilleroPrefab != null) ? recruiter.artilleroPrefab.GetComponent<Unit>() : null;
        // Asumo Oveja y Trigo (Blanco/Piel -> Oveja, Amarillo -> Trigo)
        sb.AppendLine(GetFormattedRow(artilleroPrefab, humanPlayer, ResourceType.Oveja, ResourceType.Trigo));

        // --- 3. Caballero (Oveja, Trigo) ---
        Unit caballeroPrefab = (recruiter != null && recruiter.caballeroPrefab != null) ? recruiter.caballeroPrefab.GetComponent<Unit>() : null;
        sb.AppendLine(GetFormattedRow(caballeroPrefab, humanPlayer, ResourceType.Oveja, ResourceType.Trigo));

        // --- 4. Colono (Oveja, Trigo) ---
        Unit colonoPrefab = (recruiter != null && recruiter.colonoPrefab != null) ? recruiter.colonoPrefab.GetComponent<Unit>() : null;
        sb.Append(GetFormattedRow(colonoPrefab, humanPlayer, ResourceType.Oveja, ResourceType.Trigo, 0f)); // Sin nueva línea al final

        accionesText.text = sb.ToString();
        // Debug.Log($"[UI] Text Updated:\n{accionesText.text}");
    }

    /// <summary>
    /// Convierte un diccionario de costes en una cadena legible para la UI
    /// </summary>
    private string GetCostString(Dictionary<ResourceType, int> costs)
    {
        if (costs == null || costs.Count == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        foreach (var costPair in costs)
        {
            string icon = GetResourceIcon(costPair.Key);
            sb.Append($"{icon}{costPair.Value} ");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Obtiene el icono/emoji para cada tipo de recurso
    /// </summary>
    private string GetResourceIcon(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Madera: return "🪵";
            case ResourceType.Roca: return "🪨";
            case ResourceType.Trigo: return "🌾";
            case ResourceType.Arcilla: return "🏺";
            case ResourceType.Oveja: return "🐑";
            default: return "?";
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
            // SI ES CABALLERO (no dispara), desactivamos el botón de ataque
            if(unit.statsBase.nombreUnidad == TypeUnit.Caballero)
            {
                 actionAttackButton.gameObject.SetActive(false);
            }
            
            actionMoveButton.interactable = unit.movimientosRestantes > 0;
            if (actionPassButton != null) actionPassButton.interactable = true;

            // Configurar Botón Especial (Colono)
            UnitBuilder colonoLogic = unit.GetComponent<UnitBuilder>();
            Ability militares = unit.GetComponent<Ability>();

            actionSpecialButton.onClick.RemoveAllListeners(); // Limpiar listeners antiguos

            if (colonoLogic != null)
            {
                actionSpecialButton.gameObject.SetActive(true);
                accionSaquear.gameObject.SetActive(false);
                if (actionSpecialButtonText != null)
                {
                    actionSpecialButtonText.text = "Construir";
                }
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

    public void ShowGameOver(bool playerWon)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
            {
                if (playerWon)
                {
                    gameOverText.text = "¡VICTORIA!\nHas conquistado el mapa.";
                    gameOverText.color = Color.green; // Opcional
                }
                else
                {
                    gameOverText.text = "DERROTA...\nLa IA ha dominado.";
                    gameOverText.color = Color.red; // Opcional
                }
            }
        }
    }

    public void OnRestartPressed()
    {
        // Reiniciar la escena actual (o ir al Menu Principal si es la escena 0)
        // Por ahora, recargamos la escena activa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnExitPressed()
    {
        Debug.Log("Volviendo al inicio...");
        SceneManager.LoadScene("inicio");
    }

}

