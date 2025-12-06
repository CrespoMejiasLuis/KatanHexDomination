
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Requiere que este script est� en el MISMO objeto que un script de Player
[RequireComponent(typeof(Player))]
public class PlayerArmyManager : MonoBehaviour
{
    // La lista principal que rastrea todas las unidades de este jugador
    private List<Unit> playerUnits = new List<Unit>();

    // Referencia al due�o de este ej�rcito
    private Player owner;

    void Awake()
    {
        // Obtiene la referencia al script Player que est� en este mismo objeto
        owner = GetComponent<Player>();
    }

    /// <summary>
    /// A�ade una unidad a la "librer�a" y se suscribe a su evento de muerte.
    /// Esto debe ser llamado por cualquier script que CREE una unidad (Builder, Settlement, etc.)
    /// </summary>
    public void RegisterUnit(Unit unit)
    {
        if (unit == null || playerUnits.Contains(unit))
        {
            return;
        }

        playerUnits.Add(unit);

        // �Magia! Nos suscribimos al evento de muerte de la unidad.
        // Cuando la unidad muera, llamar� a nuestra funci�n "HandleUnitDied".
        unit.OnUnitDied += HandleUnitDied;

        Debug.Log($"[{owner.playerName}] registr� a: {unit.statsBase.nombreUnidad}. Total de unidades: {playerUnits.Count}");
    }

    /// <summary>
    /// Elimina una unidad de la lista (generalmente porque ha muerto).
    /// </summary>
    private void HandleUnitDied(Unit deadUnit)
    {
        if (deadUnit == null || !playerUnits.Contains(deadUnit))
        {
            return;
        }

        // 1. Darse de baja del evento (buena pr�ctica)
        deadUnit.OnUnitDied -= HandleUnitDied;

        // 2. Eliminar de la lista
        playerUnits.Remove(deadUnit);

        Debug.Log($"[{owner.playerName}] ha perdido a: {deadUnit.statsBase.nombreUnidad}. Unidades restantes: {playerUnits.Count}");
    }

    // --- FUNCIONES DE ACCESO (La "Librer�a") ---

    /// <summary>
    /// Devuelve una lista de todas las unidades vivas que posee este jugador.
    /// </summary>
    public List<Unit> GetAllUnits()
    {
        // Devolvemos una nueva lista para evitar modificaciones externas
        return new List<Unit>(playerUnits);
    }

    /// <summary>
    /// Obtiene todas las unidades de un tipo espec�fico (ej: todos los "Soldado").
    /// </summary>
    public List<Unit> GetUnitsByType(TypeUnit type)
    {
        return playerUnits.Where(unit => unit.statsBase.nombreUnidad == type).ToList();
    }

    public int GetCountOfType(TypeUnit type)
    {
        // Usamos LINQ para contar cuántas cumplen la condición
        return playerUnits.Count(unit => unit.statsBase.nombreUnidad == type);
    }

    /// <summary>
    /// Encuentra si este jugador tiene una unidad en una coordenada espec�fica.
    /// </summary>
    public Unit GetUnitAt(Vector2Int coordinates)
    {
        return playerUnits.FirstOrDefault(unit => unit.misCoordenadasActuales == coordinates);
    }

    /// <summary>
    /// Devuelve el n�mero total de unidades.
    /// </summary>
    public int GetTotalUnitCount()
    {
        return playerUnits.Count;
    }

    public void DeregisterUnit(Unit unit)
    {
        if (unit != null && playerUnits.Contains(unit))
        {
            unit.OnUnitDied -= HandleUnitDied;
            playerUnits.Remove(unit);
        }
    }
}