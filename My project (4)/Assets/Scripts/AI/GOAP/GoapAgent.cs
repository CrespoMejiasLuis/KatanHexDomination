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
    private Dictionary<string, int> lastGoal;

    // 🔧 FIX CRÍTICO #1: Protección contra re-planning infinito
    private int failedPlanAttempts = 0;
    private const int MAX_PLAN_ATTEMPTS = 3;

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
                Debug.Log($"[OK] ACCIÓN COMPLETADA: {currentAction.GetType().Name}. Pasando a la siguiente.");
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
                
                // 🔧 FIX CRÍTICO #1: Incrementar contador de fallos
                failedPlanAttempts++;
                AbortPlan();
                
                // Solo reintentar si no hemos superado el límite
                if (failedPlanAttempts < MAX_PLAN_ATTEMPTS && lastGoal != null)
                {
                    Debug.Log($"[WARNING] GOAP: Reintento {failedPlanAttempts}/{MAX_PLAN_ATTEMPTS} - Recalculando plan...");
                    SetGoal(lastGoal);
                }
                else
                {
                    Debug.LogWarning($"[ERROR] GOAP: {name} falló {failedPlanAttempts} veces. Abandonando objetivo para evitar bucle infinito.");
                    failedPlanAttempts = 0;
                    // La unidad queda idle hasta que PlayerIA le asigne nuevo objetivo en el próximo turno
                }
            }
            return;
        }

        // 3. ESTADO: PLAN TERMINADO (Cola vacía y sin acción en curso)
        if (IsActing)
        {
            Debug.Log($"[SUCCESS] GOAP: {name} ha completado su plan exitosamente.");
            IsActing = false;
        }
    }

    // Punto de entrada. PlayerIA llama a esto para darle una orden a la unidad.
    public void SetGoal(Dictionary<string, int> goal)
    {
        // 🔧 FIX CRÍTICO #1: Resetear contador si el objetivo es diferente
        if (!GoalsAreEqual(lastGoal, goal))
        {
            failedPlanAttempts = 0;
            Debug.Log($"[GOAL] GOAP: {name} recibe NUEVO objetivo (reseteando intentos fallidos)");
        }
        
        lastGoal = new Dictionary<string, int>(goal);
        
        Debug.Log($"====================================================");
        Debug.Log($"[GOAL] GOAP AGENT: {name} recibe nuevo objetivo");
        Debug.Log($"====================================================");
        
        // 1. Construir la vision actual del mundo
        UpdateWorldState();

        string stateLog = "[WORLD] ESTADO DEL MUNDO: ";
        foreach(var kvp in worldState) stateLog += $"[{kvp.Key}:{kvp.Value}] ";
        Debug.Log(stateLog);

        string goalLog = "[GOAL] OBJETIVO ASIGNADO: ";
        foreach(var kvp in goal) goalLog += $"[{kvp.Key}:{kvp.Value}] ";
        Debug.Log(goalLog);

        // 2. Pedir un plan al Planner (A* sobre el grafo de acciones)
        Queue<GoapAction> plan = planner.Plan(gameObject, availableActions, worldState, goal);

        if (plan != null && plan.Count > 0)
        {
            currentActions = plan;
            currentAction = null;
            IsActing = true;

            Debug.Log($"[PLAN] GOAP AGENT: Plan generado para {name} con {plan.Count} pasos:");
            int stepNum = 1;
            foreach(var action in plan)
            {
                Debug.Log($"   Paso {stepNum}: {action.GetType().Name}");
                stepNum++;
            }
        }
        else
        {
            Debug.LogWarning($"[NO PLAN] GOAP AGENT: {name} NO pudo encontrar un plan para el objetivo:");
            foreach(var kv in goal)
            {
                Debug.LogWarning($"   - Requiere: [{kv.Key}] = {kv.Value}");
            }
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


    
    // 🔧 FIX CRÍTICO #1: Función auxiliar para comparar objetivos
    private bool GoalsAreEqual(Dictionary<string, int> goal1, Dictionary<string, int> goal2)
    {
        if (goal1 == null || goal2 == null) return false;
        if (goal1.Count != goal2.Count) return false;
        
        foreach (var kvp in goal1)
        {
            if (!goal2.ContainsKey(kvp.Key) || goal2[kvp.Key] != kvp.Value)
                return false;
        }
        return true;
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
            // 🔧 FIX: Coste de poblados SIN incremento lineal
            // El coste se mantiene constante en el valor base
            Dictionary<ResourceType, int> pobladoCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Madera, 1 }, { ResourceType.Oveja, 1 },
                { ResourceType.Trigo, 1 }, { ResourceType.Arcilla, 1 }
            };

            // ELIMINADO: Escalado lineal que causaba costes prohibitivos
            // if(playerAgent.numPoblados > 1)
            // {
            //     pobladoCost = unit.actualizarCostes(pobladoCost, playerAgent);
            // }

            bool canAffordPoblado = playerAgent.CanAfford(pobladoCost);

            // Si el jugador tiene recursos, el planificador lo sabe (TieneRecursosParaPoblado = 1)
            worldState.Add("TieneRecursosParaPoblado", canAffordPoblado ? 1 : 0);
        }

        if (playerAgent != null)
        {
            // Chequear si el jugador tiene suficientes recursos para construir UN poblado
            // (1 Madera, 1 Oveja, 1 Trigo, 1 Arcilla)
            var ciudadCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Roca, 3 }, 
                { ResourceType.Trigo, 2 }
            };

            if(playerAgent.numPoblados > 1)
            {
                ciudadCost = unit.actualizarCostes(ciudadCost, playerAgent);
            }

            bool canAffordCiudad = playerAgent.CanAfford(ciudadCost);

            // Si el jugador tiene recursos, el planificador lo sabe (TieneRecursosParaPoblado = 1)
            worldState.Add("TieneRecursosParaCiudad", canAffordCiudad ? 1 : 0);
        }

        if (playerAgent != null)
        {
            // Recursos para Colono
            var colonoCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Oveja, 1 },
                { ResourceType.Trigo, 1 }
            };

            if(playerAgent.numPoblados > 1)
            {
                colonoCost = unit.actualizarCostes(colonoCost, playerAgent);
            }

            bool canAffordColono = playerAgent.CanAfford(colonoCost);
            worldState.Add("TieneRecursosParaColono", canAffordColono ? 1 : 0);

            // 🔧 FIX CRÍTICO #3: Recursos para Arquero (Artillero)
            var arqueroCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Roca, 1 },
                { ResourceType.Madera, 1 }
            };

            if(playerAgent.numPoblados > 1)
            {
                arqueroCost = unit.actualizarCostes(arqueroCost, playerAgent);
            }

            bool canAffordArquero = playerAgent.CanAfford(arqueroCost);
            worldState.Add("TieneRecursosParaArquero", canAffordArquero ? 1 : 0);

            // 🔧 FIX CRÍTICO #3: Recursos para Caballero
            var caballeroCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Roca, 1 },
                { ResourceType.Oveja, 1 }
            };

            if(playerAgent.numPoblados > 1)
            {
                caballeroCost = unit.actualizarCostes(caballeroCost, playerAgent);
            }

            bool canAffordCaballero = playerAgent.CanAfford(caballeroCost);
            worldState.Add("TieneRecursosParaCaballero", canAffordCaballero ? 1 : 0);
        }

        // --- 3. ESTADO DE SEGURIDAD ---
        // 🔧 FIX CRÍTICO #2 y #4: Añadir worldState "Seguro" para objetivos de combate
        bool isSafe = true;
        
        // Chequeo 1: Salud baja = NO seguro
        if (unit != null && unit.statsBase != null)
        {
            float healthPercent = unit.vidaActual / (float)unit.statsBase.vidaMaxima;
            if (healthPercent < 0.4f)
            {
                isSafe = false;
            }
        }

        // Chequeo 2: Amenazas cercanas = NO seguro
        if (GameManager.Instance?.aiAnalysis?.threatMap != null && BoardManager.Instance != null)
        {
            int gridRadius = BoardManager.Instance.gridRadius;
            int x = unit.misCoordenadasActuales.x + (gridRadius - 1);
            int y = unit.misCoordenadasActuales.y + (gridRadius - 1);
            
            var threatMap = GameManager.Instance.aiAnalysis.threatMap;
            if (x >= 0 && y >= 0 && x < threatMap.GetLength(0) && y < threatMap.GetLength(1))
            {
                float localThreat = threatMap[x, y];
                if (localThreat > 30f) // Umbral de peligro
                {
                    isSafe = false;
                }
            }
        }

        worldState.Add("Seguro", isSafe ? 1 : 0);
    }
}