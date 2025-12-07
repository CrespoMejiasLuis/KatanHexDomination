using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Unit : MonoBehaviour
{
    // 1. EL ENLACE A LOS DATOS
    // Arrastra aquí tu asset "Stats_Colono.asset"
    public UnitStats statsBase;
    public event Action<Unit> OnUnitDied;

    [Header("Propiedad")]
    // 0 = Jugador Humano, 1 = IA
    public int ownerID = 0;
    public bool startWithZeroMovement = false;

    // 2. ESTADO ACTUAL DE LA UNIDAD
    // Estas son las variables que cambian durante el juego
    [Header("Estado Actual")]
    public int vidaActual;
    public int movimientosRestantes;
    public Vector2Int misCoordenadasActuales;
    // public Player propietario; // (Necesitarás una referencia a su dueño)
    [Header("Efectos Visuales")]
    [Tooltip("Duración de la animación de aparición")]
    public float spawnInDuration = 1.5f; // 1 segundo y medio
    private Vector3 originalScale;

    // 3. REFERENCIAS A OTROS COMPONENTES
    // (Los añadiremos después, pero los preparamos)
    private UnitMovement moveComponent;
    //private UnitAttack attackComponent;
    //private UnitBuilder builderComponent; // <-- Especial para el colono

    // AWAKE se usa para configurar referencias internas
    void Awake()
    {
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        // Busca sus otros "brazos" y "piernas"
        if (GetComponent<UnitMovement>() != null)
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
            if(startWithZeroMovement)
            {
               movimientosRestantes = 0;
            }
            else
            {
                movimientosRestantes = statsBase.puntosMovimiento;
            }
        }
        else
        {
            Debug.LogError("¡La unidad " + gameObject.name + " no tiene statsBase asignado!");
        }

        // 2. Suscripción de Turno (Corregido)
        // La unidad comprueba quién es su dueño y se suscribe al evento de turno correcto.
        if (ownerID == 0) // Asumimos 0 = Humano
        {
            GameManager.OnPlayerTurnStart += OnTurnStart;
        }
        else // Asumimos 1 = IA
        {
            GameManager.OnAITurnStart += OnTurnStart;
        }

        // --- ¡¡AQUÍ ESTÁ TU LÓGICA DE ROTACIÓN!! ---
        // 3. Comprobar Dueño y Rotar
        if (ownerID != 0) // Si NO soy del Jugador 0 (Humano)...
        {
            // ...girar 180 grados para mirar al jugador.
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // 4. Iniciar animación de aparición
        StartCoroutine(ScaleInCoroutine());
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
    public void GastarPuntoDeMovimiento(int costeMovimiento)
    {
        if (movimientosRestantes > 0)
        {
            movimientosRestantes--;
        }
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public void RecibirCuracion(int cantidad)
    {
        vidaActual += cantidad;
        if (vidaActual > statsBase.vidaMaxima)
        {
            vidaActual = statsBase.vidaMaxima;
        }
    }

    private void Morir()
    {
        // Lógica de muerte (animación, notificar al juego, etc.)
        OnUnitDied?.Invoke(this);
        Debug.Log(statsBase.nombreUnidad + " ha muerto.");
        
        // --- FIX: Restaurar casilla si estabamos sobre un Poblado ---
        CellData currentCell = BoardManager.Instance.GetCell(misCoordenadasActuales);
        if(currentCell != null && currentCell.unitOnCell == this)
        {
             // Chequear si hay OTRA unidad (ej: poblado) lógicamente ahí
             Unit otherUnit = null;

             // 1. Buscar en el jugador humano
             if(GameManager.Instance != null && GameManager.Instance.humanPlayer != null)
             {
                 foreach(Unit u in GameManager.Instance.humanPlayer.ArmyManager.GetAllUnits())
                 {
                     if(u != null && u != this && u.misCoordenadasActuales == misCoordenadasActuales)
                     {
                         otherUnit = u;
                         break;
                     }
                 }
             }

             // 2. Buscar en la IA (si no hemos encontrado nada aún)
             if(otherUnit == null && GameManager.Instance != null && GameManager.Instance.IAPlayer != null)
             {
                 foreach(Unit u in GameManager.Instance.IAPlayer.ArmyManager.GetAllUnits())
                 {
                     if(u != null && u != this && u.misCoordenadasActuales == misCoordenadasActuales)
                     {
                         otherUnit = u;
                         break;
                     }
                 }
             }
 
             if(otherUnit != null)
             {
                 currentCell.unitOnCell = otherUnit;
                 currentCell.typeUnitOnCell = otherUnit.statsBase.nombreUnidad;
                 Debug.Log($"[Fix] Casilla {misCoordenadasActuales} restaurada a: {otherUnit.name}");
             }
             else
             {
                 currentCell.unitOnCell = null;
                 currentCell.typeUnitOnCell = TypeUnit.None;
             }
        }

        GameManager.OnPlayerTurnStart -= OnTurnStart;
        GameManager.OnAITurnStart -= OnTurnStart;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        GameManager.OnPlayerTurnStart -= OnTurnStart;
        GameManager.OnAITurnStart -= OnTurnStart;
    }
    private IEnumerator ScaleInCoroutine()
    {
        float timer = 0f;
        Vector3 startScale = Vector3.zero; // Empezar en 0
        Vector3 targetScale = originalScale; // Terminar en la escala original

        while (timer < spawnInDuration)
        {
            // 2. Calcular la escala actual (de 0 a originalScale)
            // Usamos Vector3.Lerp para la interpolación suave
            transform.localScale = Vector3.Lerp(startScale, targetScale, timer / spawnInDuration);

            // 3. Esperar al siguiente frame
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. Asegurar que tiene la escala 100% correcta al final
        transform.localScale = targetScale;
    }
    public bool RecursosNecesarios(Unit unitPrefabToRecruit)
    {
        //1.Obtener referenccia al jugador
        Player activePlayer = GameManager.Instance.humanPlayer;

        if (activePlayer == null) return false;

        //2.Obtener estadisticas de unidad
        UnitStats stats = unitPrefabToRecruit.statsBase;

        if (stats == null)
        {
            Debug.Log($"No tiene unitStats el prefab {unitPrefabToRecruit.name}");
            return false;
        }

        Dictionary<ResourceType, int> productionCost = stats.GetProductCost();

        return activePlayer.CanAfford(productionCost);
    }

    public Dictionary<ResourceType, int> actualizarCostes(Dictionary<ResourceType, int> baseCost, Player jugador)
    {
        Dictionary<ResourceType, int> finalCost = new Dictionary<ResourceType, int>();

        foreach(var resourcePair in baseCost)
        {
            ResourceType type = resourcePair.Key;
            int originalAmount = resourcePair.Value;
            
            int adjustedAmount = originalAmount + jugador.numPoblados; 
            
            // Guardar en el nuevo diccionario
            finalCost.Add(type, adjustedAmount);
            
            // Debug opcional para ver el cambio
            // Debug.Log($"Recurso {type}: Base {originalAmount} -> Final {adjustedAmount}");
        }
        return finalCost;
    }
}