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
    private HexTile targetTile; // Referencia al tile de construcción

    protected override void Awake()
    {
        base.Awake();

        playerAgent = unitAgent.GetComponentInParent<PlayerIA>();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Construir_Poblado;
        cost = 5.0f;
        rangeInTiles = 0; // Debe estar en la casilla
        requiresInRange = true;

        preConditionsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "EstaEnRango", value = 1 },
            new WorldStateConfig { key = "TienePoblado", value = 0 },
            new WorldStateConfig { key = "TieneCiudad", value = 0 }
        };

        afterEffectsConfig = new List<WorldStateConfig>
        {
            new WorldStateConfig { key = "TienePoblado", value = 1 }
        };
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (playerAgent == null || goapAgent == null) return false;

        // 1. Chequear Recursos
        if (!playerAgent.CanAfford(CostoConstruccion)) return false;

        // 2. Obtener el HexTile y asignar el target
        if (target == null)
        {
            // Convertir la coordenada de destino (targetDestination) a HexTile
            // targetTile = BoardManager.Instance.GetTileFromCoordinates(goapAgent.targetDestination);

            if (targetTile == null) return false;
            target = targetTile.gameObject;
        }

        // 3. Chequear Rango (¿estamos en la casilla de construcción?)
        if (requiresInRange && !IsInRange())
        {
            return false;
        }

        // 4. Chequeo de Negocio: La casilla debe ser válida para construir
        // if (targetTile.tipoTerreno == TerrenoType.Agua) return false;

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (playerAgent == null || target == null || !IsInRange())
        {
            DoReset();
            return true;
        }

        running = true;

        // 1. GASTAR RECURSOS
        bool success = playerAgent.SpendResources(CostoConstruccion);

        if (!success)
        {
            running = false;
            return false;
        }

        // 2. CONSTRUIR
        // (Lógica para transformar el targetTile en un Poblado)

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