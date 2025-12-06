using UnityEngine;

public class Mejorar_A_Ciudad : GoapAction
{
    private GoapAgent goapAgent;
    private SettlementUnit settlementUnit;
    private SimpleClickTester simpleClickTester;

    protected override void Awake()
    {
        base.Awake();

        goapAgent = GetComponent<GoapAgent>();
        settlementUnit = GetComponent<SettlementUnit>();
        simpleClickTester = FindFirstObjectByType<SimpleClickTester>();

        actionType = ActionType.Mejorar_A_Ciudad;
        cost = 10.0f; // Costo razonable
        rangeInTiles = 0; // Debe ser el propio poblado
        requiresInRange = false;

        // Configuración GOAP: necesita recursos y efecto final
        if (!Preconditions.ContainsKey("TieneRecursosParaCiudad"))
            Preconditions.Add("TieneRecursosParaCiudad", 1);

        if (!Effects.ContainsKey("Mejorar_A_Ciudad"))
            Effects.Add("Mejorar_A_Ciudad", 1);

        if(simpleClickTester == null)
        {
            Debug.LogWarning("Action Mejorar_A_Ciudad no encuentra el SimpleClickTester");
        }
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        //1.Verificar que es un poblado
        if(unitAgent.statsBase.nombreUnidad != TypeUnit.Poblado)
        {
            return false;
        }
        
        //2.Verificar que el poblado pertenece al agente (evitar mejorar poblados del jugador humano)
        Unit agentUnit = agent.GetComponent<Unit>();
        if(agentUnit != null && unitAgent.ownerID != agentUnit.ownerID)
        {
            return false;
        }
        
        //3.Verificar recursos
        if(simpleClickTester != null && simpleClickTester.ciudadPrefab != null)
        {
            Unit ciudadUnitPrefab = simpleClickTester.ciudadPrefab.GetComponent<Unit>();
            if(ciudadUnitPrefab != null)
            {
                bool hasResources = unitAgent.RecursosNecesarios(ciudadUnitPrefab);
                if (!hasResources)
                {
                    Debug.Log($"GOAP: Mejorar_A_Ciudad - No hay recursos suficientes");
                }
                return hasResources;
            }
        }

        return false;
    }

    public override bool Perform(GameObject agent)
    {
        if(simpleClickTester == null)
        {
            Debug.LogError("GOAP: Mejorar_A_Ciudad - SimpleClickTester es null");
            running = false;
            return true; // Terminar con error
        }

        running = true;
        
        Debug.Log($"GOAP: Mejorando {unitAgent.statsBase.nombreUnidad} a Ciudad en {unitAgent.misCoordenadasActuales}");
        
        // UpgradeCiudad es síncrono, se completa inmediatamente
        simpleClickTester.UpgradeCiudad(unitAgent);
        
        running = false;
        return true; // Acción completada
    }
}
