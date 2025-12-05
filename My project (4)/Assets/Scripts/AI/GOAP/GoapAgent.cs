using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GoapAgent : MonoBehaviour
{
    [Header("Configuracion")]
    public float actionDelay = 0.2f;

    // --- REFERENCIAS ---
    private GoapPlanner planner;
    private HashSet<GoapAction> availableActions;
    private Queue<GoapAction> currentActions;
    private GoapAction currentAction;

    // --- DATOS COMPARTIDOS (Blackboard) ---
    [HideInInspector]
    public Vector2Int targetDestination;
    private Dictionary<string, int> lastGoal; // <-- NUEVA VARIABLE------------------------------

    // --- ESTADO DEL MUNDO (Memoria del Agente) ---
    public Dictionary<string, int> worldState = new Dictionary<string, int>();

    // --- SINCRONIZACIO DE TURNOS ---
    public bool IsActing { get; private set; } = false;

    void Start()
    {
        planner = new GoapPlanner();
        availableActions = new HashSet<GoapAction>(GetComponents<GoapAction>());
    }

    void Update()
    {
        if (!IsActing) return;

        // 1. ESTADO: EJECUTANDO ACCION
        if (currentAction != null && currentAction.running)
        {
            if (currentAction.Perform(gameObject))
            {
                Debug.Log($"✅ ACCIÓN COMPLETADA: {currentAction.GetType().Name}. Pasando a la siguiente.");
                currentAction.running = false;
            }
            return;
        }

        // 2. ESTADO: BUSCANDO SIGUIENTE ACCION
        if (currentActions != null && currentActions.Count > 0)
        {
            currentAction = currentActions.Dequeue();

            if (currentAction.CheckProceduralPrecondition(gameObject))
            {
                currentAction.running = true;
            }
            else
            {
                // El plan falló a mitad de camino. Abortar.
                Debug.LogWarning($"GOAP: Plan interrumpido. La acción {currentAction.GetType().Name} ya no es válida. Abortando plan.");
                AbortPlan();
                if (lastGoal != null)
                {
                    // Debe hacerse en el siguiente frame para evitar problemas de recursividad
                    // y permitir que el sistema se estabilice, pero llamaremos a SetGoal directamente 
                    // ya que el AbortPlan puso IsActing en false.
                    SetGoal(lastGoal);
                }
            }
            return;
        }

        // 3. ESTADO: PLAN TERMINADO (Cola vacía y sin acción en curso)
        if (IsActing)
        {
            Debug.Log($"🎉 GOAP: {name} ha completado su plan exitosamente.");
            IsActing = false;
        }
    }

    // Punto de entrada. PlayerIA llama a esto para darle una orden a la unidad.
    public void SetGoal(Dictionary<string, int> goal)
    {
        lastGoal = new Dictionary<string, int>(goal);
        // 1. Construir la vision actual del mundo
        UpdateWorldState();

        string stateLog = "WorldState: ";
        foreach(var kvp in worldState) stateLog += $"[{kvp.Key}:{kvp.Value}] ";
        Debug.Log(stateLog);

        string goalLog = "Goal: ";
        foreach(var kvp in goal) goalLog += $"[{kvp.Key}:{kvp.Value}] ";
        Debug.Log(goalLog);

        // 2. Pedir un plan al Planner (A* sobre el grafo de acciones)
        Queue<GoapAction> plan = planner.Plan(gameObject, availableActions, worldState, goal);

        if (plan != null && plan.Count > 0)
        {
            currentActions = plan;
            currentAction = null;
            IsActing = true;

            Debug.Log($"GOAP: Plan generado para {name} con {plan.Count} pasos.");
        }
        else
        {
            Debug.Log($"GOAP: {name} no pudo encontrar un plan para el objetivo {string.Join(", ", goal.Select(kv => $"{kv.Key}={kv.Value}"))}.");
            IsActing = false;
        }
    }

    public void AbortPlan()
    {
        currentActions = null;
        if (currentAction != null)
        {
            currentAction.DoReset();
            currentAction = null;
        }
        IsActing = false;
    }


    // Traduce los datos del juego (Unit, Player, Grid) al lenguaje del GOAP (Strings).
    private void UpdateWorldState()
    {
        worldState.Clear();

        Unit unit = GetComponent<Unit>();
        // Asumimos que GameManager.Instance.GetPlayer() devuelve el objeto PlayerIA/Player
        Player playerAgent = GameManager.Instance.GetPlayer(unit.ownerID);

        // --- 1. ESTADO DE POSICIÓN ---
        
        if (unit.misCoordenadasActuales == targetDestination)
        {
            worldState.Add("EstaEnRango", 1);
        }
        else
        {
            worldState.Add("EstaEnRango", 0);
        }

        // --- 2. ESTADO DE RECURSOS (Clave para construir) ---
        if (playerAgent != null)
        {
            // Chequear si el jugador tiene suficientes recursos para construir UN poblado
            // (1 Madera, 1 Oveja, 1 Trigo, 1 Arcilla)
            var pobladoCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Madera, 1 }, { ResourceType.Oveja, 1 },
                { ResourceType.Trigo, 1 }, { ResourceType.Arcilla, 1 }
            };

            bool canAffordPoblado = playerAgent.CanAfford(pobladoCost);

            // Si el jugador tiene recursos, el planificador lo sabe (TieneRecursosParaPoblado = 1)
            worldState.Add("TieneRecursosParaPoblado", canAffordPoblado ? 1 : 0);
        }

        if (playerAgent != null)
        {
            // Chequear si el jugador tiene suficientes recursos para construir UN poblado
            // (1 Madera, 1 Oveja, 1 Trigo, 1 Arcilla)
            var colonoCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Oveja, 1 },
                { ResourceType.Trigo, 1 }
            };

            bool canAffordColono = playerAgent.CanAfford(colonoCost);

            // Si el jugador tiene recursos, el planificador lo sabe (TieneRecursosParaPoblado = 1)
            worldState.Add("TieneRecursosParaColono", canAffordColono ? 1 : 0);
        }

        // --- 3. OTROS ESTADOS (Ej: Estructuras) ---
        // (Lógica para chequear si la casilla tiene poblado/ciudad)
        // CellData currentCell = BoardManager.Instance.GetCell(unit.misCoordenadasActuales);
        // if(currentCell != null)
        // {
        //     worldState.Add("TienePoblado", currentCell.hasSettlement ? 1 : 0);
        // }
    }
}