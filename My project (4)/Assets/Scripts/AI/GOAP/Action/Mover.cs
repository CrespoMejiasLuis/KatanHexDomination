using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para la función de selección de ruta simple

public class MoverAction : GoapAction
{
    private UnitMovement movementComponent;
    private GoapAgent goapAgent;
    private List<Vector2Int> path;

    protected override void Awake()
    {
        base.Awake();

        movementComponent = unitAgent.GetComponent<UnitMovement>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Mover;
        cost = 10.0f;
        rangeInTiles = 1; // Un paso adyacente
        requiresInRange = false; 
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        path.Clear();
        if (goapAgent == null || BoardManager.Instance == null || unitAgent == null || unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        Vector2Int currentCoord = unitAgent.misCoordenadasActuales;
        Vector2Int finalCoord = goapAgent.targetDestination;

        // 1. Si ya estamos en el destino final, esta acción no es necesaria.
        if (currentCoord == finalCoord)
        {
            return false;
        }

        //llamamos a A* para comprobar si existe ruta
        Vector2Int start = unitAgent.misCoordenadasActuales;
        Vector2Int goal = goapAgent.targetDestination;

        // Obtener el mapa de amenaza (asumiendo que está en GameManager o AIAnalysisManager)
        float[,] threatMap = GameManager.Instance.aiAnalysis.threatMap; 

        if (Pathfinding.Instance != null)
        {
            path = Pathfinding.Instance.FindSmartPath(start, goal, threatMap);

            if(path.Count <= 0)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (movementComponent == null)
        {
            DoReset();
            return true;
        }

        if(unitAgent.movimientosRestantes <= 0)
        {
            running = false;
            return true;
        }

        while (path.Count() > 0 || unitAgent.movimientosRestantes > 0)
        {
            CellData cellTarget = BoardManager.Instance.GetCell(path[0]);
            bool movementCompleted = movementComponent.IntentarMover(cellTarget.visualTile);

            if (movementCompleted)
            {
                path.RemoveAt(0);
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}