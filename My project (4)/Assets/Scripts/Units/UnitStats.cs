using UnityEngine;
using System.Collections.Generic;

// Esta línea es la magia:
// Te permite crear "assets" de este tipo desde el menú de Unity
[CreateAssetMenu(fileName = "NuevaUnidad", menuName = "Katan/Estadísticas de Unidad")]
public class UnitStats : ScriptableObject // <- Fíjate que hereda de ScriptableObject
{
    [Header("Identificación")]
    public TypeUnit nombreUnidad;
    public GameObject prefabModelo; // Para el modelo visual

    [Header("Estadísticas de Combate")]
    public int vidaMaxima;
    public int ataque;
    public int rangoAtaque;

    [Header("Movimiento")]
    public int puntosMovimiento;

    [Header("Costes de Producción")]
    public int costeMadera;
    public int costePiedra;
    public int costeTrigo;
    public int costeArcilla;
    public int costeOveja;

    public Dictionary<ResourceType, int> GetProductCost()
    {
        Dictionary <ResourceType, int> costs = new Dictionary<ResourceType, int>();

        if(costeMadera>0) costs.Add(ResourceType.Madera, costeMadera);
        if(costeArcilla>0) costs.Add(ResourceType.Arcilla, costeArcilla);
        if(costeOveja>0) costs.Add(ResourceType.Oveja, costeOveja);
        if(costePiedra>0) costs.Add(ResourceType.Roca, costePiedra);
        if(costeTrigo>0) costs.Add(ResourceType.Trigo, costeTrigo);

        return costs;
    }
}