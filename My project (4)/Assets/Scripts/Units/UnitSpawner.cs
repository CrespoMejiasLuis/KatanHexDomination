using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    // Singleton para que el GameManager pueda llamarlo fácilmente
    public static UnitSpawner Instance { get; private set; }

    [Header("Prefabs de Unidades")]
    [Tooltip("Arrastra aquí el prefab del Colono (debe tener el script 'Unit')")]
    public GameObject colonoPrefab;

    private void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Punto de entrada principal. El GameManager llamará a esto después
    /// de que el tablero se haya generado.
    /// </summary>
    public void SpawnInitialUnits()
    {
        // 1. Obtener referencias
        Player humanPlayer = GameManager.Instance.humanPlayer;
        Player aiPlayer = GameManager.Instance.IAPlayer;
        if (humanPlayer == null || aiPlayer == null)
        {
            Debug.LogError("¡Faltan referencias a humanPlayer o IAPlayer en el GameManager!");
            return;
        }
        // 2. Encontrar el radio de tierra (lo leemos del generador)
        HexGridGenerator gridGenerator = FindObjectOfType<HexGridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogError("¡UnitSpawner no pudo encontrar el HexGridGenerator!");
            return;
        }

        // 'boardRadius' es el número de anillos de TIERRA
        // (según el script del generador que me enviaste)
        int landRadius = gridGenerator.boardRadius;

        // 3. Calcular Coordenadas de Inicio
        // Queremos los extremos del mapa (el anillo más externo) pero centrados.
        // En un mapa "pointy-top", el eje vertical centrado es q=0.
        // Restamos 1 al radio para obtener el índice del anillo más externo.
        int spawnOffset = landRadius - 1;

        // Si el radio es 1, el offset es 0. Ambos spawns serían (0,0).
        // Si el radio es 3, el offset es 2. Spawns en (0, 2) y (0, -2).
        if (spawnOffset < 0) spawnOffset = 0;

        // Coordenadas opuestas y centradas: (0, offset) y (0, -offset)
        Vector2Int aiStartCoords = new Vector2Int(0, spawnOffset); // "Abajo" y centrado
        Vector2Int humanStartCoords = new Vector2Int(0, -spawnOffset);   // "Arriba" y centrado

        // Caso especial: si el radio es 1, ambos spawns son (0,0).
        // Debemos mover a la IA a una casilla adyacente.
        if (humanStartCoords == aiStartCoords && landRadius > 1)
        {
            aiStartCoords = new Vector2Int(1, -1); // Movemos la IA (ej: arriba-derecha)
            Debug.LogWarning("Las coordenadas de inicio eran idénticas. Moviendo IA a (1,-1).");
        }

        // 4. Instanciar las unidades
        Debug.Log("--- Spawneando Unidades Iniciales ---");
        SpawnUnitAt(humanPlayer, humanStartCoords);
        SpawnUnitAt(aiPlayer, aiStartCoords);
    }

    /// <summary>
    /// Función helper que crea una unidad en el tablero y actualiza todos los sistemas.
    /// </summary>
    /// <param name="owner">El jugador que será dueño de la unidad (Humano o IA)</param>
    /// <param name="coords">Las coordenadas axiales donde debe aparecer</param>
    private void SpawnUnitAt(Player owner, Vector2Int coords)
    {
        // --- 1. Validaciones ---
        if (owner == null)
        {
            Debug.LogError("SpawnUnitAt falló: ¡El 'owner' es nulo!"); return;
        }
        if (colonoPrefab == null)
        {
            Debug.LogError($"SpawnUnitAt ({owner.playerName}) falló: ¡El 'colonoPrefab' no está asignado en el Inspector!"); return;
        }

        // --- 2. Obtener Celda Lógica ---
        CellData cell = BoardManager.Instance.GetCell(coords);
        if (cell == null)
        {
            Debug.LogError($"Spawn fallido ({owner.playerName}): No se encontró CellData en las coords {coords}. ¿Es agua o está fuera del mapa?");
            return;
        }
        if (cell.typeUnitOnCell != TypeUnit.None)
        {
            Debug.LogWarning($"Spawn fallido ({owner.playerName}): Ya hay una unidad en {coords}");
            return;
        }

        // --- 3. Obtener Celda Visual ---
        HexTile visualTile = cell.visualTile;
        if (visualTile == null)
        {
            Debug.LogError($"Spawn fallido ({owner.playerName}): La CellData en {coords} no tiene un 'visualTile' asignado.");
            return;
        }

        // --- 4. Instanciar la Unidad ---
        GameObject unitGO = Instantiate(colonoPrefab, visualTile.transform.position, Quaternion.identity);
        Unit unit = unitGO.GetComponent<Unit>();

 

        // --- 5. Configurar la Unidad (Script Unit.cs) ---
        unit.misCoordenadasActuales = coords;
        unit.ownerID = owner.playerID;
        unitGO.name = $"Colono_{owner.playerName}";

        // --- 6. Actualizar la Celda Lógica (Script CellData.cs) ---
        cell.unitOnCell = unit; // Referencia directa a la unidad
        cell.typeUnitOnCell = unit.statsBase.nombreUnidad; // El tipo de unidad

        // --- 7. Registrar en la "Librería" (Script PlayerArmyManager.cs) ---
        owner.ArmyManager.RegisterUnit(unit);

        Debug.Log($"Colono para {owner.playerName} instanciado en {coords}");
    }
}