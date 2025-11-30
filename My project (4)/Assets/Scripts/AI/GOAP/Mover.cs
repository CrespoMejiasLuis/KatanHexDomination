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

    protected override void Awake()
    {
        // 1. Aseguramos que la base se llame PRIMERO.
        base.Awake();

        // 2. CORRECCIÓN: Asegurar que unitAgent se obtiene si no se hizo en la base.
        if (unitAgent == null) unitAgent = GetComponent<Unit>();

        // 3. Inicializar referencias
        movementComponent = GetComponent<UnitMovement>();
        goapAgent = GetComponent<GoapAgent>();

        cost = 1.0f;

        // Inicializamos los efectos vacíos. La lógica dinámica los llenará.
        if (Effects.Count > 0) Effects.Clear();
    }

    

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (movementComponent == null || goapAgent == null || unitAgent == null || BoardManager.Instance == null) return false;

        Vector2Int currentCoord = unitAgent.misCoordenadasActuales;
        Vector2Int finalCoord = goapAgent.targetDestination;

        // Calculamos la distancia real
        int currentDistance = BoardManager.Instance.Distance(currentCoord, finalCoord);

        // -----------------------------------------------------------
        // 1. LÓGICA CLAVE: ASIGNACIÓN DINÁMICA DEL EFECTO GOAP
        // -----------------------------------------------------------
        // Si estamos a 1 paso (o menos) del destino final, esta acción debe SIMULAR el efecto.
        if (currentDistance <= rangeInTiles)
        {
            // Borramos y añadimos el efecto GOAP directamente al diccionario Effects de esta instancia.
            Effects.Clear();
            if (!Effects.ContainsKey("EstaEnRango"))
            {
                Effects.Add("EstaEnRango", 1);
            }
        }
        else
        {
            // Si estamos lejos (paso intermedio), esta acción NO tiene efecto simulado.
            Effects.Clear();
        }
        // -----------------------------------------------------------

        // 2. Si ya estamos en el destino final, NO necesitamos Moverse.
        if (currentCoord == finalCoord)
        {
            return false;
        }

        // 3. Puntos de movimiento restantes
        if (unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        // 4. El destino final debe existir.
        CellData destCellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);
        HexTile destTile = (destCellData != null) ? destCellData.visualTile : null;
        if (destTile == null)
        {
            return false;
        }

        // 5. Debe existir al menos un paso válido para que la acción sea útil.
        Vector2Int nextStep = FindBestAdjacentStep(goapAgent.targetDestination);
        if (nextStep == unitAgent.misCoordenadasActuales)
        {
            // Si el mejor paso es la posición actual, la unidad está atascada/bloqueada.
            return false;
        }

        return true;
    }

    // El método Perform no tiene cambios críticos.
    public override bool Perform(GameObject agent)
    {
        // 1. Si la unidad está visualmente en movimiento (Corutina activa), esperamos.
        if (movementComponent.isMoving)
        {
            return false; // Sigue en curso
        }

        // 2. Si llegamos aquí y nextStepTile no es nulo, significa que el paso anterior se completó.
        if (nextStepTile != null)
        {
            return true; // Hemos completado un paso simple. Terminamos esta instancia de la acción.
        }

        // --- 3. Cálculo e inicio del siguiente paso ---
        Vector2Int bestStepCoords = FindBestAdjacentStep(goapAgent.targetDestination);

        if (bestStepCoords == unitAgent.misCoordenadasActuales)
        {
            Debug.LogWarning("GOAP: Bloqueado o destino inalcanzable. Abortando esta acción.");
            return true;
        }

        CellData cellData = BoardManager.Instance.GetCell(bestStepCoords);
        if (cellData != null)
        {
            nextStepTile = cellData.visualTile;
        }

        if (nextStepTile != null)
        {
            if (movementComponent.IntentarMover(nextStepTile))
            {
                return false; // Movimiento iniciado.
            }
            else
            {
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

    public override void DoReset()
    {
        base.DoReset();
        nextStepTile = null;
    }

    // Este método debe estar presente en el script.
    private Vector2Int FindBestAdjacentStep(Vector2Int finalTarget)
    {
        Vector2Int currentCoords = unitAgent.misCoordenadasActuales;
        int minDistance = BoardManager.Instance.Distance(currentCoords, finalTarget);
        Vector2Int bestNextCoord = currentCoords;

        List<CellData> adjacents = BoardManager.Instance.GetAdjacents(currentCoords);

        foreach (CellData cell in adjacents)
        {
            int distance = BoardManager.Instance.Distance(cell.coordinates, finalTarget);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestNextCoord = cell.coordinates;
            }
        }
        return bestNextCoord;
    }
}