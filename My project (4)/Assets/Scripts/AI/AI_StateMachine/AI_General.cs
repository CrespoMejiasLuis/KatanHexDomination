using UnityEngine;

public class AI_General : MonoBehaviour
{   
    [Header("Referencias")]
    public AIAnalysisManager aiAnalysis;
    public PlayerIA myPlayer;
    [Header("Configuración de Umbrales")]
    [Tooltip("Amenaza necesaria para entrar en guerra (> 50)")]
    public float warThreshold = 50f;
    
    [Tooltip("Amenaza baja necesaria para volver a paz (< 40)")]
    public float peaceThreshold = 40f;
    public float opportunismFactor = 1.5f;
    private AIState currentStrategicState;


    public TacticalAction CurrentOrder { get; set; } 

    void Start()
    {
        ChangeState(new EconomyState(this));
    }

    public void DecideStrategy()
    {
        if (aiAnalysis == null)
        {
            Debug.LogError("AI_General: Faltan referencias.");
            return;
        }

        // 1. Calcular amenaza global (Dato que necesitan los estados)
        float totalThreat = CalculateGlobalThreat();

        // 2. Ejecutar la lógica del estado actual
        if (currentStrategicState != null)
        {
            currentStrategicState.Execute(totalThreat);
        }

        // Debug visual para ver qué está pasando
        Debug.Log($"🧠 GENERAL: Estado [{currentStrategicState.GetType().Name}] -> Orden [{CurrentOrder}] (Amenaza: {totalThreat})");
    }

    // Método público para permitir que los Estados se cambien a sí mismos
    public void ChangeState(AIState newState)
    {
        // Salir del anterior
        if (currentStrategicState != null)
        {
            currentStrategicState.OnExit();
        }

        // Cambiar referencia
        currentStrategicState = newState;

        // Entrar en el nuevo
        currentStrategicState.OnEnter();
    }

    // Función auxiliar para sumar la amenaza del mapa
    public float CalculateGlobalThreat()
    {
        float threat = 0f;
        if (aiAnalysis != null && aiAnalysis.threatMap != null)
        {
            foreach (float val in aiAnalysis.threatMap) threat += val;
        }
        return threat;
    }
    public float CalculateMyMilitaryPower()
    {
        if (myPlayer == null || myPlayer.ArmyManager == null) return 0f;

        float totalPower = 0f;
        var myUnits = myPlayer.ArmyManager.GetAllUnits();

        foreach (var unit in myUnits)
        {
            if (unit.statsBase != null)
            {
                totalPower += unit.statsBase.ataque * 1.0f;
                totalPower += unit.statsBase.vidaMaxima * 0.1f;
            }
        }
        return totalPower;
    }
    public bool IsEconomyCritical()
    {
        if (myPlayer == null) return true;

        var res = myPlayer.GetResources(); 
        if (res.ContainsKey(ResourceType.Trigo) && res[ResourceType.Trigo] < 2) return true;
        
        return false;
    }
}