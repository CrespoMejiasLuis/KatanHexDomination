using UnityEngine;
using System.Collections.Generic;

public class MoveToCombatPositionAction : GoapAction
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

        actionType = ActionType.MoveToCombat;  // ← Enum específico
        cost = 1.0f; // Bajo costo para que sea preferido
        rangeInTiles = 1;
        requiresInRange = false;

        if (!Effects.ContainsKey("IsAtCombatPosition"))
            Effects.Add("IsAtCombatPosition", 1);
        
        // 🎯 FIX: Marcar que estamos en rango de ataque para que AttackAction pueda ejecutarse
        // Esto permite que el planificador encadene Mover -> Atacar
        if (!Effects.ContainsKey("EnRangoDeAtaque"))
            Effects.Add("EnRangoDeAtaque", 1);
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (path != null)
        {
            path.Clear();
        }

        if (goapAgent == null || BoardManager.Instance == null || unitAgent == null || unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        Vector2Int start = unitAgent.misCoordenadasActuales;
        Vector2Int goal = goapAgent.targetDestination;

        // Si ya estamos en el destino, no hace falta moverse
        if (start == goal)
        {
            return false;
        }

        if (Pathfinding.Instance != null)
        {
            // 🎯 USAR NUEVO PATHFINDING DE COMBATE
            // Ignora amenazas y evita unidades
            path = Pathfinding.Instance.FindPathForCombat(start, goal);
            
            if (path == null || path.Count <= 0)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public override bool Perform(GameObject agent)
    {
        // Validaciones básicas
        if (movementComponent == null) 
        { 
            DoReset(); 
            return true; 
        }

        // Si no hay camino o no quedan movimientos, terminamos
        if (path == null || path.Count == 0 || unitAgent.movimientosRestantes <= 0)
        {
            running = false;
            return true;
        }

        // Esperar si la unidad se está moviendo visualmente
        if (movementComponent.isMoving)
        {
            return false; // Seguimos esperando
        }

        // Limpiar casilla actual del path si ya estamos ahí
        if (path.Count > 0 && path[0] == unitAgent.misCoordenadasActuales)
        {
            path.RemoveAt(0);
        }

        // Si no queda camino, terminamos
        if (path.Count == 0)
        {
            running = false;
            return true;
        }

        // Iniciar el siguiente paso de movimiento
        CellData cellTarget = BoardManager.Instance.GetCell(path[0]);

        if (cellTarget != null && cellTarget.visualTile != null)
        {
            bool movementStarted = movementComponent.IntentarMover(cellTarget.visualTile);

            if (movementStarted)
            {
                return false; // Seguir ejecutando (esperando que termine el movimiento)
            }
            else
            {
                // Si falla (casilla ocupada, etc.), abortamos
                Debug.LogWarning($"MoveToCombatPosition: No se pudo mover a {path[0]}. Abortando.");
                running = false;
                return true;
            }
        }

        return true;
    }
}
