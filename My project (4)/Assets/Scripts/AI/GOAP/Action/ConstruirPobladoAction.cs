using UnityEngine;
using System.Collections.Generic;

public class ConstruirPobladoAction : GoapAction
{
    // ... (Costo y referencias PlayerIA, GoapAgent) ...
    private readonly Dictionary<ResourceType, int> CostoConstruccion = new Dictionary<ResourceType, int>
    {
        { ResourceType.Madera, 1 },
        { ResourceType.Oveja, 1 },
        { ResourceType.Trigo, 1 },
        { ResourceType.Arcilla, 1 }
    };

    private PlayerIA playerAgent;
    private GoapAgent goapAgent;
    private HexTile targetTile;

    protected override void Awake()
    {
        base.Awake();

        playerAgent = unitAgent.GetComponentInParent<PlayerIA>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Construir_Poblado;
        cost = 5.0f;
        rangeInTiles = 0; // Debe estar en la casilla
        requiresInRange = true;

        // ... (Precondiciones y Efectos Estáticos) ...
        preConditionsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "EstaEnRango", value = 1 },
            new WorldStateConfig { key = "TienePoblado", value = 0 },
            new WorldStateConfig { key = "TieneCiudad", value = 0 },
            // AÑADIR: Ahora, el planificador sabe que necesita este estado
            new WorldStateConfig { key = "TieneRecursosParaPoblado", value = 1 }
        };

        afterEffectsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "TienePoblado", value = 1 }
        };
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (playerAgent == null || goapAgent == null || BoardManager.Instance == null) return false;

        // 1. Chequear Recursos
        if (!playerAgent.CanAfford(CostoConstruccion)) return false;

        // 2. Obtener el HexTile y asignar el target
        if (target == null)
        {
            // USAR BoardManager para convertir la coordenada de destino (targetDestination) a HexTile.
            CellData cellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);

            if (cellData == null || cellData.visualTile == null)
            {
                Debug.LogError("ConstruirPobladoAction Falló: La coordenada objetivo no tiene un Tile visual válido.");
                return false;
            }

            targetTile = cellData.visualTile;
            target = targetTile.gameObject;
        }

        // 3. Chequear Rango (¿estamos en la casilla?)
        if (requiresInRange && !IsInRange())
        {
            return false;
        }

        // 4. Chequeo de Negocio (Ej: la casilla no debe tener ya una unidad, etc.)

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        // ... (Lógica de Perform, gasto de recursos, y construcción) ...
        if (playerAgent == null || target == null || !IsInRange())
        {
            DoReset();
            return true;
        }

        running = true;

        bool success = playerAgent.SpendResources(CostoConstruccion);

        if (!success)
        {
            running = false;
            return false;
        }

        // Lógica de construcción
        // targetTile.SetPoblado(true, playerAgent.playerID);

        Debug.Log($"GOAP: {agent.name} ha construido un poblado.");

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