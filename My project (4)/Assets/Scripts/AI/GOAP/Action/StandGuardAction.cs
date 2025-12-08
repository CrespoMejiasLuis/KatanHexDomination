using UnityEngine;

/// <summary>
/// Acción de guardia estática para unidades militares.
/// Se ejecuta cuando no hay posiciones de patrulla disponibles pero se pide patrullar.
/// La unidad se queda en su posición actual vigilando.
/// </summary>
public class StandGuardAction : GoapAction
{
    protected override void Awake()
    {
        // IMPORTANTE: Llamar a base.Awake() PRIMERO
        base.Awake();

        // Configuración básica
        actionType = ActionType.StandGuard;  // ← Enum específico
        cost = 15.0f; // Coste mayor que PatrolAction (preferir patrullar si es posible)
        rangeInTiles = 0;
        requiresInRange = false; // ¡No requiere movimiento! Ya estamos donde debemos estar

        // Añadir efectos DESPUÉS de que base.Awake() haya llenado los diccionarios
        if (!Effects.ContainsKey("Patrullando"))
            Effects.Add("Patrullando", 1);
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (unitAgent == null)
        {
            return false;
        }

        // Solo vigilar si tenemos salud razonable
        float healthPercent = unitAgent.vidaActual / (float)unitAgent.statsBase.vidaMaxima;
        if (healthPercent < 0.4f)
        {
            return false; // Mejor huir
        }

        // Siempre es válido quedarse vigilando en la posición actual
        // Esta acción sirve como fallback cuando PatrolAction no encuentra posiciones
        return true;
    }

    public override bool Perform(GameObject agent)
    {
        running = true;
        Debug.Log($"🛡️ GOAP: {agent.name} vigila desde posición actual {unitAgent.misCoordenadasActuales}.");

        // No hacemos nada físicamente, solo completamos el objetivo
        running = false;
        return true;
    }
}
