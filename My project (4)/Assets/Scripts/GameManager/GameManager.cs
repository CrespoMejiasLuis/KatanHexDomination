using UnityEngine;
using System; // Necesario para usar 'Action'

public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    // Un patr�n 'Singleton' asegura que solo haya UN GameManager en todo el juego.
    public static GameManager Instance { get; private set; }

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
    public static event Action<UnitBase> OnUnitSelected; // Notifica que una unidad ha sido seleccionada
    public static event Action OnDeselected; // Notifica que no hay nada seleccionado

    public Player humanPlayer; 
    public Player IAPlayer;

    private static readonly Vector2Int[] axialNeighborDirections = new Vector2Int[]
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
            DontDestroyOnLoad(gameObject); // Opcional, si persiste entre escenas
        }

    }

    void Start()
    {
        _gridGenerator = FindFirstObjectByType<HexGridGenerator>();

        if(_gridGenerator == null)
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

        // Dispara el evento correspondiente al nuevo estado
        switch (newState)
        {
            case GameState.Initializing:
                // El HexGridGenerator llamar� a esto cuando termine sus animaciones
                SetUp(() => {
                    Debug.Log("🎉 Tablero listo. Transicionando a Turno del Jugador.");
                    // 🔑 Solo cambiamos de estado CUANDO el generador nos avisa que ha terminado.
                    SetState(GameState.PlayerTurn); 
                });
                break;

            case GameState.PlayerTurn:
                OnPlayerTurnStart?.Invoke(); // Llama al evento
                Debug.Log("📢 Evento OnPlayerTurnStart disparado.");
                CollectTurnResources(1);     // L�gica de "Civilization": recolectar recursos al inicio del turno
                break;

            case GameState.AITurn:
                OnAITurnStart?.Invoke();     // Llama al evento
                CollectTurnResources(2);     // La IA tambi�n recolecta

                StartCoroutine(AIPlayTurn());
                break;

            case GameState.EndTurnResolution:
                // Aqu� podr�as comprobar condiciones de victoria
                // Y luego pasar al siguiente turno
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
        if (CurrentState == GameState.PlayerTurn)
        {
            OnPlayerTurnEnd?.Invoke();
            SetState(GameState.AITurn); // O GameState.EndTurnResolution si necesitas un paso intermedio
        }
    }

    /// <summary>
    /// L�gica de tu juego: Otorga recursos al jugador activo al inicio de su turno.
    /// </summary>
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
            if (cell != null && cell.owner == ownerIDToCheck && cell.hasCity)
            {
                Debug.Log($"Ciudad encontrada en {cell.coordinates} para Jugador {playerID}. Comprobando vecinos.");
                
                Vector2Int cityCoords = cell.coordinates;
                ResourceType type = cell.resource;
                currentPlayer.AddResource(type, 1);
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
                        // 7. ...¡Añadir su recurso al jugador!
                        type = neighborCell.resource;
                        currentPlayer.AddResource(type, 1); 
                        // (El método AddResource ya imprime el log de "ganó X")
                    }
                }
            }
        }
    }

    //Corutina donde se hacen las acciones de la IA -por ajora- pa que salte el turno
    private System.Collections.IEnumerator AIPlayTurn()
    {
        Debug.Log("🤖 La IA está jugando su turno...");

        // Esperar 2 segundos simulando que la IA “piensa”
        yield return new WaitForSeconds(2f);

        // Aquí podrías meter lógica real de IA (mover, atacar, etc.)

        Debug.Log("🤖 La IA ha terminado su turno. Pasando al jugador...");
        OnAITurnEnd?.Invoke();

        // Cambiar de nuevo al jugador
        SetState(GameState.PlayerTurn);
    }

    private void SetUp(Action onGridReady) 
    {
        if(_gridGenerator!=null)

            _gridGenerator.SetUp(onGridReady);
    }
}