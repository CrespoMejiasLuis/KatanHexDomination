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


    public Dictionary<ResourceType, int> GetResources()
    {
        return resources;
    }
    protected override void Awake()
    {
        base.Awake();
        aiAnalysis = FindFirstObjectByType<AIAnalysisManager>();
        myArmyManager = GetComponent<PlayerArmyManager>();
        generalBrain.myPlayer = this;
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
        
        yield return new WaitForSeconds(3f); // pausa para "pensar"

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
            switch(generalBrain.CurrentOrder){
                case TacticalAction.EarlyExpansion:
                    goal.Add("ColonoProducido", 1);
                    break;
                
                case TacticalAction.Development:
                    if (unit.statsBase.nombreUnidad == TypeUnit.Poblado)
                    {
                        goal.Add("Mejorar_A_Ciudad", 1); // <-- Necesitarás una Action_MejorarCiudad con este efecto
                    }
                    // Si ya es ciudad, quizás reclutar trabajadores o soldados
                    else 
                    {
                        goal.Add("TropaProducida", 1);
                    }
                    break;

                case TacticalAction.Assault:
                    goal.Add("ArqueroProducido", 1);
                    break;

                case TacticalAction.ActiveDefense:
                    goal.Add("CaballeroProducido", 1);
                    break;
            }
            
            
            return goal;
        }

        // --- B. COLONOS (Constructores) ---
        if (unit.statsBase.nombreUnidad == TypeUnit.Colono)
        {
            if (generalBrain.CurrentOrder == TacticalAction.EarlyExpansion || generalBrain.CurrentOrder == TacticalAction.Development)
            {
                // 1. Encontrar el mejor lugar (Datos para la acción)
                Vector2Int? bestSpot = aiAnalysis.GetBestPositionForExpansion(unit, this);
                GoapAgent agent = unit.GetComponent<GoapAgent>();

                if (bestSpot.HasValue && agent != null)
                {
                    // Asignar el destino calculado
                    agent.targetDestination = bestSpot.Value;
                    
                    Debug.Log($"🏗️ Colono en {unit.misCoordenadasActuales} asignado a construir en {bestSpot.Value}");

                    // 2. Establecer el Objetivo GOAP
                    goal.Add("PobladoConstruido", 1);
                }
                else
                {
                    Debug.LogWarning($"⚠️ No se encontró ubicación válida para colono en {unit.misCoordenadasActuales}");
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