using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(UnitMovement))]
public class Action_Moverse : GoapAction
{
    private UnitMovement movementComponent;
    private GoapAgent goapAgent;

    // Almacena el objetivo de un solo paso (la casilla adyacente)
    private HexTile nextStepTile;

    // Se inicializa en Awake o en el Inspector (Cost: 1.0f)
    protected override void Awake()
    {
        base.Awake();

        movementComponent = GetComponent<UnitMovement>();
        goapAgent = GetComponent<GoapAgent>();

        // Costo bajo para fomentar múltiples pasos en el plan.
        cost = 1.0f;

        // Configuración GOAP para el Planner:
        // After Effects: Key="EstaEnRango", Value=1 (Solo en el último paso)
    }

    public override void DoReset()
    {
        base.DoReset();
        nextStepTile = null;
    }

    // El GOAPAgent llama a Perform cada frame mientras 'running' sea true.
    public override bool Perform(GameObject agent)
    {
        // 1. Si la unidad está visualmente en movimiento (Corutina activa), esperamos.
        if (movementComponent.isMoving)
        {
            return false; // Sigue en curso
        }

        // 2. Si llegamos aquí y nextStepTile no es nulo, significa que el paso anterior
        // se completó en el frame justo antes de este (movementComponent.isMoving ya es false).
        if (nextStepTile != null)
        {
            // Hemos completado un paso simple. Terminamos esta instancia de la acción.
            return true;
        }

        // --- 3. Cálculo e inicio del siguiente paso ---

        // Encuentra la mejor coordenada adyacente para acercarse a la meta final.
        Vector2Int bestStepCoords = FindBestAdjacentStep(goapAgent.targetDestination);

        // Si el mejor paso es la casilla actual, algo salió mal (o el destino está bloqueado).
        if (bestStepCoords == unitAgent.misCoordenadasActuales)
        {
            // Debería ser capturado por CheckProceduralPrecondition, pero es un seguro.
            Debug.LogWarning("GOAP: Bloqueado o destino inalcanzable. Abortando esta acción.");
            return true; // Falla/termina la acción.
        }

        CellData cellData = BoardManager.Instance.GetCell(bestStepCoords);
        if (cellData != null)
        {
            nextStepTile = cellData.visualTile;
        }
        else
        {
            nextStepTile = null; // No se encontró la celda lógica
        }

        if (nextStepTile != null)
        {
            // IntentarMover inicia la corutina y gasta los puntos de movimiento.
            if (movementComponent.IntentarMover(nextStepTile))
            {
                // Movimiento iniciado. Devolvemos false y esperamos a que el movimiento termine.
                return false;
            }
            else
            {
                // Fallo al iniciar (ej: IntentarMover falló por recursos o bloqueo).
                Debug.LogWarning($"❌ Action_Moverse: Falló IntentarMover hacia {bestStepCoords}.");
                return true; // Falla la acción
            }
        }
        else
        {
            Debug.LogError($"❌ Action_Moverse: No se encontró HexTile para el mejor paso: {bestStepCoords}.");
            return true; // Falla
        }
    }

    // Chequeos Adicionales (Procedurales) que deben cumplirse antes de empezar el plan.
    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (movementComponent == null || goapAgent == null || unitAgent == null) return false;

        // 1. Si ya estamos en el destino final, NO necesitamos Moverse.
        if (unitAgent.misCoordenadasActuales == goapAgent.targetDestination)
        {
            return false;
        }

        // 2. Puntos de movimiento restantes
        if (unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        // 3. El destino final debe existir.
        CellData destCellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);
        HexTile destTile = (destCellData != null) ? destCellData.visualTile : null;
        if (destTile == null)
        {
            return false;
        }

        // 4. Debe existir al menos un paso válido para que la acción sea útil.
        Vector2Int nextStep = FindBestAdjacentStep(goapAgent.targetDestination);
        if (nextStep == unitAgent.misCoordenadasActuales)
        {
            // Si el mejor paso es la posición actual, la unidad está atascada/bloqueada.
            return false;
        }

        return true;
    }

    /// <summary>
    /// Utiliza la distancia axial (cubo) para encontrar la casilla adyacente 
    /// que más se acerca al objetivo final (greedy step).
    /// </summary>
    private Vector2Int FindBestAdjacentStep(Vector2Int finalTarget)
    {
        Vector2Int currentCoords = unitAgent.misCoordenadasActuales;
        // Inicializamos la distancia mínima con la distancia actual al objetivo.
        int minDistance = BoardManager.Instance.Distance(currentCoords, finalTarget);
        Vector2Int bestNextCoord = currentCoords;

        // Iteramos sobre todos los vecinos para encontrar el mejor
        List<CellData> adjacents = BoardManager.Instance.GetAdjacents(currentCoords);

        foreach (CellData cell in adjacents)
        {
            // Opcional: Ignoramos si la casilla está ocupada o es intransitable (si aplica).
            // if (cell.unitOnCell != null || cell.isBlocked) continue; 

            // Calculamos la distancia desde este vecino a la meta final
            int distance = BoardManager.Instance.Distance(cell.coordinates, finalTarget);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestNextCoord = cell.coordinates;
            }
        }

        // Si bestNextCoord cambió, hemos encontrado un paso que nos acerca al destino.
        return bestNextCoord;
    }
}