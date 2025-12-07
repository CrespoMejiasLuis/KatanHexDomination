using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ConstruirPobladoAction : GoapAction
{
    // Definimos el coste estándar (esto debe coincidir con lo que comprueba UnitBuilder)
    private Dictionary<ResourceType, int> CostoConstruccion = new Dictionary<ResourceType, int>
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
        // === LOG 1: Validación de Referencias ===
        if (playerAgent == null)
        {
            int ownerID = unitAgent.ownerID;
            if (GameManager.Instance != null)
            {
                playerAgent = GameManager.Instance.IAPlayer;
                Debug.Log($"🔍 ConstruirPoblado [{unitAgent?.name}]: Referencia de playerAgent obtenida (OwnerID: {ownerID})");
            }
            else
            {
                Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent?.name}]: GameManager.Instance es null");
                return false;
            }
        }

        if (playerAgent == null)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent?.name}]: No se pudo obtener playerAgent");
            return false;
        }

        if (goapAgent == null)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent?.name}]: GoapAgent es null");
            return false;
        }

        if (BoardManager.Instance == null)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent?.name}]: BoardManager.Instance es null");
            return false;
        }

        Unit unit = GetComponent<Unit>();

        // === LOG 2: Actualización de Costos ===
        if(playerAgent.numPoblados > 1)
        {
            CostoConstruccion = unit.actualizarCostes(CostoConstruccion, playerAgent);
            Debug.Log($"🔍 ConstruirPoblado [{unitAgent.name}]: Costos actualizados (numPoblados: {playerAgent.numPoblados})");
        }

        // === LOG 3: Validación de Recursos ===
        if (!playerAgent.CanAfford(CostoConstruccion))
        {
            string costStr = string.Join(", ", CostoConstruccion.Select(kv => $"{kv.Key}:{kv.Value}"));
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent.name}]: Recursos insuficientes. Necesita: {costStr}");
            return false;
        }

        Debug.Log($"✅ ConstruirPoblado [{unitAgent.name}]: Recursos disponibles");

        // === LOG 4: Validación de Destino ===
        CellData cellData = BoardManager.Instance.GetCell(goapAgent.targetDestination);

        if (cellData == null)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent.name}]: CellData es null en destino {goapAgent.targetDestination}");
            return false;
        }

        if (cellData.visualTile == null)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent.name}]: VisualTile es null en destino {goapAgent.targetDestination}");
            return false;
        }

        // === LOG 5: Validación de Ocupación ===
        if (cellData.typeUnitOnCell == TypeUnit.Poblado || cellData.typeUnitOnCell == TypeUnit.Ciudad)
        {
            Debug.LogWarning($"❌ ConstruirPoblado [{unitAgent.name}]: Casilla {goapAgent.targetDestination} ya tiene edificio ({cellData.typeUnitOnCell})");
            return false;
        }

        // Asignamos el objeto físico para que GoapAction.IsInRange() funcione
        target = cellData.visualTile.gameObject;

        Debug.Log($"✅ ConstruirPoblado [{unitAgent.name}]: Todas las precondiciones cumplidas. Target asignado: {goapAgent.targetDestination}");
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