using UnityEngine;
using System.Collections.Generic;

public class ConstruirPobladoAction : GoapAction
{
    private readonly Dictionary<ResourceType, int> CostoConstruccion = new Dictionary<ResourceType, int>
    {
        { ResourceType.Madera, 1 },
        { ResourceType.Oveja, 1 },
        { ResourceType.Trigo, 1 },
        { ResourceType.Arcilla, 1 }
    };

    private PlayerIA playerAgent;
    private GoapAgent goapAgent;
    private HexTile targetTile; // Referencia al Tile específico de destino

    protected override void Awake()
    {
        base.Awake();

        // CORRECCIÓN: Asegurar inicialización y tipo de referencia
        if (unitAgent == null) unitAgent = GetComponent<Unit>();
        // Usar FindObjectOfType si PlayerIA no es padre, o GetComponentInParent/GameManager si es un singleton.
        // Asumiremos que GetComponentInParent<PlayerIA>() funciona si el agente está bajo un PlayerIA.
        playerAgent = unitAgent.GetComponentInParent<PlayerIA>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Construir_Poblado;
        cost = 5.0f;
        rangeInTiles = 0;
        requiresInRange = true;

        // **NOTA:** Las listas preConditionsConfig y afterEffectsConfig deben ser 
        // llenadas en el Inspector o usar el método FillDictionaries() si estas 
        // reasignaciones de código se hacen en Awake.
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (playerAgent == null || goapAgent == null || BoardManager.Instance == null || unitAgent == null) return false;

        // 1. Chequear Recursos
        if (!playerAgent.CanAfford(CostoConstruccion))
        {
            Debug.LogWarning("ConstruirPobladoAction: No se puede construir por falta de recursos (Procedural Check).");
            return false;
        }

        // 2. Obtener el HexTile y asignar el target (Solo si target es nulo)
        if (target == null)
        {
            CellData cellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);

            if (cellData == null || cellData.visualTile == null)
            {
                // Solo si la casilla lógica no existe o no tiene visual.
                Debug.LogError("ConstruirPobladoAction Falló: Coordenada objetivo no tiene Tile visual válido.");
                return false;
            }

            targetTile = cellData.visualTile;
            target = targetTile.gameObject;
        }

        // 3. Chequear Rango (¿estamos en la casilla?)
        // IsInRange() usa 'target' y compara con la posición del agente.
        if (requiresInRange && !IsInRange())
        {
            // Esto puede ser true si el agente está en el lugar equivocado, 
            // o si el planificador lo seleccionó, pero el MoveAction anterior falló.
            return false;
        }

        // 4. Chequeo de Negocio: Verificar si la casilla ya tiene algo.
        // CellData currentCell = BoardManager.Instance.GetCell(unitAgent.misCoordenadasActuales);
        // if(currentCell != null && currentCell.hasSettlement) return false;

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (playerAgent == null || targetTile == null || !IsInRange())
        {
            DoReset();
            // Si falla a mitad de la ejecución, terminamos la acción.
            return true;
        }

        // 1. Re-chequear recursos antes de gastar (Seguridad)
        if (!playerAgent.CanAfford(CostoConstruccion))
        {
            Debug.LogWarning("ConstruirPobladoAction: Recursos insuficientes durante Perform. Abortando.");
            running = false;
            return true; // Terminamos la acción con fallo.
        }

        // 2. Gasto de recursos
        playerAgent.SpendResources(CostoConstruccion);

        // 3. Lógica de construcción
        // targetTile.SetPoblado(true, playerAgent.playerID);

        Debug.Log($"GOAP: {agent.name} ha construido un poblado en {unitAgent.misCoordenadasActuales}.");

        // 4. La acción terminó con éxito.
        running = false;
        return true;
    }

    public override void DoReset()
    {
        base.DoReset();
        target = null;
        targetTile = null;
    }
}