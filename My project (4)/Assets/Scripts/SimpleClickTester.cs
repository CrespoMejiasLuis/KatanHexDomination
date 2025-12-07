using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SimpleClickTester : MonoBehaviour
{
    [HideInInspector] public Unit unidadSeleccionada;
    private Camera camaraPrincipal;

    [Header("UI y referencias")]
    public GameObject unitActionMenu;

    [Header("Visuales Seleccion")]
    public Color selectionColor = Color.green;
    public Color attackRangeColor = Color.yellow;

    [Header("Configuracion de Capas")]
    public LayerMask unitLayerMask;
    public LayerMask gridLayerMask;

    [Header("Prefabs construccion")]
    public GameObject ciudadPrefab;

    private readonly int PLAYER_ID = 0; //human player

    private PlayerInputMode currentMode = PlayerInputMode.Selection;
    private SettlementUnit activePoblado = null;
    private HexTile lastHoveredTile = null;
    void Start()
    {
        camaraPrincipal = Camera.main;

        if (unitActionMenu != null) unitActionMenu.SetActive(false);
        GameManager.OnPlayerTurnEnd += DeseleccionarUnit;

        // 🔥 Al terminar turno de la IA → deseleccionar (opcional pero recomendable)
        GameManager.OnAITurnEnd += DeseleccionarUnit;
    }

    void Update()
    {
        if (currentMode == PlayerInputMode.MoveTargeting && GameManager.Instance.CurrentState == GameState.PlayerTurn)
        {
            HandleHoverPath();
        }
        else
        {
            // Si salimos del modo mover, asegurarnos de borrar la línea
            if (PathVisualizer.Instance != null) PathVisualizer.Instance.HidePath();
        }
        if (!Input.GetMouseButtonDown(0))
        {
            return; // No hay clic, no hacer nada
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Clic en la UI, no hacer nada en el mundo
        }

        Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(rayo, out hit, float.MaxValue, gridLayerMask))
        {
            HexTile casillaClicada = hit.collider.GetComponentInParent<HexTile>();
            if (casillaClicada != null)
            {
                switch (currentMode)
                {
                    case PlayerInputMode.AbilityTargeting:
                        IntentarSaqueo(casillaClicada);
                        return;

                    case PlayerInputMode.MoveTargeting:
                        HandleGridClick(casillaClicada);
                        return;
                }
            }
        }
        if (Physics.Raycast(rayo, out hit, float.MaxValue, unitLayerMask))
        {
            Unit unidadClickada = hit.collider.GetComponentInParent<Unit>();
            if (unidadClickada != null)
            {
                switch (currentMode)
                {
                    case PlayerInputMode.Selection:
                        HandleUnitSelection(unidadClickada);
                        break;

                    case PlayerInputMode.AttackTargeting:
                        IntentarAtacar(unidadClickada);
                        break;

                    case PlayerInputMode.MoveTargeting:
                        if(unidadClickada.ownerID != unidadSeleccionada.ownerID)
                        {
                            IntentarAtacar(unidadClickada);
                            return;
                        }
                        break;
                }
                return;
            }
        }

        // 2. ¿He clicado en la capa de CASILLAS?
        if (Physics.Raycast(rayo, out hit, float.MaxValue, gridLayerMask))
        {
            HexTile casillaClicada = hit.collider.GetComponentInParent<HexTile>();
            if (casillaClicada != null)
            {
                // Clic en casilla. Qué hacer depende del MODO actual
                HandleGridClick(casillaClicada);
                return; // Acción de clic completada
            }
        }

        // 3. ¿He clicado en el vacío?
        // Si el código llega aquí, es que no se ha clicado ni en unidad ni en casilla
        DeseleccionarUnit();
    }
    private void HandleHoverPath()
    {
        Ray ray = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, float.MaxValue, gridLayerMask))
        {
            HexTile hoveredTile = hit.collider.GetComponentInParent<HexTile>();

            // Solo calculamos si la casilla ha cambiado para ahorrar rendimiento
            if (hoveredTile != null && hoveredTile != lastHoveredTile)
            {
                lastHoveredTile = hoveredTile;

                Unit selectedUnit = GameManager.Instance.selectedUnit;

                if (selectedUnit != null && Pathfinding.Instance != null && PathVisualizer.Instance != null)
                {
                    // 1. Calcular la ruta completa (sin amenazas)
                    List<Vector2Int> fullPath = Pathfinding.Instance.FindSmartPath(
                        selectedUnit.misCoordenadasActuales,
                        hoveredTile.AxialCoordinates,
                        null
                    );

                    // --- NUEVA LÓGICA: FILTRAR POR PUNTOS DE MOVIMIENTO ---

                    List<Vector2Int> reachablePath = new List<Vector2Int>();
                    int movementCostSum = 0;
                    int maxMovePoints = selectedUnit.movimientosRestantes;

                    // Siempre añadimos el punto de inicio (donde está la unidad)
                    if (fullPath.Count > 0) reachablePath.Add(fullPath[0]);

                    // Recorremos el resto del camino (empezando por el paso 1)
                    for (int i = 1; i < fullPath.Count; i++)
                    {
                        // Obtenemos el coste de la casilla
                        CellData cell = BoardManager.Instance.GetCell(fullPath[i]);

                        if (cell != null)
                        {
                            int stepCost = cell.cost; // Por defecto es 1, o 2 en bosques, etc.

                            // Verificamos si nos alcanza
                            if (movementCostSum + stepCost <= maxMovePoints)
                            {
                                movementCostSum += stepCost;
                                reachablePath.Add(fullPath[i]);
                            }
                            else
                            {
                                // ¡No tenemos gasolina para llegar a esta casilla!
                                // Cortamos la línea aquí.
                                break;
                            }
                        }
                    }

                    // 2. Dibujar solo el camino alcanzable
                    PathVisualizer.Instance.DrawPath(reachablePath);
                }
            }
        }
        else
        {
            // Si sacamos el ratón del tablero, borrar línea
            if (lastHoveredTile != null)
            {
                lastHoveredTile = null;
                if (PathVisualizer.Instance != null) PathVisualizer.Instance.HidePath();
            }
        }
    }
    private void HandleGridClick(HexTile casillaClicada)
    {
        switch (currentMode)
        {
            case PlayerInputMode.Selection:
                DeseleccionarUnit();
                break;

            case PlayerInputMode.MoveTargeting:
                // 1. Revisar si la casilla tiene un enemigo -> Atacar
                CellData targetCell = BoardManager.Instance.GetCell(casillaClicada.AxialCoordinates);
                if (targetCell != null && targetCell.unitOnCell != null)
                {
                    if (targetCell.unitOnCell.ownerID != unidadSeleccionada.ownerID)
                    {
                        IntentarAtacar(targetCell.unitOnCell);
                        return;
                    }
                }
                if (targetCell != null && (targetCell.typeUnitOnCell == TypeUnit.Poblado|| targetCell.typeUnitOnCell == TypeUnit.Ciudad))
                {
                    if (targetCell.owner  != unidadSeleccionada.ownerID)
                    {
                        IntentarAtacar(targetCell.unitOnCell);
                        return;
                    }
                }

                if (unidadSeleccionada != null)
                {
                    UnitMovement mover = unidadSeleccionada.GetComponent<UnitMovement>();
                    if (mover != null && Pathfinding.Instance != null)
                    {
                        // 1. Calcular la ruta (Igual que en el Hover: Sin amenaza)
                        List<Vector2Int> path = Pathfinding.Instance.FindSmartPath(
                            unidadSeleccionada.misCoordenadasActuales,
                            casillaClicada.AxialCoordinates, // O la coordenada de la casilla
                            null // Sin threatMap
                        );

                        // 2. Mandar la ruta a la unidad
                        if (path != null && path.Count > 0)
                        {
                            mover.MoversePorRuta(path);

                            // Limpiamos visuales
                            if (PathVisualizer.Instance != null) PathVisualizer.Instance.HidePath();
                        }
                    }
                }
                currentMode = PlayerInputMode.Selection;
                break;
        }
    }

    //para que si clickas una unidad te salga el menu de acciones que puede realizar
    private void HandleUnitSelection(Unit unitClickada)
    {
        DeseleccionarUnit(); // Deselecciona la anterior

        unidadSeleccionada = unitClickada;
        GameManager.Instance.SelectUnit(unitClickada);

        if (unitClickada.ownerID == PLAYER_ID)
        {
            SettlementUnit pobladoLogic = unitClickada.GetComponent<SettlementUnit>(); 
        
            if(pobladoLogic != null) 
            {
                // Es un poblado. Llamamos a su acción específica.
                activePoblado = pobladoLogic;
                pobladoLogic.OpenTradeMenu();
                Debug.Log("Poblado seleccionado. Menú de Intercambio activado.");
                return;
            }

            if(unitActionMenu != null) unitActionMenu.SetActive(true);
            
        }
        else
        {
            //if(unitActionMenu != null) unitActionMenu.SetActive(false);
        }
        
        // ¡Importante! Al seleccionar una unidad, SIEMPRE volvemos al modo Selección
        currentMode = PlayerInputMode.Selection;
        HighlightAdjacents(unitClickada);

    }

    // MODIFICADA: Ahora también resetea el modo
    private void DeseleccionarUnit()
    {
        if(activePoblado!=null && activePoblado.tradeMenu!=null)
        {
            activePoblado.tradeMenu.SetActive(false);
            activePoblado = null;
        }

        if(unidadSeleccionada != null)
        {
            unidadSeleccionada = null;
        }

        if(unitActionMenu != null)
        {
            unitActionMenu.SetActive(false);
        }

        // ¡Importante! Si no hay nada seleccionado, volvemos al modo Selección
        currentMode = PlayerInputMode.Selection;
        GameManager.Instance.DeselectAll();
        BoardManager.Instance.UpdateAllBorders();

    }

    /// <summary>
    /// Esta función es pública para que un botón de la UI pueda llamarla.
    /// </summary>
    public void BotonConstruirPulsado()
    {
        // 1. ¿Tenemos una unidad seleccionada?
        if (unidadSeleccionada == null)
        {
            Debug.Log("¡No hay ninguna unidad seleccionada para construir!");
            return;
        }

        // 2. ¿Esa unidad es un Colono (tiene el script UnitBuilder)?
        UnitBuilder builder = unidadSeleccionada.GetComponent<UnitBuilder>();

        if (builder != null)
        {
            // 3. ¡Sí! Le damos la orden de construir
            builder.IntentarConstruirPoblado();
            unitActionMenu.SetActive(false);
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede construir!");
        }
        BoardManager.Instance.UpdateAllBorders();

    }

    public void BotonMoverPulsado()
    {
        if (unidadSeleccionada == null)
        {
            return; // No hay unidad seleccionada, no hacer nada
        }

        // Comprobar si la unidad se puede mover
        if (unidadSeleccionada.GetComponent<UnitMovement>() != null && unidadSeleccionada.movimientosRestantes > 0)
        {
            // ¡Sí puede! Cambiamos al modo de movimiento
            currentMode = PlayerInputMode.MoveTargeting;
            
            // Ocultamos el menú para que el jugador pueda ver el tablero
            if (unitActionMenu != null)
            {
                unitActionMenu.SetActive(false);
            }
            BoardManager.Instance.HideAllBorders(); // quita bordes previos

            // volver a marcar solo la casilla de la unidad
           // HighlightAdjacents(unidadSeleccionada);


            Debug.Log("MODO MOVER: Seleccione una casilla de destino.");
        }
        else
        {
            Debug.Log("¡Esta unidad no se puede mover o no tiene puntos de movimiento!");
        }
    }
    private void IntentarSaqueo(HexTile casilla)
    {
        if (unidadSeleccionada == null || casilla == null) return;

        Ability ability = unidadSeleccionada.GetComponent<Ability>();
        if (ability == null) return;

        // Usamos las coordenadas de la casilla para obtener la celda
        CellData cellData = BoardManager.Instance.GetCell(casilla.AxialCoordinates);
        if (cellData == null) return;

        ability.Saquear(cellData);

        // Volver al modo selección
        currentMode = PlayerInputMode.Selection;
    }

    public void BotonSaqueoPulsado()
    {
        if (unidadSeleccionada == null) return;

        Ability ability = unidadSeleccionada.GetComponent<Ability>();
        if (ability == null)
        {
            Debug.Log("La unidad seleccionada no tiene la habilidad de saquear.");
            return;
        }

        // Cambiar al modo selección de casilla para saquear
        currentMode = PlayerInputMode.AbilityTargeting;
        if (unitActionMenu != null) unitActionMenu.SetActive(false);

        Debug.Log("Modo saquear: selecciona una casilla enemiga o neutral.");
    }

    public void BotonAtacarPulsado()
    {
        if (unidadSeleccionada == null) return;

        UnitAttack attack = unidadSeleccionada.GetComponent<UnitAttack>();
        if (attack == null)
        {
            Debug.Log("La unidad seleccionada no puede atacar");
            return;
        }

        currentMode = PlayerInputMode.AttackTargeting;
        if (unitActionMenu != null)
            unitActionMenu.SetActive(false);

        HighlightAttackRange(unidadSeleccionada);

        Debug.Log("Modo atacar activado. Selecciona un objetivo enemigo.");
    }

    private void IntentarAtacar(Unit objetivo)
    {
        if (unidadSeleccionada == null || objetivo == null) return;

        // Verificar propietario
         if (objetivo.ownerID == unidadSeleccionada.ownerID)
         {
             Debug.Log("No puedes atacar a tus aliados");
             currentMode = PlayerInputMode.Selection;
             return;
         }
        

        UnitAttack attack = unidadSeleccionada.GetComponent<UnitAttack>();
        if (attack == null)
        {
            Debug.Log("Unidad no tiene capacidad de ataque");
            currentMode = PlayerInputMode.Selection;
            return;
        }

        if (!attack.PuedeAtacar(objetivo))
        {
            Debug.Log("Objetivo fuera de rango");
            currentMode = PlayerInputMode.Selection;
            return;
        }

        // Atacar
        attack.Atacar(objetivo);

        currentMode = PlayerInputMode.Selection;
        // Restaurar visuales normal (o deseleccionar si se acaban los puntos)
        if(unidadSeleccionada != null) HighlightAdjacents(unidadSeleccionada);
    }

    public void BotonCrearArtilleroPulsado()
    {
        // 1. Comprobar si hay unidad seleccionada
        if (unidadSeleccionada == null)
        {
            Debug.Log("¡No hay ninguna unidad seleccionada para crear Artillero!");
            return;
        }

        // 2. Comprobar si la unidad seleccionada tiene un UnitRecruiter (o SettlementUnit con reclutamiento)
        UnitRecruiter recruiter = unidadSeleccionada.GetComponent<UnitRecruiter>();

        if (recruiter != null)
        {
            // 3. Llamar a la función de reclutamiento, pasando la unidad seleccionada
            recruiter.ConstruirArtillero(unidadSeleccionada);

            // 4. Cerrar menú de acciones
            if (unitActionMenu != null)
                unitActionMenu.SetActive(false);
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede crear Artillero!");
        }
    }

    public void BotonCrearCaballeroPulsado()
    {
        // 1. Comprobar si hay unidad seleccionada
        if (unidadSeleccionada == null)
        {
            Debug.Log("¡No hay ninguna unidad seleccionada para crear Caballero!");
            return;
        }

        // 2. Comprobar si la unidad seleccionada tiene un UnitRecruiter (o SettlementUnit con reclutamiento)
        UnitRecruiter recruiter = unidadSeleccionada.GetComponent<UnitRecruiter>();

        if (recruiter != null)
        {
            // 3. Llamar a la función de reclutamiento, pasando la unidad seleccionada
            recruiter.ConstruirCaballero(unidadSeleccionada);

            // 4. Cerrar menú de acciones
            if (unitActionMenu != null)
                unitActionMenu.SetActive(false);
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede crear Caballero!");
        }
    }

    public void UpgradeCiudad()
    {
        UpgradeCiudad(this.unidadSeleccionada);
    }

    public void UpgradeCiudad(Unit unitToUpgrade)
    {
        if(unitToUpgrade == null)
        {
            Debug.Log("No hay unidad por mejorar");
            return;
        }
        else
        {
            unidadSeleccionada = unitToUpgrade;
        }

        if (unidadSeleccionada == null)
        {
            Debug.Log("No hay unidad seleccionada");
            return;
        }

        SettlementUnit pobladoLogic = unidadSeleccionada.GetComponent<SettlementUnit>();

        if (pobladoLogic != null && unidadSeleccionada.statsBase.nombreUnidad == TypeUnit.Poblado)
        {
            if (ciudadPrefab == null) return;

            Unit unitCerebro = unidadSeleccionada;
            if (unitCerebro.ownerID == -1) return;

            //datos casilla
            CellData cellDondeEstamos = BoardManager.Instance.GetCell(unitCerebro.misCoordenadasActuales);
            Vector3 spawnPoint = unitCerebro.transform.position;
            if (cellDondeEstamos == null) { /* ... error ... */ return; }
            if(cellDondeEstamos.typeUnitOnCell == TypeUnit.Ciudad) return;

            //Necesitamos el Unit del prefab
            Unit ciudadUnitPrefab = ciudadPrefab.GetComponent<Unit>();
            bool recursosNecesarios = unitCerebro.RecursosNecesarios(ciudadUnitPrefab);
            if (!recursosNecesarios) return; //si no tienes materiales suficientes no construye

            //Gastar recursos - Obtener el jugador correcto basándose en el dueño de la unidad
            Player jugador = GameManager.Instance.GetPlayer(unitCerebro.ownerID);
            if (jugador == null)
            {
                Debug.LogError("No se pudo obtener el jugador para la unidad con ownerID: " + unitCerebro.ownerID);
                return;
            }

            Dictionary<ResourceType, int> productionCost = ciudadUnitPrefab.statsBase.GetProductCost();

            if(jugador.numPoblados > 1)
            {
                productionCost = ciudadUnitPrefab.actualizarCostes(productionCost, jugador);
            }

            bool recursosGastados = jugador.SpendResources(productionCost);
            if (!recursosGastados) return;

            //Accion
            HexTile tileVisual = cellDondeEstamos.visualTile;

            // 2. OCULTAR LA CASILLA VIEJA
            // Desactiva todos los Renderers (modelos 3D) de la casilla de terreno
            foreach (Renderer r in tileVisual.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            //Crear ciudad
            GameObject nuevaCiudad = Instantiate(ciudadPrefab, spawnPoint, Quaternion.identity);

            //Dueno ciudad
            Unit ciudad = nuevaCiudad.GetComponent<Unit>();
            if (ciudad != null)
            {
                ciudad.ownerID = unitCerebro.ownerID;
                jugador.ArmyManager.RegisterUnit(ciudad);
            }

            ciudad.misCoordenadasActuales = cellDondeEstamos.coordinates;
            cellDondeEstamos.typeUnitOnCell = TypeUnit.Ciudad;
            cellDondeEstamos.unitOnCell = ciudad;
            jugador.AddVictoryPoints(1);
            // UIManager.Instance.UpdateVictoryPointsText(jugador.victoryPoints);
            Destroy(unitCerebro.gameObject);
            jugador.ArmyManager.DeregisterUnit(unitCerebro);
        }
    }
    public void BotonCrearColonoPulsado()
    {
        // 1. Comprobar si hay unidad seleccionada
        if (unidadSeleccionada == null)
        {
            Debug.Log("¡No hay ninguna unidad seleccionada para crear Colono!");
            return;
        }

        // 2. Comprobar si la unidad seleccionada tiene un UnitRecruiter
        UnitRecruiter recruiter = unidadSeleccionada.GetComponent<UnitRecruiter>();

        if (recruiter != null)
        {
            // 3. Llamar a la función del recruiter
            recruiter.ConstruirColono(unidadSeleccionada);

            // 4. Cerrar menú de acciones
            if (unitActionMenu != null)
                unitActionMenu.SetActive(false);
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede crear Colono!");
        }
    }
    private void HighlightAdjacents(Unit unidad)
    {
        // Ocultar todos los bordes primero
        BoardManager.Instance.HideAllBorders();

        // Obtener la casilla donde está la unidad
        CellData cellActual = BoardManager.Instance.GetCell(unidad.misCoordenadasActuales);
        if (cellActual == null) return;

        // Mostrar borde SOLO en la casilla actual
        if (cellActual.visualTile != null)
        {
            cellActual.visualTile.EnableFullBorder(selectionColor);
        }
        
    }

    

    private void HighlightAttackRange(Unit unit)
    {
        BoardManager.Instance.HideAllBorders();
        
        if (unit == null || BoardManager.Instance == null || BoardManager.Instance.gridData == null) return;

        int range = unit.statsBase.rangoAtaque;
        Vector2Int unitCoords = unit.misCoordenadasActuales;

        foreach (CellData cell in BoardManager.Instance.gridData)
        {
            if (cell != null && cell.visualTile != null)
            {
                int dist = BoardManager.Instance.Distance(unitCoords, cell.coordinates);
                if (dist <= range && dist > 0) // dist > 0 para no resaltar la propia unidad si no se quiere
                {
                    cell.visualTile.EnableFullBorder(attackRangeColor);
                }
            }
        }
    }

}