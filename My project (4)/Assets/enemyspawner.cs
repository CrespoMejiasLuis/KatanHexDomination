using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab del enemigo a spawnear")]
    public GameObject enemyPrefab;

    /// <summary>
    /// Spawnea un enemigo en las coordenadas axiales indicadas.
    /// SOLO para debug y pruebas.
    /// </summary>
    public void SpawnEnemigo(Vector2Int coords)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("❌ No hay prefab enemigo asignado en EnemySpawner.");
            return;
        }

        // 1. Obtener la celda
        CellData cell = BoardManager.Instance.GetCell(coords);
        if (cell == null)
        {
            Debug.LogError("❌ Coordenadas fuera del tablero: " + coords);
            return;
        }

        // 2. Instanciar el enemigo
        GameObject enemigoGO = Instantiate(
            enemyPrefab,
            cell.visualTile.transform.position,
            Quaternion.identity
        );

        Unit enemyUnit = enemigoGO.GetComponent<Unit>();

        // 3. Registrar datos
        enemyUnit.misCoordenadasActuales = coords;
        enemyUnit.ownerID = 1; // Dueño IA/enemigo

        // 4. Actualizar la celda
        cell.unitOnCell = enemyUnit;
        cell.owner = 1;
        cell.typeUnitOnCell = enemyUnit.statsBase.nombreUnidad;

        Debug.Log($"👹 Enemigo spawneado en {coords}");
    }
}
