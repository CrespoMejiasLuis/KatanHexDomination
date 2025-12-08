using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PlayerArmyManager))]
public class PlayerIA : Player
{
    [Header("Cerebros")]
    private AIAnalysisManager aiAnalysis;
    public AI_General generalBrain;
    private PlayerArmyManager myArmyManager;


    public Dictionary<ResourceType, int> GetResources()
    {
        return resources;
    }
    protected override void Awake()
    {
        base.Awake();
        aiAnalysis = FindFirstObjectByType<AIAnalysisManager>();
        myArmyManager = GetComponent<PlayerArmyManager>();
        generalBrain.myPlayer = this;
    }

    public override void BeginTurn()
    {
        Debug.Log($"🟢 --- INICIO TURNO IA ({playerName}) ---");
        StartCoroutine(ExecuteAITurn());
    }

    private IEnumerator ExecuteAITurn()
    {
        // 1. Analisis del Tablero
        if (aiAnalysis != null) aiAnalysis.CalculateBaseMaps(this.playerID);
        yield return null; 

        // 2. Decision Estrategica Global (FSM)
        if (generalBrain != null) generalBrain.DecideStrategy();
        
        // 🔧 FIX ALTO #7: Eliminar pausa innecesaria - CurrentOrder debe estar actualizado inmediatamente
        // yield return new WaitForSeconds(3f); // ❌ REMOVIDO

        // 3. Asignacion de Objetivos GOAP a las Unidades
        AssignGoapGoals();
        yield return null;
       
        // 4. FIN DEL TURNO DE IA
        // En un sistema GOAP real, la IA podria esperar a que las unidades terminen,
        // pero en turnos, asignamos los planes y dejamos que se ejecuten.
        Debug.Log("⏳ IA: Esperando a que las unidades terminen...");
        
        // Si una unidad se bugea y nunca termina, pasamos turno a los 10 segundos para no colgar el juego.
        float timeOut = 10f;
        while (!AreAllUnitsIdle() && timeOut > 0)
        {
           // ReassignGoalsToIdleUnits();
            timeOut -= Time.deltaTime;
            yield return null;
        }

        // 🔧 FIX ALTO #7: Reducir pausa visual final de 3s a 1s
        yield return new WaitForSeconds(1f); // Pausa visual breve

        // 5. FIN
        Debug.Log("🔴 IA: Fin de turno. Pasando al jugador.");
        GameManager.Instance.EndAITurn(); 
    }

    // Itera sobre todas las unidades y les asigna un objetivo GOAP basado en la estrategia actual.
    private void AssignGoapGoals()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();
        
        Debug.Log($"🎯 AssignGoapGoals: Iniciando asignación para {allUnits.Count} unidades");

        foreach (Unit unit in allUnits)
        {
            if (unit == null)
            {
                Debug.LogWarning("⚠️ Unidad null detectada en la lista, saltando...");
                continue;
            }
            
            Debug.Log($"🔍 Procesando unidad: {unit.name} (Tipo: {unit.statsBase.nombreUnidad})");

            // Obtener el Agente GOAP de la unidad
            GoapAgent agent = unit.GetComponent<GoapAgent>();
            if (agent == null)
            {
                Debug.LogWarning($"❌ La unidad {unit.name} de la IA no tiene GoapAgent.");
                continue;
            }

            // Calcular el objetivo para esta unidad
            Dictionary<string, int> goal = CalculateGoapGoal(unit);
            
            Debug.Log($"📋 Objetivo calculado para {unit.name}: {goal.Count} entradas");

            // Asignar el objetivo al agente (esto dispara el Planner)
            if (goal != null && goal.Count > 0)
            {
                Debug.Log($"✅ Asignando objetivo a {unit.name}");
                agent.SetGoal(goal);
            }
            else
            {
                Debug.LogWarning($"⚠️ No se pudo calcular objetivo para {unit.name}");
            }
        }
        
