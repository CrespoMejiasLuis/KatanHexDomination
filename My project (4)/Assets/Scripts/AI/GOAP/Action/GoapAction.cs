using UnityEngine;
using System.Collections.Generic;

public abstract class GoapAction : MonoBehaviour
{
    [Header("Configuracion General")]
    // 1. Usamos Enum en lugar de String
    public ActionType actionType; 
    
    // 2. Costo para que la IA decida que plan es "mas barato" o preferible
    public float cost = 1.0f; 

    [Header("Objetivo")]
    // El objeto fisico con el que interactuamos (la casilla a construir, el enemigo a atacar)
    [HideInInspector] public GameObject target; 
    
    [Header("Rango de Interacción")]
    // En tu juego de casillas: 0 = Misma casilla, 1 = Vecino, etc.
    public int rangeInTiles = 1; 
    public bool requiresInRange = true;

    // --- TRUCO PARA EL INSPECTOR ---
    // Usamos esto solo para poder editar en Unity
    [System.Serializable]
    public struct WorldStateConfig
    {
        public string key;   // Ej: "TienePoblado"
        public int value;    // 1 = True
    }

    [Header("Configuracion del Estado (Editar Aqui)")]
    public List<WorldStateConfig> preConditionsConfig; // Lo que necesito antes
    public List<WorldStateConfig> afterEffectsConfig;  // Lo que consigo despues

    // Estos se llenan Awake
    public Dictionary<string, int> Preconditions { get; private set; }
    public Dictionary<string, int> Effects { get; private set; }

    [HideInInspector] public bool running = false; 
    protected Unit unitAgent; // Tu componente Unit (cerebro)

    protected virtual void Awake()
    {
        unitAgent = GetComponent<Unit>();
        FillDictionaries(); // 4.se llenan los diccionarios
    }

    // Pasa los datos de la Lista (Inspector) al Diccionario 
    private void FillDictionaries()
    {
        Preconditions = new Dictionary<string, int>();
        Effects = new Dictionary<string, int>();

        foreach (var item in preConditionsConfig)
        {
            if (!Preconditions.ContainsKey(item.key))
                Preconditions.Add(item.key, item.value);
        }

        foreach (var item in afterEffectsConfig)
        {
            if (!Effects.ContainsKey(item.key))
                Effects.Add(item.key, item.value);
        }
    }

    public virtual void DoReset()
    {
        running = false;
        target = null;
    }

    // devolver true cuanado acaba accion
    public abstract bool Perform(GameObject agent);

    //Para chequear
    public virtual bool CheckProceduralPrecondition(GameObject agent)
    {
        return true;
    }

    // Chequeo de rango basado en casillas (usando tu sistema de coordenadas)
    public bool IsInRange()
    {
        if (target == null) return false;
        
        // Asumimos que target tiene un componente para obtener coordenadas
        // Si es una casilla:
        var hexTile = target.GetComponent<HexTile>();
        if (hexTile != null)
        {
            // Usar logica de distancia axial aqui
            // int dist = HexUtils.Distance(unitAgent.misCoordenadasActuales, hexTile.AxialCoordinates);
            // return dist <= rangeInTiles;
            
            // Simplificado por ahora (Distancia fisica):
            return Vector3.Distance(transform.position, target.transform.position) < (rangeInTiles * 1.5f); 
        }
        
        return true;
    }
}
