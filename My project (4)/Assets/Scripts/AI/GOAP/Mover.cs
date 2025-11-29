using UnityEngine;
using System.Collections.Generic;

public class MoverAction : GoapAction
{
    private UnitMovement movementComponent;
    private GoapAgent goapAgent;
    // Necesitas un HexTile para tu método IntentarMover
    private HexTile targetTile;

    protected override void Awake()
    {
        base.Awake();

        movementComponent = unitAgent.GetComponent<UnitMovement>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Mover;
        cost = 1.0f;
        rangeInTiles = 1; // Un paso, ya que IntentarMover solo va a adyacentes.
        requiresInRange = false;

        preConditionsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "HaLlegado", value = 0 }
        };

        afterEffectsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "HaLlegado", value = 1 }
        };
    }

    // Calcula el target, chequea si podemos mover un PASO (adyacente)
    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (goapAgent == null || goapAgent.targetDestination == null)
        {
            return false;
        }

        // --- PASO CLAVE: CALCULAR EL PRÓXIMO PASO ---
        // 1. Obtener la coordenada actual y la coordenada de destino final
        Vector2Int currentCoord = unitAgent.misCoordenadasActuales;
        Vector2Int finalCoord = goapAgent.targetDestination;

        // 2. Aquí necesitas implementar (o usar) un algoritmo de pathfinding
        // (A*, BFS) para encontrar el *siguiente* HexTile adyacente en la ruta hacia el destino final.
        // Esto es un placeholder para la lógica de pathfinding:
        // Vector2Int nextCoord = PathFinder.GetNextStepTowards(currentCoord, finalCoord);

        // 3. Convertir la coordenada del siguiente paso en el HexTile físico
        // targetTile = BoardManager.Instance.GetTileFromCoordinates(nextCoord);

        if (targetTile == null)
        {
            // El Pathfinding falló o no hay ruta.
            Debug.LogError("MoverAction Falló: Target GameObject es NULL para la coordenada: " + goapAgent.targetDestination);
            return false;
        }

        // Asignar el HexTile físico a la variable 'target' heredada de GoapAction
        target = targetTile.gameObject;

        // 4. Chequeo de PM: Ya que IntentarMover lo chequea, solo necesitamos el target válido.
        // PERO para que el planificador pueda encadenar la acción, si la unidad no tiene PM,
        // esta acción debe FALLAR el chequeo.

        // Si el coste es mayor a 0 y no tenemos PM:
        if (unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (targetTile == null || movementComponent == null)
        {
            DoReset();
            return true;
        }

        running = true;

        // 1. Llamar a la función de movimiento que ya maneja la lógica de PM y adyacencia
        // IntentarMover ya consume PM, inicia la corrutina y pone isMoving = true.
        if (movementComponent.IntentarMover(targetTile))
        {
            // El movimiento ha sido iniciado correctamente (isMoving = true)
            // Perform debe retornar false hasta que isMoving vuelva a ser false
            return false;
        }
        else
        {
            // IntentarMover falló (ej: no tenía PM, estaba ocupada, etc.)
            Debug.LogWarning("GOAP: IntentarMover falló durante la ejecución del plan.");
            running = false;
            return true; // Falla la acción, el agente aborta el plan.
        }

        // NOTA: Esta acción (Perform) seguirá regresando 'false' hasta que
        // movementComponent.isMoving se ponga a 'false' al final de la corrutina.
        // El GoapAgent.Update() será el encargado de re-evaluar y ver que
        // movementComponent.isMoving ahora es false.
    }

    public override void DoReset()
    {
        base.DoReset();
        // Detener cualquier movimiento pendiente
        if (movementComponent != null)
        {
            // Necesitarías una función StopMovement en UnitMovement para esto:
            // movementComponent.StopMovement(); 
        }
        target = null;
        targetTile = null;
    }
}