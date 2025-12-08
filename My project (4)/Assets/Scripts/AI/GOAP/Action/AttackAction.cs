using UnityEngine;
using System.Linq;

public class AttackAction : GoapAction
{
    private UnitAttack unitAttackComponent;
    private AIAnalysisManager analysisManager; // Referencia al analizador
    private GoapAgent goapAgent;

    protected override void Awake()
    {
        base.Awake();

        unitAttackComponent = GetComponent<UnitAttack>();
        goapAgent = GetComponent<GoapAgent>();

        if (GameManager.Instance != null && GameManager.Instance.aiAnalysis != null)
        {
            analysisManager = GameManager.Instance.aiAnalysis;
        }

        actionType = ActionType.Atacar;
        cost = 5.0f; // Coste bajo para priorizar el combate
        rangeInTiles = 1;
        requiresInRange = true;
        
        // Efecto para satisfacer objetivos de seguridad (ActiveDefense/Assault)
        if (!Effects.ContainsKey("Seguro"))
            Effects.Add("Seguro", 1);

        // 🎯 FIX: Cumplir el objetivo de combate principal
        if (!Effects.ContainsKey("ObjetivoDerrotado"))
            Effects.Add("ObjetivoDerrotado", 1);
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

        // 🔴 MODIFICACION GUERRA: Permitir planear ataque aunque estemos lejos
        // Si es nuestro objetivo de guerra, IGNORAMOS el check de rango procedimental
        // (El Movimiento se encargará de acercarnos)
        if (goapAgent != null && goapAgent.warTarget == targetUnit)
        {
             // Estamos autorizados a proceder aunque 'PuedeAtacar' de false por distancia
             // PERO debemos actualizar rangeInTiles para que el planner sepa a dónde ir
             rangeInTiles = unitAgent.statsBase.rangoAtaque;
             return true; 
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

        // 🔴 CHECK FINAL DE RANGO ANTES DE DISPARAR
        // Aunque el plan nos haya dejado pasar, aquí SI debemos estar cerca
        if (!unitAttackComponent.PuedeAtacar(targetUnit))
        {
            // Si llegamos aqui y no podemos atacar, es que el movimiento falló o algo pasó.
            // Terminamos la acción sin hacer nada (o podríamos fallar el plan)
            Debug.Log($"[ATTACK] {agent.name} intentó atacar a {target.name} pero estaba fuera de rango.");
            running = false;
            return true;
        }

        Debug.Log($"⚔️ GOAP: {agent.name} atacando a {target.name}.");

        // 1. Ejecutar el ataque real
        unitAttackComponent.Atacar(targetUnit);

        // 2. Consumir Puntos de Movimiento
        // CAMBIO CRUCIAL: Consumimos un punto de movimiento para representar la acción.
        unitAgent.movimientosRestantes=0;

        // 3. Finalizar la acción (es instantánea)
        running = false;
        return true;
    }

    //---------------------------------------------------------
    // 3. FUNCIÓN AUXILIAR DE BÚSQUEDA (Sin cambios)
    //---------------------------------------------------------
    private Unit FindBestTarget()
    {
        // 1. Prioridad Absoluta: War Target
        if (goapAgent != null && goapAgent.warTarget != null && goapAgent.warTarget.vidaActual > 0)
        {
             return goapAgent.warTarget;
        }

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