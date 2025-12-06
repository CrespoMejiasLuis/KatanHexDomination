using UnityEngine;
using System.Collections.Generic;

public class ConstruirPobladoAction : GoapAction
{
    // Definimos el coste estándar (esto debe coincidir con lo que comprueba UnitBuilder)
    private readonly Dictionary<ResourceType, int> CostoConstruccion = new Dictionary<ResourceType, int>
    {
        { ResourceType.Madera, 1 },
        { ResourceType.Oveja, 1 },
        { ResourceType.Trigo, 1 },
        { ResourceType.Arcilla, 1 }
    };

    private Player playerAgent;
    private GoapAgent goapAgent;

    protected override void Awake()
    {
        base.Awake();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Construir_Poblado;
        cost = 5.0f; // Coste bajo para que sea prioritario si es posible
        rangeInTiles = 0; // Debe estar EN la misma casilla
        requiresInRange = true;

        // --- Configuración de Lógica GOAP ---
        // 1. Necesito estar en el sitio
        if (!Preconditions.ContainsKey("EstaEnRango"))
            Preconditions.Add("EstaEnRango", 1);

        // 2. Necesito tener dinero
        if (!Preconditions.ContainsKey("TieneRecursosParaPoblado"))
            Preconditions.Add("TieneRecursosParaPoblado", 1);

        // 3. El resultado es que existe un poblado
        if (!Effects.ContainsKey("PobladoConstruido"))
            Effects.Add("PobladoConstruido", 1);
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // 1. Encontrar al Jugador (Dueño)
        if (playerAgent == null)
        {
            // Buscamos la referencia en el GameManager usando el ID de la unidad
            // Asumiendo: ownerID 1 es IA, 0 es Humano
            int ownerID = unitAgent.ownerID;
            if (GameManager.Instance != null)
            {
                playerAgent = (ownerID == 1) ? GameManager.Instance.IAPlayer : GameManager.Instance.humanPlayer;
            }
        }

        if (playerAgent == null || goapAgent == null || BoardManager.Instance == null) return false;

        // 2. Chequear Recursos Económicos
        if (!playerAgent.CanAfford(CostoConstruccion))
        {
            return false;
        }

        // 3. Validar Destino y Asignar Target Físico
        // El GoapAgent ya debería tener el 'targetDestination' asignado por PlayerIA
        CellData cellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);

        if (cellData == null || cellData.visualTile == null)
        {
            return false; // Casilla inválida
        }

        // Seguridad: No construir encima de otro edificio
        if (cellData.typeUnitOnCell == TypeUnit.Poblado || cellData.typeUnitOnCell == TypeUnit.Ciudad)
        {
            return false;
        }

        // Asignamos el objeto físico para que GoapAction.IsInRange() funcione
        target = cellData.visualTile.gameObject;

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        // Validaciones de seguridad
        if (playerAgent == null || target == null) return true;

        running = true;

        // Delegamos la lógica de construcción al UnitBuilder que ya tiene el colono
        UnitBuilder builder = unitAgent.GetComponent<UnitBuilder>();

        if (builder != null)
        {
            Debug.Log($"GOAP: {agent.name} ejecutando construcción en {unitAgent.misCoordenadasActuales}");

            // Esta función se encarga de:
            // 1. Gastar recursos
            // 2. Instanciar el poblado
            // 3. Actualizar el BoardManager
            // 4. Matar al colono (Destroy)
            builder.IntentarConstruirPoblado();

            // Como el colono muere/desaparece al construir, la acción termina aquí.
            running = false;
            return true;
        }
        else
        {
            Debug.LogError("ConstruirPobladoAction: No se encontró el componente UnitBuilder.");
            running = false;
            return false;
        }
    }
}