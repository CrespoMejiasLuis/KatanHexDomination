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

        path = new List<Vector2Int>();

        actionType = ActionType.Mover;
        cost = 10.0f;
        rangeInTiles = 1; // Un paso adyacente
        requiresInRange = false; 
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if(path != null)
        {
            path.Clear();
        }

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
            Debug.Log("Numero de camino:" + path.Count);
            if(path == null || path.Count == 0) return false;


            if(path.Count <= 0)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public override bool Perform(GameObject agent)
    {
        // 1. Validaciones básicas
        if (movementComponent == null) { DoReset(); return true; }

        // Si no hay camino o no quedan movimientos, terminamos (con éxito o fracaso)
        if (path == null || path.Count == 0 || unitAgent.movimientosRestantes <= 0)
        {
            running = false;
            return true; // Acción terminada
        }

        // 2. Esperar si la unidad se está moviendo visualmente
        // (Necesitas hacer pública la variable 'isMoving' en UnitMovement o crear un getter)
        if (movementComponent.isMoving) // Asegúrate que 'isMoving' sea public o usa un método IsMoving()
        {
            return false; // Seguimos en esta acción, esperando
        }
        if (path.Count > 0 && path[0] == unitAgent.misCoordenadasActuales)
        {
            path.RemoveAt(0);
        }

        // Si después de limpiar no queda camino, terminamos.
        if (path.Count == 0)
        {
            running = false;
            return true;
        }
        // 3. Iniciar el siguiente paso
        CellData cellTarget = BoardManager.Instance.GetCell(path[0]);

        if (cellTarget != null && cellTarget.visualTile != null)
        {
            // Intentamos mover
            bool movementStarted = movementComponent.IntentarMover(cellTarget.visualTile);

            if (movementStarted)
            {
                path.RemoveAt(0); // Consumimos el paso
                return false;     // Seguimos en la acción (ahora isMoving será true)
            }
            else
            {
                // Si falla (ej. casilla ocupada de repente), abortamos
                Debug.Log("MoverAction: Falló al intentar mover. Abortando.");
                running = false;
                return true;
            }
        }

        return true; // Fallback
    }
}