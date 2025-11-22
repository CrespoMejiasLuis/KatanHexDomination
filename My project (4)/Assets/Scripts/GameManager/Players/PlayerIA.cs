using UnityEngine;
using System.Collections;
using System.Linq;

public class PlayerIA : Player
{
    private AIAnalysisManager aiBrain;

    protected override void Awake()
    {
        base.Awake();
        aiBrain = FindObjectOfType<AIAnalysisManager>();
    }

    public override void BeginTurn()
    {
        Debug.Log($"🟢 --- INICIO TURNO IA ({playerName}) ---");
        StartCoroutine(ExecuteAITurn());
    }

    private IEnumerator ExecuteAITurn()
    {
        // 1. CHEQUEO DE CEREBRO
        if (aiBrain == null)
        {
            Debug.LogError("❌ IA ERROR: No encuentro el script AIAnalysisManager en la escena.");
            GameManager.Instance.EndAITurn();
            yield break;
        }

        // 2. PERCEPCIÓN
        Debug.Log("🧠 IA: Calculando mapas de influencia...");
        aiBrain.CalculateBaseMaps(this.playerID);

        yield return new WaitForSeconds(1.0f);

        // 3. BUSCAR UNIDAD
        // Buscamos cualquier unidad nuestra que tenga el componente UnitBuilder (Colono)
        var myUnits = ArmyManager.GetAllUnits();
        Debug.Log($"🔍 IA: Tengo {myUnits.Count} unidades registradas en mi ejército.");

        Unit colono = myUnits.FirstOrDefault(u => u.GetComponent<UnitBuilder>() != null);

        if (colono == null)
        {
            Debug.LogError("❌ IA ERROR: No encuentro ningún Colono en mi lista de unidades.");
            // (Aquí podrías intentar atacar si tienes soldados, pero por ahora terminamos)
        }
        else
        {
            Debug.Log($"✅ IA: Colono encontrado: {colono.name} en {colono.misCoordenadasActuales}. Movimientos: {colono.movimientosRestantes}");

            // 4. DECIDIR DESTINO
            Vector2Int? bestSpot = aiBrain.GetBestPositionForExpansion();

            if (bestSpot.HasValue)
            {
                Debug.Log($"🎯 IA: Objetivo decidido en {bestSpot.Value}. Intentando mover...");

                // Intentamos mover
                yield return MoverUnidadHacia(colono, bestSpot.Value);
            }
            else
            {
                Debug.LogWarning("⚠️ IA ALERTA: GetBestPositionForExpansion devolvió null. Todos los recursos valen 0 o están ocupados.");
            }
        }

        // 5. TERMINAR
        Debug.Log("🔴 IA: Fin de turno.");
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.EndAITurn();
    }

    private IEnumerator MoverUnidadHacia(Unit unit, Vector2Int targetCoords)
    {
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement == null)
        {
            Debug.LogError($"❌ IA ERROR: La unidad {unit.name} no tiene script UnitMovement.");
            yield break;
        }

        // Obtenemos la celda objetivo
        CellData targetCell = BoardManager.Instance.GetCell(targetCoords);

        if (targetCell == null)
        {
            Debug.LogError($"❌ IA ERROR: La celda en {targetCoords} no existe en el BoardManager.");
            yield break;
        }

        if (targetCell.visualTile == null)
        {
            Debug.LogError($"❌ IA ERROR: La celda en {targetCoords} no tiene visualTile asignado.");
            yield break;
        }

        Debug.Log($"🏃 IA: Llamando a IntentarMover hacia {targetCell.visualTile.name}...");

        // --- LLAMADA AL MOVIMIENTO ---
        bool seMovio = movement.IntentarMover(targetCell.visualTile);

        if (seMovio)
        {
            Debug.Log("✅ IA: ¡Movimiento exitoso! Esperando animación...");
            yield return new WaitForSeconds(2.0f);
        }
        else
        {
            Debug.LogError("❌ IA ERROR: IntentarMover devolvió FALSE. Revisa los logs de UnitMovement.");
            // Causas comunes: No hay puntos de movimiento, la celda está muy lejos (teletransporte no permitido si validas adyacencia), o coste muy alto.
        }
    }
}