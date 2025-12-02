using UnityEngine;
using System.Collections.Generic;

public class CrearUnidad_Action : GoapAction
{
    [Header("Configuración de Producción")]
    public TypeUnit unitTypeToProduce;

    public GameObject unitPrefab; 

    private UnitRecruiter recruiter;
    private Player ownerPlayer;

    protected override void Awake()
    {
        base.Awake();
        
        recruiter = GetComponent<UnitRecruiter>();
        actionType = ActionType.Crear_Unidad; 
        
        // Coste de planificación: Le ponemos un coste algo alto para que la IA
        // no espamee unidades si tiene otras prioridades más baratas.
        cost = 20.0f; 
        rangeInTiles = 0; 
        requiresInRange = true;
    }

    private void Start()
    {
        // Obtener la referencia al jugador dueño de esta ciudad
        if (GameManager.Instance != null && unitAgent != null)
        {
            ownerPlayer = GameManager.Instance.GetPlayer(unitAgent.ownerID);
        }
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // 1. Validar componentes
        if (recruiter == null || unitPrefab == null || ownerPlayer == null) 
        {
            return false;
        }

        // 2. Validar Costos
        // Obtenemos el componente Unit del prefab para leer sus Stats
        Unit unitScript = unitPrefab.GetComponent<Unit>();
        if (unitScript == null || unitScript.statsBase == null) return false;

        // Usamos la función CanAfford del Player
        Dictionary<ResourceType, int> productionCost = unitScript.statsBase.GetProductCost();
        
        if (!ownerPlayer.CanAfford(productionCost))
        {
            return false; // No hay recursos, la acción no es válida ahora
        }

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (recruiter == null) return true; // Fallo silencioso, terminar acción

        running = true;
        Debug.Log($"🏭 GOAP: {agent.name} produciendo {unitTypeToProduce}...");

        // Llamamos a la función específica del Recruiter según el tipo
        // (Asumiendo que UnitRecruiter tiene estos métodos o uno genérico)
        switch (unitTypeToProduce)
        {
            case TypeUnit.Colono:
                recruiter.ConstruirColono(unitAgent);
                break;
            
            case TypeUnit.Artillero:
                recruiter.ConstruirArtillero(unitAgent);
                break;

            case TypeUnit.Caballero:
                recruiter.ConstruirCaballero(unitAgent);
                break;

            // Añadir más casos aquí...
            default:
                Debug.LogError($"Action_CrearUnidad: No hay lógica en UnitRecruiter para {unitTypeToProduce}");
                running = false;
                return false; // Falló
        }

        // Asumimos que la construcción es instantánea en el turno.
        // Si tarda varios turnos, aquí tendrías otra lógica.
        
        running = false;
        return true; // Acción completada
    }
}