        Debug.Log($"🏁 AssignGoapGoals: Asignación finalizada");
    }

    //calcula objetivo de cada unidad
    private Dictionary<string, int> CalculateGoapGoal(Unit unit)
    {
        Dictionary<string, int> goal = new Dictionary<string, int>();
        
        Debug.Log($"🧮 CalculateGoapGoal para {unit.name}: nombreUnidad={unit.statsBase.nombreUnidad} (int: {(int)unit.statsBase.nombreUnidad})");

        // --- A. CIUDADES / RECLUTADORES ---
        if (unit.statsBase.nombreUnidad == TypeUnit.Poblado || unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
        {
            Debug.Log($"✅ {unit.name} es Poblado/Ciudad");
            switch(generalBrain.CurrentOrder){
                case TacticalAction.EarlyExpansion:
                    goal.Add("ColonoProducido", 1);
                    break;
                
                case TacticalAction.Development:
                    if (unit.statsBase.nombreUnidad == TypeUnit.Poblado)
                    {
                        goal.Add("Mejorar_A_Ciudad", 1); // <-- Necesitarás una Action_MejorarCiudad con este efecto
                    }
                    // Si ya es ciudad, quizás reclutar trabajadores o soldados
                    else 
                    {
                        goal.Add("TropaProducida", 1);
                    }
                    break;

                case TacticalAction.BuildArmy:
                    // Durante militarización, producir unidades militares
                    if (unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
                    {
                        goal.Add("CaballeroProducido", 1);  // Ciudades → Caballeros
                    }
                    else // Poblados
                    {
                        goal.Add("ArqueroProducido", 1);  // Poblados → Arqueros (más baratos)
                    }
                    break;

                case TacticalAction.Assault:
                    goal.Add("ArqueroProducido", 1);
                    break;

                case TacticalAction.ActiveDefense:
                    goal.Add("CaballeroProducido", 1);
                    break;

                
            }
            
            
            return goal;
        }

        // --- B. COLONOS (Constructores) ---
        // 🔧 FIX CRÍTICO: Unity guarda valores int de enums en prefabs
        // Si cambias el enum, los valores viejos persisten hasta regenerar prefabs
        int colonoEnumValue = (int)TypeUnit.Colono;
        int unidadValue = (int)unit.statsBase.nombreUnidad;
        
        Debug.Log($"🔍 Comprobando si {unit.name} es Colono: {unidadValue} == {colonoEnumValue} (TypeUnit.Colono) ? {unidadValue == colonoEnumValue}");
        Debug.Log($"📊 Valores de enum - None:{(int)TypeUnit.None}, Camino:{(int)TypeUnit.Camino}, Poblado:{(int)TypeUnit.Poblado}, Ciudad:{(int)TypeUnit.Ciudad}, Artillero:{(int)TypeUnit.Artillero}, Caballero:{(int)TypeUnit.Caballero}, Colono:{(int)TypeUnit.Colono}");
        
        // WORKAROUND: Hasta regenerar prefabs, comparar por valor int directo
        // Si unidadValue == 8 y colonoEnumValue == 6, es un Colono con valor obsoleto
        bool esColono = (unidadValue == colonoEnumValue) || (unidadValue == 8); // 8 = valor obsoleto de Colono
        
        if (esColono)
        {
            Debug.Log($"✅ {unit.name} detectado como COLONO (workaround aplicado), orden actual: {generalBrain.CurrentOrder}");
            
            // En expansión, desarrollo y militarización queremos construir si es posible
            if (generalBrain.CurrentOrder == TacticalAction.EarlyExpansion || 
                generalBrain.CurrentOrder == TacticalAction.Development ||
                generalBrain.CurrentOrder == TacticalAction.BuildArmy)
            {
                // 1. Encontrar el mejor lugar
                Vector2Int? bestSpot = aiAnalysis.GetBestPositionForExpansion(unit, this);
                GoapAgent agent = unit.GetComponent<GoapAgent>();

                if (bestSpot.HasValue && agent != null)
                {
                    agent.targetDestination = bestSpot.Value;
                    Debug.Log($"🏗️ Colono en {unit.misCoordenadasActuales} asignado a construir en {bestSpot.Value}");
                    goal.Add("PobladoConstruido", 1);
                }
                else
                {
                    // 🎯 MEJORA: Si no hay sitio, mantenerse seguro
                    Debug.Log($"🛡️ Colono sin sitios disponibles. Mantenerse seguro.");
                    goal.Add("Seguro", 1);
                }
            }
            // 🎯 MEJORA: En guerra, colonos deben huir/refugiarse
            else if (generalBrain.CurrentOrder == TacticalAction.Assault ||
                     generalBrain.CurrentOrder == TacticalAction.ActiveDefense)
            {
                Debug.Log($"⚠️ Colono en zona de guerra. Huyendo a seguridad.");
                goal.Add("Seguro", 1); // Activará HuirAction si vida < 40%
            }
            
            return goal;
        }
        
        Debug.Log($"✅ {unit.name} NO es Colono. Checkando tropas (Artillero={TypeUnit.Artillero}, Caballero={TypeUnit.Caballero}, unit={unit.statsBase.nombreUnidad})...");
        Debug.Log($"⚠️ {unit.name} NO es Colono, Poblado ni Ciudad. Comprobando tropas...");


        // --- C. TROPAS DE COMBATE ---
        if(unit.statsBase.nombreUnidad == TypeUnit.Artillero || unit.statsBase.nombreUnidad == TypeUnit.Caballero)
        {
            GoapAgent combatAgent = unit.GetComponent<GoapAgent>();
            
            switch(generalBrain.CurrentOrder)
            {
                // 🎯 MEJORA: Tropas en expansión temprana patrullan
                case TacticalAction.EarlyExpansion:
                    Debug.Log($"🛡️ {unit.name} en EarlyExpansion: Patrullando territorio");
                    goal.Add("Patrullando", 1);  // ← CAMBIO: Usar objetivo específico de patrullaje
                    break;

                // 🎯 MEJORA: Tropas en desarrollo patrullan
                case TacticalAction.Development:
                    Debug.Log($"🚫 {unit.name} en Development: Patrullando territorio");
                    goal.Add("Patrullando", 1);  // ← CAMBIO: Usar objetivo específico de patrullaje
                    break;

                case TacticalAction.BuildArmy:
                    // Durante militarización, posicionarse/patrullar
                    Debug.Log($"🛡️ {unit.name} en BuildArmy: Preparación militar");
                    goal.Add("Patrullando", 1);  // ← CAMBIO: Usar objetivo específico de patrullaje
                    break;

                case TacticalAction.ActiveDefense:
                case TacticalAction.Assault:
                    // 🎯 MEJORA: Buscar celda adyacente libre al enemigo más cercano
                    if (combatAgent != null)
                    {
                        Unit nearestEnemy = FindNearestEnemy(unit);
                        if (nearestEnemy != null)
                        {
                            // 🔧 FIX: Buscar celda adyacente LIBRE al enemigo (no la celda del enemigo)
                            Vector2Int? targetPos = FindAdjacentFreeCell(nearestEnemy.misCoordenadasActuales, unit);
                            
                            if (targetPos.HasValue)
                            {
                                combatAgent.targetDestination = targetPos.Value;
                                Debug.Log($"⚔️ {unit.name} en combate: Moverse a {targetPos.Value} (adyacente a enemigo en {nearestEnemy.misCoordenadasActuales})");
                            }
                            else
                            {
                                // Si no hay celdas libres, patrullar territorio
                                Debug.Log($"⚠️ {unit.name}: No hay celdas libres cerca del enemigo, patrullando");
                                goal.Add("Patrullando", 1);
                                return goal;
                            }
                        }
                        else
                        {
                            Debug.Log($"⚔️ {unit.name} en combate: Sin enemigos detectados, patrullando");
                            goal.Add("Patrullando", 1);
                            return goal;
                        }
                    }
                    
                    // 🔧 FIX CRÍTICO: El objetivo de combate es ESTAR EN RANGO, no "estar seguro"
                    // Esto permitirá la cadena: MoveToCombatPositionAction → AttackAction
                    goal.Add("EnRangoDeAtaque", 1);
                    break;
            }
            
            return goal;
        }

        return goal; // Return goal si no coincide con ningún tipo
    }

    //compruba si alguan unidad de mi ejercito hace cosas
    private bool AreAllUnitsIdle()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            GoapAgent agent = unit.GetComponent<GoapAgent>();
            
            // Si tiene agente y dice que esta actuando, NO hemos terminado.
            if (agent != null && agent.IsActing)
            {
                return false; 
            }
            
            // Si la unidad se esta moviendo visualmente 
            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null && movement.isMoving) 
            {
                return false;
            } 
        }

        // Si nadie esta actuando
        return true; 
    }
    
    // 🎯 HELPER: Buscar enemigo más cercano
    private Unit FindNearestEnemy(Unit fromUnit)
    {
        if (fromUnit == null) return null;
        
        var allUnits = FindObjectsOfType<Unit>();
        Unit nearestEnemy = null;
        float minDistance = float.MaxValue;
        
        foreach (var unit in allUnits)
        {
            // Solo enemigos
            if (unit.ownerID == fromUnit.ownerID) continue;
            
            // Calcular distancia manhattan
            int dx = Mathf.Abs(unit.misCoordenadasActuales.x - fromUnit.misCoordenadasActuales.x);
            int dy = Mathf.Abs(unit.misCoordenadasActuales.y - fromUnit.misCoordenadasActuales.y);
            float distance = dx + dy;
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = unit;
            }
        }
        
        return nearestEnemy;
    }

    // 🎯 HELPER: Buscar celda adyacente libre a una posición objetivo
    private Vector2Int? FindAdjacentFreeCell(Vector2Int targetPos, Unit forUnit)
    {
        if (BoardManager.Instance == null) return null;

        // Obtener celdas adyacentes
        List<CellData> adjacentCells = BoardManager.Instance.GetAdjacents(targetPos);
        
        // Filtrar las que no están ocupadas ni son del jugador humano
        List<Vector2Int> freeCells = new List<Vector2Int>();
        
        foreach (var cellData in adjacentCells)
        {
            if (cellData == null) continue;
            
            // Validar que esté libre
            bool isFree = (cellData.unitOnCell == null);
            
            // Validar que no sea un poblado/ciudad enemigo
            bool isNotEnemySettlement = (cellData.owner == -1 || cellData.owner == forUnit.ownerID);
            
            if (isFree && isNotEnemySettlement)
            {
                freeCells.Add(cellData.coordinates);
            }
        }
        
        // Si hay celdas libres, elegir la más cercana a mi unidad
        if (freeCells.Count > 0)
        {
            Vector2Int bestCell = freeCells[0];
            int minDist = BoardManager.Instance.Distance(forUnit.misCoordenadasActuales, freeCells[0]);
            
            for (int i = 1; i < freeCells.Count; i++)
            {
                int dist = BoardManager.Instance.Distance(forUnit.misCoordenadasActuales, freeCells[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCell = freeCells[i];
                }
            }
            
            return bestCell;
        }
        
        return null;
    }
}