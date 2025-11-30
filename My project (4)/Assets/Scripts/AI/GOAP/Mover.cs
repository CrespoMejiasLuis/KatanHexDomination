using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para la función de selección de ruta simple

public class MoverAction : GoapAction
{
    private UnitMovement movementComponent;
    private GoapAgent goapAgent;
    private HexTile targetTile; // Referencia al Tile específico para IntentarMover

    protected override void Awake()
    {
        base.Awake();

        movementComponent = unitAgent.GetComponent<UnitMovement>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Mover;
        cost = 10.0f;
        rangeInTiles = 1; // Un paso adyacente
        requiresInRange = false;

        preConditionsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "EstaEnRango", value = 0 }
        };

        afterEffectsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "EstaEnRango", value = 1 }
        };
        
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (goapAgent == null || BoardManager.Instance == null || unitAgent == null)
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

        // --- PASO CLAVE: CALCULAR EL PRÓXIMO PASO (Heurística simple) ---
        // En un sistema GOAP, MoverAction solo debe mover un paso.
        // Aquí usamos una heurística simple: buscar la casilla adyacente con la menor distancia al destino final.
        int minDistance = int.MaxValue;
        CellData nextCell = null;
        int finalDistance = BoardManager.Instance.Distance(currentCoord, finalCoord);
        Debug.Log($"GOAP Mover: Buscando ruta de {currentCoord} a {finalCoord}. Distancia actual: {finalDistance}");

        foreach (CellData adjacentCell in BoardManager.Instance.GetAdjacents(currentCoord))
        {
            // CUIDADO: La celda no debe estar ocupada (si es que no es el destino final de la unidad)
            // if (adjacentCell.isOccupied) continue; 

            // Obtener la distancia si eligiera esta casilla
            if (adjacentCell == null || adjacentCell.visualTile == null) continue;
            int distance = BoardManager.Instance.Distance(adjacentCell.coordinates, finalCoord);

            Debug.Log($"   -> Vecino {adjacentCell.coordinates}: Distancia {distance}.");
            minDistance = distance;
            nextCell = adjacentCell;

            if (distance < minDistance)
            {
                
            }
        }

        if (nextCell == null || nextCell.visualTile == null)
        {
            Debug.LogError($"🛑 MoverAction Falló: No se encontró un paso adyacente que mejore la distancia.");
            return false;
        }

        // 3. Asignar el Target Físico (el siguiente paso)
        targetTile = nextCell.visualTile;
        target = targetTile.gameObject;

        // 4. Chequeo de PM (Si el primer paso es viable)
        if (unitAgent.movimientosRestantes <= 0) return false;

        return true;

       
    }

    public override bool Perform(GameObject agent)
    {
        if (targetTile == null || movementComponent == null)
        {
            DoReset();
            return true;
        }

        // 1. Si no estamos moviéndonos, iniciamos el movimiento.
        if (!movementComponent.isMoving)
        {
            // IntentarMover ya maneja la lógica de gasto de PM y el chequeo de adyacencia (aunque ya lo hicimos).
            if (movementComponent.IntentarMover(targetTile))
            {
                // Movimiento iniciado. Perform debe retornar 'false' hasta que la corrutina termine.
                return false;
            }
            else
            {
                // Falló (ej: casilla ocupada de repente).
                running = false;
                return true;
            }
        }

        // 2. Si ya estamos moviéndonos, esperar a que la corrutina de UnitMovement termine.
        if (movementComponent.isMoving)
        {
            return false;
        }

        // 3. El movimiento ha terminado (isMoving es false).
        running = false;
        return true;
    }

    public override void DoReset()
    {
        base.DoReset();
        // target = null;
        targetTile = null;
    }
}