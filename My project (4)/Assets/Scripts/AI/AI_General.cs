using UnityEngine;
using System.Linq; // Necesario para sumar arrays si usamos Linq, o bucles normales


public class AI_General : MonoBehaviour
{

    [Header("Referencias")]
    public AIAnalysisManager aiAnalysis; // Arrastra aquí el script de tus compañeros

    [Header("Configuración de Umbrales")]
    [Tooltip("Si la amenaza total supera esto, entramos en GUERRA. ")]
    public float warThreshold = 50f; 
    
    [Tooltip("Factor de histéresis para volver a PAZ (evita cambios rápidos).")]
    public float peaceThreshold = 40f;

    [Header("Estado Actual (Read Only)")]
    public StrategicState currentStrategicState = StrategicState.Economy;
    public TacticalState currentTacticalState = TacticalState.EarlyExpansion;

    // Variables internas para tomar decisiones
    private float totalThreatLevel = 0f;
    

    public void DecideStrategy()
    {
        if (aiAnalysis == null)
        {
            Debug.LogError("AI_General: No tengo referencia al AIAnalysisManager.");
            return;
        }

        // Leemos los mapas que generan tus compañeros
        CalculateGlobalThreat(); 

        // ¿Estamos en Paz o en Guerra?
        UpdateStrategicState();

        // Dentro de mi estrategia actual, ¿qué debo priorizar?
        UpdateTacticalState();
        
        Debug.Log($"🧠 GENERAL: Estado decidido -> [{currentStrategicState}] > [{currentTacticalState}] (Amenaza: {totalThreatLevel})");
    }

    // --- LÓGICA DE NIVEL 1 (PADRE) ---
    private void UpdateStrategicState()
    {
        switch (currentStrategicState)
        {
            case StrategicState.Economy:
                // Si estamos en paz, vigilamos si la amenaza sube demasiado
                if (totalThreatLevel > warThreshold)
                {
                    currentStrategicState = StrategicState.War;
                    Debug.Log(totalThreatLevel);
                    Debug.Log("⚔️ ¡ALERTA! La amenaza es alta. Cambiando a ESTADO DE GUERRA.");
                }
                break;

            case StrategicState.War:
                // Si estamos en guerra, solo volvemos a paz si la amenaza baja mucho
                if (totalThreatLevel < peaceThreshold)
                {
                    currentStrategicState = StrategicState.Economy;
                    Debug.Log("🕊️ La amenaza ha disminuido. Volviendo a ESTADO DE ECONOMÍA.");
                }
                break;
        }
    }

    // --- LÓGICA DE NIVEL 2 (HIJO) ---
    private void UpdateTacticalState()
    {
        // Aquí es donde ocurre la "Jerarquía": Un switch dentro de la decisión anterior.
        switch (currentStrategicState)
        {
            case StrategicState.Economy:
                
                // Preguntamos al mapa si hay buenos sitios para expandirse
                Vector2Int? bestExpansionSpot = aiAnalysis.GetBestPositionForExpansion();
                
                if (bestExpansionSpot.HasValue)
                {
                    currentTacticalState = TacticalState.EarlyExpansion;
                }
                else
                {
                    currentTacticalState = TacticalState.Development;
                }
                break;

            case StrategicState.War:
                
                // Aquí necesitaríamos saber nuestra fuerza militar vs la del enemigo.
                // Por ahora, usaremos una lógica simple basada en amenaza local.
                
                // TODO: Conectar con ArmyManager para saber mi fuerza real.
                bool soyMasFuerte = false; // Placeholder

                if (soyMasFuerte)
                {
                    currentTacticalState = TacticalState.Assault;
                }
                else
                {
                    currentTacticalState = TacticalState.ActiveDefense;
                }
                break;
        }
    }

    private void CalculateGlobalThreat()
    {
        // Sumamos todos los valores del Mapa de Amenaza de tus compañeros
        totalThreatLevel = 0f;
        
        if (aiAnalysis.threatMap != null)
        {
            foreach (float val in aiAnalysis.threatMap)
            {
                Debug.Log(val);
                totalThreatLevel += val;
            }
        }
    }
}