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
        cost = 50.0f;
        rangeInTiles = 0; // Un paso adyacente
        requiresInRange = false; 

        if(simpleClickTester == null)
        {
            Debug.Log("Action Mejorar_A_Ciudad no encuentra el SimpleClickTester");
        }
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        //1.Verificar que es un poblado
        if(unitAgent.statsBase.nombreUnidad != TypeUnit.Poblado) return false;
        
        //2.Verificar recursos
        if(simpleClickTester != null && simpleClickTester.ciudadPrefab != null)
        {
            Unit ciudadUnitPrefab = simpleClickTester.ciudadPrefab.GetComponent<Unit>();
            if(ciudadUnitPrefab != null)
            {
                return unitAgent.RecursosNecesarios(ciudadUnitPrefab);
            }
        }

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if(simpleClickTester == null) return true;

        simpleClickTester.UpgradeCiudad(unitAgent);
        return true;
    }
}
