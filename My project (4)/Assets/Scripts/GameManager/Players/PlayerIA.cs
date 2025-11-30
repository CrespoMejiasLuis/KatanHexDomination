using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PlayerArmyManager))]
public class PlayerIA : Player
{
    [Header("Cerebros")]
    private AIAnalysisManager aiAnalysis;
    public AI_General generalBrain;
    private PlayerArmyManager myArmyManager;

    // Ya no necesitamos la struct AIGoal porque usaremos Diccionarios para GOAP
    // pero mantenemos la lógica de decisión estratégica.
    public Dictionary<ResourceType, int> GetResources()
    {
        return resources;
    }
    protected override void Awake()
    {
        base.Awake();
        aiAnalysis = FindFirstObjectByType<AIAnalysisManager>();
        myArmyManager = GetComponent<PlayerArmyManager>();
    }

    public override void BeginTurn()
    {
        Debug.Log($"🟢 --- INICIO TURNO IA ({playerName}) ---");
        StartCoroutine(ExecuteAITurn());
    }

    private IEnumerator ExecuteAITurn()
    {
        // 1. Analisis del Tablero
        if (aiAnalysis != null) aiAnalysis.CalculateBaseMaps(this.playerID);
        yield return null; 

        // 2. Decision Estrategica Global (FSM)
        if (generalBrain != null) generalBrain.DecideStrategy();
        
        yield return new WaitForSeconds(1f); // pausa para "pensar"

        // 3. Asignacion de Objetivos GOAP a las Unidades
        AssignGoapGoals();
        yield return null;
       
        // 4. FIN DEL TURNO DE IA
        // En un sistema GOAP real, la IA podria esperar a que las unidades terminen,
        // pero en turnos, asignamos los planes y dejamos que se ejecuten.
        Debug.Log("⏳ IA: Esperando a que las unidades terminen...");
        
        // Si una unidad se bugea y nunca termina, pasamos turno a los 10 segundos para no colgar el juego.
        float timeOut = 10f;
        while (!AreAllUnitsIdle() && timeOut > 0)
        {
           // ReassignGoalsToIdleUnits();
            timeOut -= Time.deltaTime;
            yield return null;
        }

        // 5. FIN
        Debug.Log("🔴 IA: Todas las unidades terminaron. Fin de turno.");
        GameManager.Instance.EndAITurn();
        
        // Simulamos tiempo de ejecucion de las acciones
        yield return new WaitForSeconds(2f); 

        Debug.Log("🔴 IA: Fin de turno. Pasando al jugador.");
        GameManager.Instance.EndAITurn(); 
    }

    // Itera sobre todas las unidades y les asigna un objetivo GOAP basado en la estrategia actual.
    private void AssignGoapGoals()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            // Obtener el Agente GOAP de la unidad
            GoapAgent agent = unit.GetComponent<GoapAgent>();
            if (agent == null)
            {
                Debug.LogWarning($"La unidad {unit.name} de la IA no tiene GoapAgent.");
                continue;
            }

            // Calcular el objetivo para esta unidad
            Dictionary<string, int> goal = CalculateGoapGoal(unit);

            // Asignar el objetivo al agente (esto dispara el Planner)
            if (goal != null && goal.Count > 0)
            {
                agent.SetGoal(goal);
            }
        }
    }

    //calcula objetivo de cada unidad
    private Dictionary<string, int> CalculateGoapGoal(Unit unit)
    {
        Dictionary<string, int> goal = new Dictionary<string, int>();


        // --- A. CIUDADES / RECLUTADORES ---
        if (unit.statsBase.nombreUnidad == TypeUnit.Poblado || unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
        {
            // Estrategia: EXPANSIÓN -> Construir Colono
            if (generalBrain.CurrentOrder == TacticalAction.EarlyExpansion)
            {
                // El objetivo GOAP es "Tener Un Colono Producido"
                // La acción Action_ProducirColono tendrá este efecto.
                goal.Add("ColonoProducido", 1); 
            }
            // Estrategia: GUERRA / DEFENSA -> Construir Tropa
            else if (generalBrain.CurrentOrder == TacticalAction.ActiveDefense || 
                     generalBrain.CurrentOrder == TacticalAction.Assault)
            {
                goal.Add("TropaProducida", 1);
            }
            return goal;
        }

        // --- B. COLONOS (Constructores) ---
        if (unit.statsBase.nombreUnidad == TypeUnit.Colono)
        {
            if (generalBrain.CurrentOrder == TacticalAction.EarlyExpansion)
            {
                // 1. Encontrar el mejor lugar (Datos para la acción)
                Vector2Int? bestSpot = aiAnalysis.GetBestPositionForExpansion();
                GoapAgent agent = unit.GetComponent<GoapAgent>();
                // En PlayerIA.CalculateGoapGoal, fuerza esto:
                agent.targetDestination = unit.misCoordenadasActuales + new Vector2Int(1, 0);

                if (bestSpot.HasValue && agent!=null)
                {
                    // PASO CRITICO: Asignar el destino a la unidad para que Action_Moverse sepa dónde ir.
                    // Necesitarás añadir una variable 'targetDestination' a tu script Unit o GoapAgent.
                    agent.targetDestination = bestSpot.Value; 

                    // 2. Establecer el Objetivo GOAP
                    goal.Add("PobladoConstruido", 1);
                }
            }
            return goal;
        }

        // --- C. TROPAS DE COMBATE ---
        if(unit.statsBase.nombreUnidad == TypeUnit.Artillero || unit.statsBase.nombreUnidad == TypeUnit.Caballero || unit.statsBase.nombreUnidad == TypeUnit.Caballeria)
        {

            goal.Add("Seguro", 1);///mirar
        }
        // (logica para Soldados, Caballeros, etc.)
        // if (unit.EsTropaCombatiente) ...

        return goal; // Objetivo vacio si no hay nada que hacer
    }

    //compruba si alguan unidad de mi ejercito hace cosas
    private bool AreAllUnitsIdle()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            GoapAgent agent = unit.GetComponent<GoapAgent>();
            
            // Si tiene agente y dice que esta actuando, NO hemos terminado.
            if (agent != null && agent.IsActing)
            {
                return false; 
            }
            
            // Si la unidad se esta moviendo visualmente 
            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null && movement.isMoving) 
            {
                return false;
            } 
        }

        // Si nadie esta actuando
        return true; 
    }
}