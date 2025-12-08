using UnityEngine;

public class AI_General : MonoBehaviour
{   
    [Header("Referencias")]
    public AIAnalysisManager aiAnalysis;
    public PlayerIA myPlayer;
    [Header("Configuración de Umbrales")]
    
    // 🔧 FIX ALTO #6: Umbrales de ENTRADA (triggering)
    [Tooltip("Amenaza necesaria para entrar en guerra (ciudad enemiga + tropas)")]
    public float warThreshold = 100f;  // 🔧 Reducido para amenaza LOCAL (~1 ciudad + algo)
    
    [Tooltip("Amenaza moderada para empezar militarización (2-3 unidades enemigas)")]
    public float militarizationThreshold = 20f;  // 🔧 Reducido de 300 a 250
    
    // 🔧 FIX ALTO #6: Umbrales de SALIDA (con histéresis)
    [Tooltip("Amenaza baja para salir de guerra y volver a economía")]
    public float exitWarThreshold = 50f;  // Más bajo que militarizationThreshold
    
    [Tooltip("Amenaza muy baja para salir de militarización")]
    public float exitMilitarizationThreshold = 10f;  // Muy bajo para confirmar paz
    
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

        // 1. Calcular amenaza LOCAL (max amenaza cerca de asentamientos)
        // 🎯 MEJORA: En vez de sumar todo el mapa, solo considerar amenaza relevante
        float totalThreat = CalculateLocalThreat();

        // 2. Ejecutar la lógica del estado actual
        if (currentStrategicState != null)
        {
            currentStrategicState.Execute(totalThreat);
        }

        // Debug visual para ver qué está pasando
        float ratio = GetMilitaryToEconomyRatio();
        Debug.Log($"🧠 GENERAL: Estado [{currentStrategicState.GetType().Name}] → Orden [{CurrentOrder}] (Amenaza Local: {totalThreat:F0}, Ratio: {ratio:F1})");
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

    // 🎯 MEJORA: Amenaza LOCAL - solo cerca de asentamientos
    public float CalculateLocalThreat()
    {
        if (myPlayer == null || myPlayer.ArmyManager == null || aiAnalysis == null) return 0f;
        
        float maxThreat = 0f;
        var myUnits = myPlayer.ArmyManager.GetAllUnits();
        
        foreach (var unit in myUnits)
        {
            // Solo considerar asentamientos
            if (unit.statsBase.nombreUnidad == TypeUnit.Poblado || 
                unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
            {
                // 🔧 FIX: Radio reducido de 3 a 2 (9 casillas vs 25)
                float localThreat = GetThreatNear(unit.misCoordenadasActuales, radius: 2);
                if (localThreat > maxThreat)
                {
                    maxThreat = localThreat;
                }
            }
        }
        
        return maxThreat;
    }
    
    // Helper: Obtiene la amenaza MÁXIMA en radio alrededor de una posición
    // 🔧 FIX: Cambiado de SUMA a MAX para evitar valores inflados
    private float GetThreatNear(Vector2Int position, int radius)
    {
        if (aiAnalysis == null || aiAnalysis.threatMap == null) return 0f;
        
        float maxThreat = 0f;  // ← CAMBIO: max en vez de total
        int gridRadius = BoardManager.Instance != null ? BoardManager.Instance.gridRadius : 10;
        
        // Convertir a coordenadas de mapa
        int centerX = position.x + (gridRadius - 1);
        int centerY = position.y + (gridRadius - 1);
        
        // Buscar amenaza MÁXIMA en el radio
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                
                if (x >= 0 && y >= 0 && 
                    x < aiAnalysis.threatMap.GetLength(0) && 
                    y < aiAnalysis.threatMap.GetLength(1))
                {
                    float cellThreat = aiAnalysis.threatMap[x, y];
                    if (cellThreat > maxThreat)
                    {
                        maxThreat = cellThreat;
                    }
                }
            }
        }
        
        return maxThreat;
    }
    
    // Función auxiliar LEGACY (ya no se usa, pero se mantiene por compatibilidad)
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
    
    // 🎯 MEJORA: Ratio Ejército/Economía para decisiones inteligentes
    public float GetMilitaryToEconomyRatio()
    {
        if (myPlayer == null || myPlayer.ArmyManager == null) return 0f;
        
        int militaryUnits = CountMilitaryUnits();
        int settlements = CountSettlements();
        
        if (settlements == 0) return 0f;
        return (float)militaryUnits / settlements;
    }
    
    // Contar unidades militares (Artillero, Caballero)
    private int CountMilitaryUnits()
    {
        if (myPlayer == null || myPlayer.ArmyManager == null) return 0;
        
        int count = 0;
        var myUnits = myPlayer.ArmyManager.GetAllUnits();
        
        foreach (var unit in myUnits)
        {
            if (unit.statsBase.nombreUnidad == TypeUnit.Artillero ||
                unit.statsBase.nombreUnidad == TypeUnit.Caballero)
            {
                count++;
            }
        }
        
        return count;
    }
    
    // Contar asentamientos (Poblado, Ciudad)
    private int CountSettlements()
    {
        if (myPlayer == null || myPlayer.ArmyManager == null) return 0;
        
        int count = 0;
        var myUnits = myPlayer.ArmyManager.GetAllUnits();
        
        foreach (var unit in myUnits)
        {
            if (unit.statsBase.nombreUnidad == TypeUnit.Poblado ||
                unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
            {
                count++;
            }
        }
        
        return count;
    }
}