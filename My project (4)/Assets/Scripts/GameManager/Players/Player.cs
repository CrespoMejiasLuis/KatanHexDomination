using UnityEngine;
using System.Collections.Generic; // Necesario para Diccionarios
using System; // Necesario para Enum.GetValues

public abstract class Player : MonoBehaviour
{
    public static event Action<Dictionary<ResourceType, int>> OnPlayerResourcesUpdated;
    public static event Action<int> OnPlayerVictoryPointsUpdated; // Para los Puntos de Victoria
    [Header("Identificación del Jugador")]
    public int playerID;
    public string playerName;
    public int victoryPoints;
    public PlayerArmyManager ArmyManager { get; private set; }



    protected Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();


    protected virtual void Awake()
    {
        victoryPoints = 0;
        InitializeResourceDictionary();
        ArmyManager = GetComponent<PlayerArmyManager>();
    }


    /// <summary>
    /// Rellena el diccionario con todos los tipos de recursos, empezando en 0.
    /// Esto evita errores de 'KeyNotFoundException' más adelante.
    /// </summary>
    private void InitializeResourceDictionary()
    {
        // Obtiene todos los valores del enum HexTile.ResourceType
        foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
        {
            // No queremos añadir 'Desierto' como un recurso acumulable
            if (resourceType != ResourceType.Desierto)
            {
                if (!resources.ContainsKey(resourceType))
                {
                    resources.Add(resourceType, 0); // Empezar con 0 de cada
                }
            }
        }

        // Pone los recursos de poblado para construir el primero
        resources[ResourceType.Madera] = 1;
        resources[ResourceType.Arcilla] = 1;
        resources[ResourceType.Oveja] = 1;
        resources[ResourceType.Trigo] = 3;
        resources[ResourceType.Roca] = 3;

        OnPlayerResourcesUpdated?.Invoke(resources);
    }

    // --- MÉTODOS PÚBLICOS DE GESTIÓN DE RECURSOS ---

    /// <summary>
    /// Añade una cantidad de un recurso al inventario del jugador.
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (type == ResourceType.Desierto) return;

        resources[type] += amount;
        Debug.Log($"Jugador {playerID} ganó {amount} de {type}. Total: {resources[type]}");
        // Aquí llamarías a la UI para actualizarse
        // UIManager.Instance.UpdateResourceUI(playerID, type, resources[type]);
        OnPlayerResourcesUpdated?.Invoke(resources);
    }

    /// <summary>
    /// Comprueba si el jugador tiene suficientes recursos para gastar.
    /// </summary>
    public bool HasEnoughResources(ResourceType type, int amountNeeded)
    {
        return resources.ContainsKey(type) && resources[type] >= amountNeeded;
    }

    /// <summary>
    /// Gasta (resta) recursos del inventario.
    /// </summary>
    /// <returns>True si tuvo éxito, False si no tenía suficientes recursos.</returns>
    public bool SpendResources(ResourceType type, int amountToSpend)
    {
        if (HasEnoughResources(type, amountToSpend))
        {
            resources[type] -= amountToSpend;
            Debug.Log($"Jugador {playerID} gastó {amountToSpend} de {type}. Restante: {resources[type]}");
            // Actualizar UI
            return true;
        }
        return false;
    }
    public bool CanAfford(Dictionary<ResourceType, int> costs)
    {
        if (costs == null) return true;

        foreach (var cost in costs)
        {
            ResourceType requiredType = cost.Key;
            int requiredAmount = cost.Value;

            if (!resources.ContainsKey(requiredType) || resources[requiredType] < requiredAmount)
            {
                return false;
            }
        }
        return true;
    }

    // NOTA: Tu método HasEnoughResources(ResourceType type, int amountNeeded) sigue siendo útil para cheques individuales.

    /// <summary>
    
    public bool SpendResources(Dictionary<ResourceType, int> costs)
    {
        // 1. Verificación final (seguridad)
        if (!CanAfford(costs))
        {
            Debug.LogWarning($"Jugador {playerID} no puede pagar el costo. Recursos insuficientes.");
            return false;
        }

        // 2. Ejecutar gasto
        foreach (var cost in costs)
        {
            resources[cost.Key] -= cost.Value;
            Debug.Log($"Jugador {playerID} gastó {cost.Value} de {cost.Key}.");
        }

        // 3. 📢 Notificar a la UI después del gasto
        OnPlayerResourcesUpdated?.Invoke(resources);

        return true;
    }

    // --- MÉTODOS DE PUNTOS DE VICTORIA (PV) ---

    
   

    // --- LÓGICA DE TURNO ABSTRACTA ---

    /// <summary>
    /// Esta es la función que el GameManager llamará.
    /// Cada tipo de jugador (Humano, IA) debe implementar esto de forma diferente.
    /// </summary>
    public abstract void BeginTurn();
}