using UnityEngine;
using System.Collections;
using System.Linq; // Necesario para buscar unidades con LINQ

public class PlayerIA : Player
{
    [Header("Cerebros de la IA")]
    // Referencia al Analista de Mapas (Ojos)
    private AIAnalysisManager aiAnalysis; 
    
    public AI_General generalBrain; 
    private PlayerArmyManager myArmyManager;

    protected override void Awake()
    {
        base.Awake();
        aiAnalysis = FindFirstObjectByType<AIAnalysisManager>();
        myArmyManager = GetComponent<PlayerArmyManager>();
    }

    public override void BeginTurn()
    {
        Debug.Log($"🟢 --- INICIO TURNO IA ({playerName}) ---");
        StartCoroutine(ExecuteAITurn());
    }

    private IEnumerator ExecuteAITurn()
    {
        // ---------------------------------------------------------
        // PASO 1: VALIDACIÓN DE DEPENDENCIAS
        // ---------------------------------------------------------
        if (aiAnalysis == null || generalBrain == null)
        {
            Debug.LogError("❌ IA CRITICAL: Faltan referencias (AIAnalysisManager o AI_General).");
            GameManager.Instance.EndAITurn(); // Saltamos turno para no colgar el juego
            yield break;
        }

        // ---------------------------------------------------------
        // PASO 2: PERCEPCIÓN (Ver el mundo)
        // ---------------------------------------------------------
        Debug.Log("👀 IA: Calculando mapas de influencia...");
        aiAnalysis.CalculateBaseMaps(this.playerID);
        
        // Pequeña pausa dramática para que no sea instantáneo
        yield return new WaitForSeconds(2f); 

        // ---------------------------------------------------------
        // PASO 3: DECISIÓN ESTRATÉGICA (HFSM)
        // ---------------------------------------------------------
        Debug.Log("🤔 IA: El General está decidiendo estrategia...");
        generalBrain.DecideStrategy();

        // ---------------------------------------------------------
        // PASO 4: EJECUCIÓN TÁCTICA (Actuar según el Estado)
        // ---------------------------------------------------------
        // Aquí es donde el 'switch' dirige el tráfico según lo que decidió el General
        switch (generalBrain.currentTacticalState)
        {
            case TacticalState.EarlyExpansion:
                Debug.Log("⚡ TÁCTICA: Expansión Temprana (Prioridad: Colonos)");
                yield return ExecuteExpansionLogic();
                break;

            case TacticalState.Development:
                Debug.Log("🔨 TÁCTICA: Desarrollo (Prioridad: Mejorar Ciudades)");
                // yield return ExecuteDevelopmentLogic(); // (Aún por hacer)
                Debug.Log("... (Lógica de desarrollo pendiente) ...");
                break;

            case TacticalState.ActiveDefense:
                Debug.Log("🛡️ TÁCTICA: Defensa Activa (Prioridad: Proteger Fronteras)");
                // yield return ExecuteDefenseLogic(); // (Aún por hacer)
                Debug.Log("... (Lógica de defensa pendiente) ...");
                break;

            case TacticalState.Assault:
                Debug.Log("⚔️ TÁCTICA: Asalto (Prioridad: Atacar Enemigo)");
                // yield return ExecuteAssaultLogic(); // (Aún por hacer)
                Debug.Log("... (Lógica de asalto pendiente) ...");
                break;
        }

        // ---------------------------------------------------------
        // PASO 5: FINALIZAR TURNO
        // ---------------------------------------------------------
        Debug.Log("🔴 IA: Fin de turno.");
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.EndAITurn(); 
    }

    // =================================================================================
    // 🧠 LÓGICA ESPECÍFICA: EXPANSIÓN
    // =================================================================================
    private IEnumerator ExecuteExpansionLogic()
    {
        var myUnits = myArmyManager.GetAllUnits();
        
        // Buscamos un Colono (que tenga UnitBuilder) y tenga movimiento
        Unit colono = myUnits.FirstOrDefault(u => u.GetComponent<UnitBuilder>() != null && u.movimientosRestantes > 0);

        if (colono == null)
        {
            Debug.LogWarning("⚠️ IA: Quiero expandirme, pero no tengo Colonos disponibles.");
            yield break;
        }

        Debug.Log($"✅ IA: Colono encontrado ({colono.name}).");

        // Preguntar al Mapa de Influencia
        Vector2Int? bestSpot = aiAnalysis.GetBestPositionForExpansion();

        if (bestSpot.HasValue)
        {
            yield return MoveAndBuildRoutine(colono, bestSpot.Value);
        }
        else
        {
            Debug.LogWarning("⚠️ IA: No hay buenos sitios para expandirse.");
        }
    }

    private IEnumerator MoveAndBuildRoutine(Unit unit, Vector2Int targetCoords)
    {
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement == null) yield break;

        // 1. Obtener la celda objetivo del BoardManager
        CellData targetCell = BoardManager.Instance.GetCell(targetCoords);
        if (targetCell == null || targetCell.visualTile == null)
        {
            Debug.LogError("❌ IA: Celda objetivo inválida.");
            yield break;
        }

        // 2. Intentar Moverse
        // Nota: IntentarMover devuelve true si empezó a moverse
        bool isMoving = movement.IntentarMover(targetCell.visualTile);

        if (isMoving)
        {
            // Esperamos lo que creamos que tarda la animación (o un poco más)
            yield return new WaitForSeconds(5f); 
            
            // 3. Comprobar si ha llegado
            // (Verificamos si las coordenadas lógicas de la unidad coinciden con el destino)
            if (unit.misCoordenadasActuales == targetCoords)
            {
                Debug.Log("🏁 IA: Llegué al destino. Intentando construir...");
                
                // 4. Intentar Construir
                UnitBuilder builder = unit.GetComponent<UnitBuilder>();
                if (builder != null)
                {
                    builder.IntentarConstruirPoblado();
                    yield return new WaitForSeconds(1.0f); // Esperar animación de construcción
                }
            }
            else
            {
                Debug.Log("⏳ IA: Me he movido, pero aún no he llegado al destino final (se me acabaron los puntos).");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ IA: No pude moverme (quizás bloqueado o sin puntos).");
        }
    }
}