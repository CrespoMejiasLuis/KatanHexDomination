using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GoapAgent : MonoBehaviour
{
    [Header("Configuracion")]
    public float actionDelay = 0.2f; // pausa entre acciones para que se vea natural

    // --- REFERENCIAS ---
    private GoapPlanner planner;
    private HashSet<GoapAction> availableActions;
    private Queue<GoapAction> currentActions; // La cola de acciones del plan actual
    private GoapAction currentAction;         // La accion que se esta ejecutando AHORA

    // --- DATOS COMPARTIDOS (Blackboard) ---
    // Aqui guardamos datos que las acciones necesitan leer
    [HideInInspector] 
    public Vector2Int targetDestination; // PlayerIA asigna esto antes de pedir un plan de movimiento

    // --- ESTADO DEL MUNDO (Memoria del Agente) ---
    // Se resetea y actualiza antes de cada planificacion
    public Dictionary<string, int> worldState = new Dictionary<string, int>();

    // --- SINCRONIZACIO DE TURNOS ---
    // Esta propiedad es la que mira el PlayerIA en 'AreAllUnitsIdle()'
    public bool IsActing { get; private set; } = false;

    void Start()
    {
        planner = new GoapPlanner();
        // Recopila todas las acciones (scripts que heredan de GoapAction) en este objeto
        availableActions = new HashSet<GoapAction>(GetComponents<GoapAction>());
    }

    void Update()
    {
        // Si no estamos actuando, no hacemos nada (ahorramos CPU)
        if (!IsActing) return;

        // MAQUINA DE ESTADOS FINITA PARA LA EJECUCION DEL PLAN

        // 1. ESTADO: EJECUTANDO ACCION
        if (currentAction != null && currentAction.running)
        {
            // Perform devuelve 'true' cuando la accion ha terminado exitosamente
            if (currentAction.Perform(gameObject)) 
            {
                currentAction.running = false;
                // La accion termino, en el siguiente frame buscaremos la siguiente en la cola
            }
            return; // Esperar al siguiente frame
        }

        // 2. ESTADO: BUSCANDO SIGUIENTE ACCION
        if (currentActions != null && currentActions.Count > 0)
        {
            // Sacamos la siguiente accion de la cola
            currentAction = currentActions.Dequeue();
            
            // Verificamos si TODAViA es valido ejecutarla (Procedural Check)
            // Ej: Iba a construir, pero me robaron los recursos mientras me movia.
            if (currentAction.CheckProceduralPrecondition(gameObject))
            {
                currentAction.running = true;
                
                // Opcional: Asignar el target fisico si la accion lo requiere
                // Si la accion es moverse, el target lo gestiona la propia accion leyendo 'targetDestination'
            }
            else
            {
                // El plan falló a mitad de camino. Abortar.
                Debug.LogWarning($"GOAP: Plan interrumpido. La accion {currentAction.GetType().Name} ya no es valida.");
                AbortPlan();
            }
            return;
        }

        // 3. ESTADO: PLAN TERMINADO (Cola vacia y sin accion en curso)
        if (IsActing) 
        {
            Debug.Log($"GOAP: {name} ha completado su plan exitosamente.");
            IsActing = false; // Avisamos al PlayerIA que terminamos
        }
    }

    //Punto de entrada. PlayerIA llama a esto para darle una orden a la unidad.
    public void SetGoal(Dictionary<string, int> goal)
    {
        // 1. Construir la vision actual del mundo
        UpdateWorldState();

        // 2. Pedir un plan al Planner (A* sobre el grafo de acciones)
        Queue<GoapAction> plan = planner.Plan(gameObject, availableActions, worldState, goal);

        if (plan != null && plan.Count > 0)
        {
            currentActions = plan;
            currentAction = null; // Resetear accion actual
            IsActing = true;      //  Empezamos a trabajar
            
            Debug.Log($"GOAP: Plan generado para {name} con {plan.Count} pasos.");
        }
        else
        {
            Debug.LogWarning($"GOAP: {name} no pudo encontrar un plan para el objetivo dado.");
            IsActing = false; // Asegurarnos de no bloquear el turno si falla el plan
        }
    }

    // Detiene todo inmediatamente.
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

        // Obtener referencias
        Unit unit = GetComponent<Unit>();
        Player player = GameManager.Instance.GetPlayer(unit.ownerID); // Asume que tienes este método o similar

        // --- DATOS GENERALES ---
        
        // 1.  recursos para construir? (especifico por accion)
        // Esto es util si tienes acciones genericas, pero Action_ConstruirPoblado ya hace su propio chequeo.
        // worldState.Add("TieneRecursos", player.HasEnoughResources(...) ? 1 : 0);

        // 2. lugar valido?
        // Esto es difícil de saber genéricamente, normalmente la acción de movimiento 
        // establece el estado "EnElDestino" como efecto.
        
        // Sin embargo, si YA ESTAMOS en el destino asignado, lo marcamos:
        if (unit.misCoordenadasActuales == targetDestination)
        {
            // Si ya estamos ahí, el planificador sabrá que puede saltarse la acción de moverse
            worldState.Add("EnElDestino", 1); 
        }
        else
        {
            worldState.Add("EnElDestino", 0);
        }

        // sumar mas estados
        // worldState.Add("EnemigoALaVista", ...);
        // worldState.Add("SaludBaja", unit.vidaActual < 10 ? 1 : 0);
    }
}