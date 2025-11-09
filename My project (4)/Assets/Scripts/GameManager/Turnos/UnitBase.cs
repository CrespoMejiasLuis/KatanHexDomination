

using UnityEngine;

// Define la clase base que usarás para todas tus unidades (Caballero, Colono, Artillero)
public abstract class UnitBase : MonoBehaviour // O simplemente public class UnitBase : MonoBehaviour
{
    [Header("Atributos de Unidad")]
    public string UnitName;
    public int MaxHealth = 100;
    public int CurrentHealth = 100;
    public int MaxMovementPoints = 3;
    public int MovementPointsRemaining = 3;
    public int Range = 3;
    public int Attack = 3;

    // ... más atributos ...

    public virtual bool CanAttack() { return false; }
    public virtual bool CanPlaceSettlement() { return false; }
}