// 📁 UnitMovement.cs
using UnityEngine;

// Asegura que este script SIEMPRE esté junto a un script "Unit"
[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    // Referencia al "cerebro" (Unit.cs)
    private Unit unitCerebro;

    void Awake()
    {
        // Obtiene la referencia al script "Unit" en este mismo GameObject
        unitCerebro = GetComponent<Unit>();
    }

    /// <summary>
    /// Esta es la función pública que otros scripts llamarán.
    /// Intenta mover la unidad a la casilla de destino.
    /// </summary>
    public bool IntentarMover(HexTile casillaDestino)
    {
        // 1. Comprobar si tenemos permiso (Puntos de Movimiento)
        if (unitCerebro.movimientosRestantes <= 0)
        {
            Debug.Log("¡No quedan puntos de movimiento!");
            return false;
        }

        // 2. (FUTURO) Comprobar si la casilla es adyacente
        // ... (por ahora nos movemos a cualquier parte) ...

        // 3. (FUTURO) Comprobar si la casilla está ocupada
        // ... (lo que hablaréis mañana con BoardManager) ...

        // 4. Si todo OK, ¡moverse!
        // Obtenemos la posición central de la casilla visual
        Vector3 destinoPos = casillaDestino.transform.position;

        // Movemos nuestro transform a esa posición (instantáneo)
        // (Más adelante puedes hacer esto una animación con Lerp o Slerp)
        transform.position = destinoPos;

        // 5. Gastar el recurso (Puntos de Movimiento)
        // Le decimos al cerebro que hemos gastado un punto
        unitCerebro.GastarPuntoDeMovimiento();

        Debug.Log($"Movido a {casillaDestino.name}. Movimientos restantes: {unitCerebro.movimientosRestantes}");
        return true;
    }
}