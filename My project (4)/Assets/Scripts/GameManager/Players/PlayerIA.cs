using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PlayerArmyManager))]
public class PlayerIA : Player
{
    // --- ESTRUCTURAS DE DATOS (Nexo con el futuro GOAP) ---
    
    public struct AIGoal
    {
        public AIGoalType type;
        public Vector2Int targetCoordinates;
        public TypeUnit unitToProduce; 
    }

    [Header("Cerebros")]
    private AIAnalysisManager aiAnalysis;
    public AI_General generalBrain;
    private PlayerArmyManager myArmyManager;

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
        if (aiAnalysis != null) aiAnalysis.CalculateBaseMaps(this.playerID);
        yield return null; 

        yield return new WaitForSeconds(3f);

        if (generalBrain != null) generalBrain.DecideStrategy();

        yield return AssignAndExecuteGoals();

        // 4. FIN
        Debug.Log("🔴 IA: Fin de turno.");
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.EndAITurn(); 
    }

    private IEnumerator AssignAndExecuteGoals()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            AIGoal goal = CalculateGoalForUnit(unit);

            switch (goal.type)
            {
                case AIGoalType.Expand:
                    Debug.Log($"🤖 IA: Ordenando a {unit.name} expandir en {goal.targetCoordinates}");
                    yield return MoveAndBuildRoutine(unit, goal.targetCoordinates);
                    break;

                case AIGoalType.ProduceUnit:
                    Debug.Log($"🏭 IA: Ciudad {unit.name} intentando producir {goal.unitToProduce}");
                    ExecuteProductionLogic(unit, goal.unitToProduce);
                    yield return new WaitForSeconds(0.5f); // Pequeña pausa entre producciones
                    break;
                
                // (Aquí añadirías Attack y Defend en el futuro)
            }
        }
    }

    // --- CEREBRO TÁCTICO (Decide QUÉ hacer) ---

    private AIGoal CalculateGoalForUnit(Unit unit)
    {
        AIGoal goal = new AIGoal { type = AIGoalType.None };

        // A. SI ES CIUDAD (Tiene UnitRecruiter)
        // Usamos UnitRecruiter para detectar si es una "fábrica"
        if (unit.GetComponent<UnitRecruiter>() != null)
        {
            // Decidimos qué producir según el estado del General
            if (generalBrain.currentTacticalState == TacticalState.EarlyExpansion)
            {
                // En expansión, priorizamos Colonos
                goal.type = AIGoalType.ProduceUnit;
                goal.unitToProduce = TypeUnit.Colono;
            }
            else if (generalBrain.currentStrategicState == StrategicState.War)
            {
                // En guerra, priorizamos tropas (ej: Caballero)
                goal.type = AIGoalType.ProduceUnit;
                goal.unitToProduce = TypeUnit.Caballero;
            }
            return goal;
        }

        // B. SI ES COLONO (Tiene UnitBuilder)
        if (unit.GetComponent<UnitBuilder>() != null)
        {
            // Solo expande si estamos en modo expansión
            if (generalBrain.currentTacticalState == TacticalState.EarlyExpansion)
            {
                Vector2Int? bestSpot = aiAnalysis.GetBestPositionForExpansion();
                if (bestSpot.HasValue)
                {
                    goal.type = AIGoalType.Expand;
                    goal.targetCoordinates = bestSpot.Value;
                }
            }
            return goal;
        }

        return goal;
    }

    // --- EJECUTORES (El "Cómo") ---

    private void ExecuteProductionLogic(Unit city, TypeUnit unitType)
    {
        UnitRecruiter recruiter = city.GetComponent<UnitRecruiter>();
        if (recruiter == null) return;

        // IMPORTANTE: Asegúrate de que UnitRecruiter use los recursos DE LA IA,
        // no del 'humanPlayer'.
        
        switch (unitType)
        {
            case TypeUnit.Colono:
                recruiter.ConstruirColono(city);
                break;
            case TypeUnit.Caballero:
                recruiter.ConstruirCaballero(city); // O Artillero según prefieras
                break;
            case TypeUnit.Artillero:
                recruiter.ConstruirArtillero(city);
                break;
             // Añadir Guerrero si existe
        }
    }

    private IEnumerator MoveAndBuildRoutine(Unit unit, Vector2Int targetCoords)
    {
        // (Esta es la misma rutina de movimiento que ya tenías y funcionaba bien)
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement == null) yield break;

        CellData targetCell = BoardManager.Instance.GetCell(targetCoords);
        if (targetCell == null || targetCell.visualTile == null) yield break;

        bool isMoving = movement.IntentarMover(targetCell.visualTile);

        if (isMoving)
        {
            yield return new WaitForSeconds(5f);
            if (unit.misCoordenadasActuales == targetCoords)
            {
                UnitBuilder builder = unit.GetComponent<UnitBuilder>();
                if (builder != null)
                {
                    builder.IntentarConstruirPoblado();
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
    }
}