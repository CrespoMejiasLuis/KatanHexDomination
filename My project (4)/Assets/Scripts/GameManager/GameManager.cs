using UnityEngine;
using System; // Necesario para usar 'Action'
public enum GameState
{
    Initializing,     // El juego se está cargando, el tablero se está animando
    PlayerTurn,       // El jugador puede realizar acciones
    AITurn,           // La IA (enemigo) está pensando y actuando
    EndTurnResolution, // Se calculan los recursos, se comprueban las victorias
    GameOver          // La partida ha terminado
}

public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    // Un patrón 'Singleton' asegura que solo haya UN GameManager en todo el juego.
    public static GameManager Instance { get; private set; }

    // === ESTADO ===
    public GameState CurrentState { get; private set; }
    private HexGridGenerator _gridGenerator;

    // === EVENTOS ===
    // Otros scripts se suscribirán a estos eventos para saber cuándo actuar.
    public static event Action OnGameStart;
    public static event Action OnPlayerTurnStart;
    public static event Action OnPlayerTurnEnd;
    public static event Action OnAITurnStart;
    public static event Action OnAITurnEnd;

    void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional, si persiste entre escenas
        }
        CurrentState = GameState.Initializing;

    }

    /// <summary>
    /// La función principal para cambiar de estado.
    /// </summary>
    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return; // No cambiar al mismo estado

        CurrentState = newState;
        Debug.Log("Nuevo estado del juego: " + newState);

        // Dispara el evento correspondiente al nuevo estado
        switch (newState)
        {
            case GameState.Initializing:
                // El HexGridGenerator llamará a esto cuando termine sus animaciones
                Debug.Log("se Inicializo wey");
                SetUp();
               // SetState(GameState.PlayerTurn);
                break;

            case GameState.PlayerTurn:
                OnPlayerTurnStart?.Invoke(); // Llama al evento
                CollectTurnResources(1);     // Lógica de "Civilization": recolectar recursos al inicio del turno
                break;

            case GameState.AITurn:
                OnAITurnStart?.Invoke();     // Llama al evento
                CollectTurnResources(2);     // La IA también recolecta
                break;

            case GameState.EndTurnResolution:
                // Aquí podrías comprobar condiciones de victoria
                // Y luego pasar al siguiente turno
                if (CurrentState == GameState.PlayerTurn)
                    SetState(GameState.AITurn);
                else
                    SetState(GameState.PlayerTurn);
                break;

            case GameState.GameOver:
                // Lógica de fin de partida
                break;
        }
    }

    /// <summary>
    /// Llamado por el botón de "Terminar Turno" de la UI.
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
    /// Lógica de tu juego: Otorga recursos al jugador activo al inicio de su turno.
    /// </summary>
    private void CollectTurnResources(int playerID)
    {
        Debug.Log($"Recolectando recursos para el jugador {playerID}...");
        // 1. Encuentra todas las ciudades/asentamientos que pertenecen a 'playerID'.
        // 2. Para cada ciudad, obtén la 'HexTile' en la que está.
        // 3. Añade 1 recurso de 'hexTile.resourceType' al inventario del jugador.
    }

    private void SetUp() {
        _gridGenerator.SetUp();
    }
}