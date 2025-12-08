using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Acción de patrullaje para unidades militares cuando no hay amenazas inmediatas.
/// Mueve la unidad a posiciones estratégicas (borde del territorio, puntos clave).
/// </summary>
public class PatrolAction : GoapAction
{
    private GoapAgent goapAgent;
    private AIAnalysisManager analysisManager;
    private Vector2Int patrolTarget;

    protected override void Awake()
    {
        // IMPORTANTE: Llamar a base.Awake() PRIMERO para inicializar las listas del Inspector
        base.Awake();
        
        goapAgent = GetComponent<GoapAgent>();

        if (GameManager.Instance != null && GameManager.Instance.aiAnalysis != null)
        {
            analysisManager = GameManager.Instance.aiAnalysis;
        }

        // Configuración básica
        actionType = ActionType.Patrol;  // ← FIX: Usar el enum correcto
        cost = 10.0f;
        rangeInTiles = 0;
        requiresInRange = false;  // ← FIX: No requiere estar en rango, es MOVIMIENTO

        // Añadir efectos DESPUÉS de que base.Awake() haya llenado los diccionarios
        if (!Effects.ContainsKey("Patrullando"))
            Effects.Add("Patrullando", 1);

        // ❌ ELIMINADO: No debe tener precondición "EstaEnRango" porque es una acción de MOVIMIENTO
        // Esta precondición creaba un deadlock: necesitas moverte para estar en rango,
        // pero necesitas estar en rango para poder moverte
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (goapAgent == null || unitAgent == null || analysisManager == null)
        {
            return false;
        }

        // Solo patrullar si no hay amenazas inmediatas (vida > 40% y poca amenaza local)
        float healthPercent = unitAgent.vidaActual / (float)unitAgent.statsBase.vidaMaxima;
        if (healthPercent < 0.4f)
        {
            return false; // Mejor usar HuirAction
        }

        // Buscar un punto de patrulla adecuado
        Player myPlayer = GameManager.Instance.GetPlayer(unitAgent.ownerID);
        if (myPlayer == null) return false;

        Vector2Int? patrolPos = analysisManager.GetPatrolPosition(myPlayer.playerID, unitAgent.misCoordenadasActuales);
        
        if (!patrolPos.HasValue)
        {
            return false; // No hay posiciones de patrulla disponibles
        }
        
        patrolTarget = patrolPos.Value;

        // Si ya estamos en un buen punto de patrulla, no necesitamos movernos
        // (el objetivo se considera cumplido)
        if (patrolTarget == unitAgent.misCoordenadasActuales)
        {
            return false; // Ya estamos patrullando en posición
        }

        // Validar que el punto es accesible
        CellData targetCell = BoardManager.Instance.GetCell(patrolTarget);
        if (targetCell == null || targetCell.unitOnCell != null)
        {
            return false;
        }

        // Asignar el target físico para que IsInRange funcione
        if (targetCell.visualTile != null)
        {
            target = targetCell.visualTile.gameObject;
        }

        // Actualizar la blackboard del agente con el destino
        goapAgent.targetDestination = patrolTarget;

        Debug.Log($"🛡️ PatrolAction: {unitAgent.name} asignado a patrullar en {patrolTarget}");
        return true;
    }

    public override bool Perform(GameObject agent)
    {
        running = true;
        Debug.Log($"🛡️ GOAP: {agent.name} ha llegado a posición de patrulla en {unitAgent.misCoordenadasActuales}.");

        // La acción real de moverse la hace MoverAction
        // Esta acción solo representa "estar patrullando"
        running = false;
        return true;
    }
}
