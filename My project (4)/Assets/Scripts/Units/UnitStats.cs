using UnityEngine;

// Esta línea es la magia:
// Te permite crear "assets" de este tipo desde el menú de Unity
[CreateAssetMenu(fileName = "NuevaUnidad", menuName = "Katan/Estadísticas de Unidad")]
public class UnitStats : ScriptableObject // <- Fíjate que hereda de ScriptableObject
{
    [Header("Identificación")]
    public string nombreUnidad;
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
}