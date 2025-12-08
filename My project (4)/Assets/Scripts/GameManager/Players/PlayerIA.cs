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


    // 🔒 Celdas reservadas durante este turno (evita que múltiples unidades apunten a la misma celda)
    private HashSet<Vector2Int> reservedCellsThisTurn = new HashSet<Vector2Int>();

    // Itera sobre todas las unidades y les asigna un objetivo GOAP basado en la estrategia actual.
    private void AssignGoapGoals()
    {
        // 🔄 Limpiar reservas del turno anterior
        reservedCellsThisTurn.Clear();
        
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

            // 🔧 FIX: NO recalcular si ya tiene un objetivo de combate válido
            bool shouldRecalculate = ShouldRecalculateGoal(unit, agent);
            
            if (!shouldRecalculate)
            {
                Debug.Log($"⏭️ {unit.name} mantiene su objetivo actual (ya en ruta)");
                continue;
            }

            // Calcular el objetivo para esta unidad
            Dictionary<string, int> goal = CalculateGoapGoal(unit);
            
            // 🔒 Si la unidad recibió un destino de combate, reservar la celda
            GoapAgent agentAfterCalc = unit.GetComponent<GoapAgent>();
            if (agentAfterCalc != null && agentAfterCalc.targetDestination != Vector2Int.zero)
            {
                reservedCellsThisTurn.Add(agentAfterCalc.targetDestination);
                Debug.Log($"🔒 Celda {agentAfterCalc.targetDestination} reservada por {unit.name}");
            }
            
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

    // 🎯 Determina si debemos recalcular el objetivo para esta unidad
    // SIMPLICADO: TODAS las unidades recalculan cada turno para máxima adaptación dinámica
    private bool ShouldRecalculateGoal(Unit unit, GoapAgent agent)
    {
        Debug.Log($"♻️ {unit.name} recalcula objetivo (recalculación universal)");
        return true;
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
                case TacticalAction.Assault:
                case TacticalAction.ActiveDefense:
                    // 🎯 MEJORA: Solo el asentamiento más cercano a la amenaza produce unidades
                    Unit bestSettlement = SelectBestSettlementForMilitaryProduction();
                    
                    if (bestSettlement == null || bestSettlement != unit)
                    {
                        // Este asentamiento NO es el prioritario para producción militar
                        Debug.Log($"⏭️ {unit.name} saltado (no es el asentamiento prioritario para producción militar)");
                        // No asignar objetivo de producción militar
                        return goal; // Retornar goal vacío
                    }
                    
                    // Este ES el asentamiento prioritario, producir según el modo
                    Debug.Log($"🏭 {unit.name} ES el asentamiento prioritario, produciendo unidades militares");
                    
                    if (generalBrain.CurrentOrder == TacticalAction.BuildArmy)
                    {
                        // Durante militarización, producir unidades variadas
                        if (unit.statsBase.nombreUnidad == TypeUnit.Ciudad)
                        {
                            goal.Add("CaballeroProducido", 1);  // Ciudades → Caballeros
                        }
                        else // Poblados
                        {
                            goal.Add("ArqueroProducido", 1);  // Poblados → Arqueros
                        }
                    }
                    else if (generalBrain.CurrentOrder == TacticalAction.Assault)
                    {
                        goal.Add("ArqueroProducido", 1);
                    }
                    else if (generalBrain.CurrentOrder == TacticalAction.ActiveDefense)
                    {
                        goal.Add("CaballeroProducido", 1);
                    }
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
                    // 🎯 PASO 1: Verificar si ya está adyacente a un enemigo
                    Unit adjacentEnemy = FindAdjacentEnemy(unit);
                    
                    if (adjacentEnemy != null)
                    {
                        // ✅ YA ESTÁ EN POSICIÓN DE COMBATE - Listo para atacar
                        Debug.Log($"⚔️ {unit.name} está adyacente a {adjacentEnemy.name} - LISTO PARA ATACAR");
                        // No cambiar targetDestination, mantener posición actual
                        // El objetivo "EnRangoDeAtaque" activará AttackAction
                        goal.Add("ObjetivoDerrotado", 1);
                        return goal;
                    }
                    
                    // 🎯 PASO 2: No está adyacente, buscar enemigo y moverse
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
        if (fromUnit == null || BoardManager.Instance == null) return null;
        
        var allUnits = FindObjectsOfType<Unit>();
        Unit nearestEnemy = null;
        int minDistance = int.MaxValue;
        
        foreach (var unit in allUnits)
        {
            // Solo enemigos
            if (unit.ownerID == fromUnit.ownerID) continue;
            
            // 🔧 FIX: Usar distancia hexagonal correcta (no Manhattan)
            int distance = BoardManager.Instance.Distance(unit.misCoordenadasActuales, fromUnit.misCoordenadasActuales);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = unit;
            }
        }
        
        return nearestEnemy;
    }

    // 🎯 HELPER: Buscar si hay un enemigo adyacente a esta unidad
    private Unit FindAdjacentEnemy(Unit fromUnit)
    {
        if (fromUnit == null || BoardManager.Instance == null) return null;

        // Obtener celdas adyacentes a la unidad
        List<CellData> adjacents = BoardManager.Instance.GetAdjacents(fromUnit.misCoordenadasActuales);

        foreach (var cellData in adjacents)
        {
            if (cellData.unitOnCell != null && cellData.unitOnCell.ownerID != fromUnit.ownerID)
            {
                // Hay un enemigo en esta celda adyacente
                Debug.Log($"🔍 {fromUnit.name} tiene enemigo adyacente: {cellData.unitOnCell.name} en {cellData.coordinates}");
                return cellData.unitOnCell;
            }
        }

        return null; // No hay enemigos adyacentes
    }

    // 🎯 Encuentra una celda adyacente libre alrededor del objetivo enemigo
    // Con recalculación cada turno, solo verificamos celdas FÍSICAMENTE libres
    private Vector2Int? FindAdjacentFreeCell(Vector2Int targetPos, Unit forUnit)
    {
        Debug.Log($"🔍 FindAdjacentFreeCell: Buscando celda libre adyacente a {targetPos} para {forUnit.name}");

        if (BoardManager.Instance == null)
        {
            Debug.LogError("❌ BoardManager.Instance es null");
            return null;
        }

        // Obtener celdas adyacentes
        List<CellData> adjacents = BoardManager.Instance.GetAdjacents(targetPos);
        Debug.Log($"📊 FindAdjacentFreeCell: {adjacents.Count} celdas adyacentes encontradas");

        if (adjacents.Count == 0)
        {
            Debug.LogWarning($"⚠️ FindAdjacentFreeCell: No hay celdas adyacentes a {targetPos}");
            return null;
        }

        // Filtrar celdas válidas (libres y neutrales)
        List<CellData> validCells = new List<CellData>();

        foreach (var cellData in adjacents)
        {
            bool isFree = cellData.unitOnCell == null;
            bool isNeutral = cellData.owner == -1;
            bool notReserved = !reservedCellsThisTurn.Contains(cellData.coordinates);

            Debug.Log($"  Celda {cellData.coordinates}: Libre={isFree}, Neutral={isNeutral}, NoReservada={notReserved}");

            if (isFree && isNeutral && notReserved)
            {
                validCells.Add(cellData);
            }
        }

        Debug.Log($"✅ FindAdjacentFreeCell: {validCells.Count} celdas válidas encontradas");

        if (validCells.Count == 0)
        {
            Debug.LogWarning($"⚠️ FindAdjacentFreeCell: No hay celdas libres alrededor de {targetPos}");
            return null;
        }

        // ⭐ Seleccionar la celda más cercana al atacante usando distancia hexagonal
        CellData bestCell = null;
        int shortestDistance = int.MaxValue;

        foreach (var cell in validCells)
        {
            int distance = BoardManager.Instance.Distance(forUnit.misCoordenadasActuales, cell.coordinates);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                bestCell = cell;
            }
        }

        if (bestCell != null)
        {
            Debug.Log($"🎯 Mejor celda elegida: {bestCell.coordinates} (distancia: {shortestDistance})");
            return bestCell.coordinates;
        }

        Debug.LogWarning($"⚠️ FindAdjacentFreeCell: No se pudo determinar mejor celda");
        return null;
    }

    // 🎯 Obtiene el nivel de amenaza cerca de un asentamiento específico
    private float GetThreatLevelNearSettlement(Unit settlement)
    {
        if (settlement == null || generalBrain == null || generalBrain.aiAnalysis == null)
        {
            return 0f;
        }

        // Verificar que sea un asentamiento
        if (settlement.statsBase.nombreUnidad != TypeUnit.Poblado && 
            settlement.statsBase.nombreUnidad != TypeUnit.Ciudad)
        {
            return 0f;
        }

        // Usar el mismo sistema que AI_General para calcular amenaza local
        var aiAnalysis = generalBrain.aiAnalysis;
        if (aiAnalysis.threatMap == null) return 0f;

        float maxThreat = 0f;
        int radius = 3; // Radio de búsqueda (mismo que patrullaje)
        int gridRadius = BoardManager.Instance != null ? BoardManager.Instance.gridRadius : 10;

        // Convertir a coordenadas de mapa
        int centerX = settlement.misCoordenadasActuales.x + (gridRadius - 1);
        int centerY = settlement.misCoordenadasActuales.y + (gridRadius - 1);

        // Buscar amenaza máxima en el radio
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                if (x >= 0 && y >= 0 && 
                    x < aiAnalysis.threatMap.GetLength(0) && 
                    y < aiAnalysis.threatMap.GetLength(1))
                {
                    float cellThreat = aiAnalysis.threatMap[x, y];
                    if (cellThreat > maxThreat)
                    {
                        maxThreat = cellThreat;
                    }
                }
            }
        }

        return maxThreat;
    }

    // 🎯 Selecciona el mejor asentamiento para producción militar basándose en amenaza cercana
    private Unit SelectBestSettlementForMilitaryProduction()
    {
        List<Unit> allUnits = myArmyManager.GetAllUnits();
        Unit bestSettlement = null;
        float maxThreat = -1f;

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            // Solo considerar asentamientos
            if (unit.statsBase.nombreUnidad != TypeUnit.Poblado && 
                unit.statsBase.nombreUnidad != TypeUnit.Ciudad)
            {
                continue;
            }

            // Calcular amenaza local
            float threatLevel = GetThreatLevelNearSettlement(unit);
            
            Debug.Log($"🎯 Amenaza cerca de {unit.name} ({unit.statsBase.nombreUnidad}): {threatLevel:F1}");

            // Actualizar el mejor si tiene más amenaza
            if (threatLevel > maxThreat)
            {
                maxThreat = threatLevel;
                bestSettlement = unit;
            }
        }

        if (bestSettlement != null)
        {
            Debug.Log($"✅ {bestSettlement.name} seleccionado como mejor productor (amenaza: {maxThreat:F1})");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró ningún asentamiento válido para producción");
        }

        return bestSettlement;
    }
}