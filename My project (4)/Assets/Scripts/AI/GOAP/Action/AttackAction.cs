using UnityEngine;
using System.Linq;

public class AttackAction : GoapAction
{
    private UnitAttack unitAttackComponent;
    private AIAnalysisManager analysisManager; // Referencia al analizador

    protected override void Awake()
    {
        base.Awake();

        unitAttackComponent = GetComponent<UnitAttack>();
        if (GameManager.Instance != null && GameManager.Instance.aiAnalysis != null)
        {
            analysisManager = GameManager.Instance.aiAnalysis;
        }

        actionType = ActionType.Atacar;
        cost = 5.0f; // Coste bajo para priorizar el combate
        rangeInTiles = 1;
        requiresInRange = true;
    }

    //---------------------------------------------------------
    // 1. CHEQUEO: ¿Puede ejecutar la acción?
    //---------------------------------------------------------
    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // 1. Validar componentes y Puntos de Movimiento disponibles
        // CAMBIO: Usamos movimientosRestantes
        if (unitAttackComponent == null || unitAgent == null || unitAgent.movimientosRestantes <= 0)
        {
            return false;
        }

        Unit targetUnit = null;

        // 2. Búsqueda de Objetivo (Si no hay target asignado)
        if (target == null)
        {
            targetUnit = FindBestTarget();

            if (targetUnit != null)
            {
                target = targetUnit.gameObject;
            }
            else
            {
                return false; // No hay objetivos válidos para atacar
            }
        }
        else
        {
            targetUnit = target.GetComponent<Unit>();
        }

        // 3. Chequeo de Objetivo y Rango
        if (targetUnit == null || targetUnit.vidaActual <= 0)
        {
            target = null;
            return false;
        }

        // Usamos la lógica precisa de tu UnitAttack para validar el rango.
        if (!unitAttackComponent.PuedeAtacar(targetUnit))
        {
            return false;
        }

        // Actualizamos el rango de la acción GOAP para que Pathfinding sepa el límite.
        rangeInTiles = unitAgent.statsBase.rangoAtaque;

        return true;
    }

    //---------------------------------------------------------
    // 2. EJECUCIÓN: Realizar el ataque
    //---------------------------------------------------------
    public override bool Perform(GameObject agent)
    {
        running = true;

        Unit targetUnit = target.GetComponent<Unit>();

        // Validación final, incluyendo los movimientosRestantes
        if (unitAttackComponent == null || targetUnit == null || unitAgent.movimientosRestantes <= 0)
        {
            DoReset();
            return true;
        }

        Debug.Log($"⚔️ GOAP: {agent.name} atacando a {target.name}.");

        // 1. Ejecutar el ataque real
        unitAttackComponent.Atacar(targetUnit);

        // 2. Consumir Puntos de Movimiento
        // CAMBIO CRUCIAL: Consumimos un punto de movimiento para representar la acción.
        unitAgent.movimientosRestantes--;

        // 3. Finalizar la acción (es instantánea)
        running = false;
        return true;
    }

    //---------------------------------------------------------
    // 3. FUNCIÓN AUXILIAR DE BÚSQUEDA (Sin cambios)
    //---------------------------------------------------------
    private Unit FindBestTarget()
    {
        var enemyUnits = FindObjectsOfType<Unit>()
            .Where(u => u.ownerID != unitAgent.ownerID)
            .ToList();

        Unit bestTarget = null;
        float highestScore = -1f;

        foreach (var unit in enemyUnits)
        {
            // Heurística Simple: Priorizar al que está en rango y/o tiene menor vida.
            if (unitAttackComponent.PuedeAtacar(unit))
            {
                // Si está en rango, le damos un gran bono y priorizamos baja vida
                float score = 1000f - unit.vidaActual;

                if (score > highestScore)
                {
                    highestScore = score;
                    bestTarget = unit;
                }
            }
        }

        return bestTarget;
    }
}