using UnityEngine;
using System; // Necesario para usar 'Action'

public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    // Un patr�n 'Singleton' asegura que solo haya UN GameManager en todo el juego.
    public static GameManager Instance { get; private set; }
    [Header("Referencias de Otros Scripts")]
    public CameraManager cameraManager;
    public AIAnalysisManager aiAnalysis;
    // === ESTADO ===
    public GameState CurrentState { get; private set; }
    private HexGridGenerator _gridGenerator;

    // === EVENTOS ===
    // Otros scripts se suscribiron a estos eventos para saber cuando actuar.
    public static event Action OnGameStart;
    public static event Action OnPlayerTurnStart;
    public static event Action OnPlayerTurnEnd;
    public static event Action OnAITurnStart;
    public static event Action OnAITurnEnd;

    // === NUEVOS EVENTOS DE INTERACCIÓN (Para la UI de Unidad) ===
    public static event Action<Unit> OnUnitSelected; // Notifica que una unidad ha sido seleccionada
    public static event Action OnDeselected; // Notifica que no hay nada seleccionado
    public Unit selectedUnit { get; private set; }
    public Player humanPlayer; 
    public Player IAPlayer;

    public static readonly Vector2Int[] axialNeighborDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0),  // Derecha
        new Vector2Int(1, -1), // Arriba-Derecha
        new Vector2Int(0, -1), // Arriba-Izquierda
        new Vector2Int(-1, 0), // Izquierda
        new Vector2Int(-1, 1), // Abajo-Izquierda
        new Vector2Int(0, 1)   // Abajo-Derecha
    };

    void Awake()
    {
        // Configuracion del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            transform.SetParent(null); // Ensure it's a root object
            DontDestroyOnLoad(gameObject); // Opcional, si persiste entre escenas
        }

    }

    void Start()
    {
        _gridGenerator = FindFirstObjectByType<HexGridGenerator>();
        if (cameraManager == null)
        {
            cameraManager = FindFirstObjectByType<CameraManager>();
            if (cameraManager == null)
            {
                Debug.LogError("CameraManager no encontrado en la escena. La cámara no rotará.");
            }
        }
        if (aiAnalysis == null) aiAnalysis = FindFirstObjectByType<AIAnalysisManager>();

        if (_gridGenerator == null)
        {
            Debug.Log("No hay un HexGridGenerator en la escena");
            return;
        }


        SetState(GameState.Initializing);   
    }

    /// <summary>
    /// La funci�n principal para cambiar de estado.
    /// </summary>
    public void SetState(GameState newState)
    {
        Debug.Log(CurrentState);
        if (CurrentState == newState) return; // No cambiar al mismo estado

        CurrentState = newState;
        Debug.Log("Nuevo estado del juego: " + newState);

        /*
        if (cameraManager != null)
        {
            // 🔑 Llamada clave: Mover la cámara antes de que empiece el turno
            if (newState == GameState.PlayerTurn)
            {
                cameraManager.ChangePerspective(true); // true = vista del Jugador
            }
            else if (newState == GameState.AITurn)
            {
                cameraManager.ChangePerspective(false); // false = vista de la IA
            }
        }
        */

        // Dispara el evento correspondiente al nuevo estado
        switch (newState)
        {
            case GameState.Initializing:
                // El HexGridGenerator llamar� a esto cuando termine sus animaciones
                SetUp();
                SetState(GameState.PlayerTurn);
                break;

            case GameState.PlayerTurn:
                OnPlayerTurnStart?.Invoke(); // Llama al evento
                Debug.Log("📢 Evento OnPlayerTurnStart disparado.");
                ProcessCooldowns();
                CollectTurnResources(1);     // L�gica de "Civilization": recolectar recursos al inicio del turno
                break;

            case GameState.AITurn:
                OnAITurnStart?.Invoke();
                CollectTurnResources(2);

                // --- ¡CORRECCIÓN AQUÍ! ---
                // Borramos StartCoroutine(AIPlayTurn());
                // Llamamos a la IA real:
                if (IAPlayer != null)
                {
                    IAPlayer.BeginTurn(); // <-- Esto inicia PlayerIA.ExecuteAITurn
                }
                else
                {
                    Debug.LogError("IAPlayer no asignado en GameManager. Saltando turno.");
                    EndAITurn();
                }
                break;

            
            case GameState.EndTurnResolution:
                // Estado 'puente', si alguna vez lo usamos.
                if (CurrentState == GameState.PlayerTurn)
                    SetState(GameState.AITurn);
                else
                    SetState(GameState.PlayerTurn);
                break;

            case GameState.GameOver:
                // L�gica de fin de partida
                break;
        }
    }

    /// <summary>
    /// Llamado por el bot�n de "Terminar Turno" de la UI.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (CheckVictory()) return;

        if (CurrentState == GameState.PlayerTurn)
        {
            OnPlayerTurnEnd?.Invoke();
            SetState(GameState.AITurn); // O GameState.EndTurnResolution si necesitas un paso intermedio
        }
    }

    public void EndAITurn()
    {
        if (CheckVictory()) return;

        if (CurrentState == GameState.AITurn)
        {
            OnAITurnEnd?.Invoke();
            // Vuelve al turno del jugador
            SetState(GameState.PlayerTurn);
        }
    }

    /// <summary>
    /// L�gica de tu juego: Otorga recursos al jugador activo al inicio de su turno.
    /// </summary>
    /// <summary>
    /// Procesa los cooldowns de todas las celdas del tablero (ej. Saqueo).
    /// </summary>
    private void ProcessCooldowns()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.gridData == null) return;

        Debug.Log("⏳ Procesando cooldowns globales...");
        foreach (CellData cell in BoardManager.Instance.gridData)
        {
            if (cell != null && cell.lootedCooldown > 0)
            {
                cell.lootedCooldown--;
                if (cell.lootedCooldown == 0)
                {
                    cell.isRaided = false;
                    cell.UpdateVisual();
                    Debug.Log($"✅ Casilla {cell.coordinates} recuperada del saqueo.");
                }
            }
        }
    }

    private void CollectTurnResources(int playerID)
    {
        Debug.Log($"Recolectando recursos para el jugador {playerID}...");

        // 1. Determinar el jugador y el 'owner ID'
        Player currentPlayer = (playerID == 1) ? humanPlayer : IAPlayer;
        int ownerIDToCheck = (playerID == 1) ? 0 : 1; // Asume 1->0 y 2->1

        if (currentPlayer == null)
        {
            Debug.LogError($"GameManager no tiene una referencia para el Jugador {playerID}. Revisa el Inspector.");
            return;
        }

        if (BoardManager.Instance == null || BoardManager.Instance.gridData == null)
        {
            Debug.LogError("BoardManager.gridData no está inicializado.");
            return;
        }
        
        // 2. Iterar por todo el tablero buscando ciudades del jugador
        foreach (CellData cell in BoardManager.Instance.gridData)
        {
            // 3. Si encontramos una ciudad que pertenece al jugador actual...
            if (cell != null && cell.owner == ownerIDToCheck && (cell.typeUnitOnCell == TypeUnit.Poblado || cell.typeUnitOnCell == TypeUnit.Ciudad))
            {
                int yieldAmount = 0;
                if (cell.typeUnitOnCell == TypeUnit.Ciudad)
                {
                    yieldAmount = 2;
                }
                else if (cell.typeUnitOnCell == TypeUnit.Poblado)
                {
                    yieldAmount = 1;
                }
                Debug.Log($"Ciudad encontrada en {cell.coordinates} para Jugador {playerID}. Comprobando vecinos.");
                
                Vector2Int cityCoords = cell.coordinates;
                ResourceType type = cell.resource;
                currentPlayer.AddResource(type, yieldAmount);
                // 4. ...iteramos por las 6 direcciones axiales
                foreach (Vector2Int direction in axialNeighborDirections)
                {
                    // Calcular las coordenadas del vecino
                    Vector2Int neighborCoords = cityCoords + direction;
                    
                    // 5. Obtener la celda vecina usando la función del BoardManager
                    CellData neighborCell = BoardManager.Instance.GetCell(neighborCoords);

                    // 6. Si la celda vecina existe (no está fuera del mapa)...
                    if (neighborCell != null)
                    {
                        // 7. Chequear si ha sido saqueada
                        if (neighborCell.lootedCooldown > 0)
                        {
                            Debug.Log($"La casilla {neighborCell.coordinates} fue saqueada. No produce recursos.");
                            // Cooldown se maneja en ProcessCooldowns
                        }
                        else
                        {
                            // 8. ...¡Añadir su recurso al jugador!
                            type = neighborCell.resource;
                            currentPlayer.AddResource(type, yieldAmount); 
                        }
                    }
                }
            }
        }
    }

    //Corutina donde se hacen las acciones de la IA -por ajora- pa que salte el turno
    
    private void GenerateGrid(Action onGridReady)
    {
        if (_gridGenerator != null)
            _gridGenerator.SetUp(onGridReady);
    }
    private void SetUp() 
    {
        GenerateGrid(() => {
            Debug.Log("🎉 Tablero listo. Transicionando a Turno del Jugador.");
            // 🔑 Solo cambiamos de estado CUANDO el generador nos avisa que ha terminado.
            UnitSpawner.Instance.SpawnInitialUnits();

            SetState(GameState.PlayerTurn);

        });
    }
    public void SelectUnit(Unit unitClicked)
    {
        // ¿Clic en una unidad enemiga?
        if (selectedUnit == unitClicked)
        {
            // Clic en unidad enemiga. Deselecciona la actual.
            DeselectAll();
            return;
        }

        // Es una unidad aliada.
        selectedUnit = unitClicked;

        // ¡Dispara el evento para que la UI reaccione!
        OnUnitSelected?.Invoke(selectedUnit);
        Debug.Log($"[GameManager] Unidad seleccionada: {selectedUnit.statsBase.nombreUnidad}");
    }

    public void DeselectAll()
    {
        if (selectedUnit != null)
        {
            selectedUnit = null;
            OnDeselected?.Invoke(); // ¡Dispara el evento para que la UI se oculte!
        }
    }

    public Player GetPlayer(int ownerID)
    {
        if(ownerID == 0) return humanPlayer;
        if(ownerID == 1) return IAPlayer;
        return null;
    }

    private bool CheckVictory()
    {
        // --- CONDICION DE VICTORIA (10 Puntos) ---
        if (humanPlayer != null && humanPlayer.victoryPoints >= 10)
        {
            Debug.Log("¡JUGADOR GANA! Has alcanzado 10 Puntos de Victoria.");
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(true);
            SetState(GameState.GameOver);
            return true;
        }
        else if (IAPlayer != null && IAPlayer.victoryPoints >= 10)
        {
            Debug.Log("¡IA GANA! La IA ha alcanzado 10 Puntos de Victoria.");
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(false);
            SetState(GameState.GameOver);
            return true;
        }
        return false;
    }
    public void OnPlayerEliminated(Player eliminatedPlayer)
    {
        if (CurrentState == GameState.GameOver) return;

        Debug.Log($"☠️ JUGADOR ELIMINADO: {eliminatedPlayer.playerName}");

        if (eliminatedPlayer == humanPlayer)
        {
            Debug.Log("¡DERROTA! Te has quedado sin unidades.");
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(false); // False = Perdiste
        }
        else if (eliminatedPlayer == IAPlayer)
        {
            Debug.Log("¡VICTORIA! La IA se ha quedado sin unidades.");
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(true); // True = Ganaste
        }

        SetState(GameState.GameOver);
    }
}