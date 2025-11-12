using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    // 1. EL ENLACE A LOS DATOS
    // Arrastra aquí tu asset "Stats_Colono.asset"
    public UnitStats statsBase;

    [Header("Propiedad")]
    // 0 = Jugador Humano, 1 = IA
    public int ownerID = 0;

    // 2. ESTADO ACTUAL DE LA UNIDAD
    // Estas son las variables que cambian durante el juego
    [Header("Estado Actual")]
    public int vidaActual;
    public int movimientosRestantes;
    public Vector2Int misCoordenadasActuales;
    // public Player propietario; // (Necesitarás una referencia a su dueño)

    // 3. REFERENCIAS A OTROS COMPONENTES
    // (Los añadiremos después, pero los preparamos)
    private UnitMovement moveComponent;
    //private UnitAttack attackComponent;
    //private UnitBuilder builderComponent; // <-- Especial para el colono

    // AWAKE se usa para configurar referencias internas
    void Awake()
    {
        // Busca sus otros "brazos" y "piernas"
        if(GetComponent<UnitMovement>() != null)
        {
            moveComponent = GetComponent<UnitMovement>();
        }
        //attackComponent = GetComponent<UnitAttack>();
        //builderComponent = GetComponent<UnitBuilder>();
    }

    // START se usa para inicializar el estado
    void Start()
    {
        // Al nacer, coge sus stats del ScriptableObject
        if (statsBase != null)
        {
            vidaActual = statsBase.vidaMaxima;
            movimientosRestantes = statsBase.puntosMovimiento;
        }
        else
        {
            Debug.LogError("¡La unidad " + gameObject.name + " no tiene statsBase asignado!");
        }

        GameManager.OnPlayerTurnStart += OnTurnStart;
    }

    // (Aquí irán métodos para gestionar el turno, recibir daño, etc.)
    
    /// <summary>
    /// Resetea los puntos de movimiento al inicio del turno.
    /// Esta función será llamada por el GameManager cuando sea tu turno.
    /// </summary>
    public void OnTurnStart()
    {
        movimientosRestantes = statsBase.puntosMovimiento;
        Debug.Log($"Turno de {statsBase.nombreUnidad}. Movimientos reseteados a {movimientosRestantes}");
    }
    
    /// <summary>
    /// Esta función es llamada por UnitMovement cuando un movimiento se completa.
    /// </summary>
    public void GastarPuntoDeMovimiento()
    {
        if (movimientosRestantes > 0)
        {
            movimientosRestantes--;
        }
    }

    public void RecibirDaño(int cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        // Lógica de muerte (animación, notificar al juego, etc.)
        Debug.Log(statsBase.nombreUnidad + " ha muerto.");
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        GameManager.OnPlayerTurnStart -= OnTurnStart;
    }
}