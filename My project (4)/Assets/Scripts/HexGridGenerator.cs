using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections; // Necesario para IEnumerator

// Clase serializable para mapear el Tipo de Recurso con su Prefab 3D.
[System.Serializable]
public class HexPrefabMapping
{
    public ResourceType type;
    public GameObject prefab;
}

public class HexGridGenerator : MonoBehaviour
{
    // --- Configuración Global ---
    [Header("Configuración Global del Tablero")]
    [Tooltip("El número de anillos de TIERRA (sin incluir el borde de agua). 3 = 19 casillas.")]
    [HideInInspector]public int boardRadius = MenuManager.selectedBoardRadius; // ¡NUEVO! Reemplaza la constante GRID_RADIUS
    public float hexRadius = 1f;
    public Transform hexParent;

    // --- Constantes Eliminadas ---
    // private const int NUM_TILES = 19; // (Reemplazado por lógica dinámica)
    // private const int GRID_RADIUS = 3; // (Reemplazado por boardRadius)

    [Header("Configuración de Animación")]
    public float delayBetweenTiles = 0.05f;

    private List<HexTile> allGeneratedTiles = new List<HexTile>();
    // private GameManager gameManager; // (Eliminado, SetUp ahora es llamado por GameManager)

    // --- Configuración de Prefabs y Recursos ---
    [Header("Configuración de Prefabs por Recurso")]
    // ¡IMPORTANTE! Asegúrate de asignar un prefab para 'Agua' aquí.
    public List<HexPrefabMapping> resourcePrefabs;

    // CORREGIDO: Este es ahora el "pool base" para un tablero de 19 casillas.
    // Se repetirá o se acortará si 'boardRadius' es diferente de 3.
    private readonly List<ResourceType> baseResourcePool = new List<ResourceType>
    {
        // 4 Madera
        ResourceType.Madera, ResourceType.Madera, ResourceType.Madera, ResourceType.Madera, 
        // 3 Arcilla
        ResourceType.Arcilla, ResourceType.Arcilla, ResourceType.Arcilla, 
        // 4 Trigo
        ResourceType.Trigo, ResourceType.Trigo, ResourceType.Trigo, ResourceType.Trigo, 
        // 4 Oveja
        ResourceType.Oveja, ResourceType.Oveja, ResourceType.Oveja, ResourceType.Oveja, 
        // 3 Roca
        ResourceType.Roca, ResourceType.Roca, ResourceType.Roca, 
        // 1 Desierto
        ResourceType.Desierto
    };

    // ---------------------------------------------------------------------
    void Awake()
    {
        boardRadius = MenuManager.selectedBoardRadius;
    }

    // Start() se usa para validaciones
    void Start()
    {
        if (resourcePrefabs == null || resourcePrefabs.Count < 7) // (6 + Agua)
        {
            Debug.LogError("[ERROR] ¡Error de Configuración! Asegúrate de asignar los 7 Prefabs (incluyendo Agua) en el Inspector.");
            return;
        }

        if (resourcePrefabs.FirstOrDefault(m => m.type == ResourceType.Agua) == null)
        {
            Debug.LogError("[ERROR] ¡Error! No se ha asignado un prefab para 'ResourceType.Agua' en 'resourcePrefabs'.");
        }
    }

    // SetUp es llamado por el GameManager
    public void SetUp(Action onGenerationComplete)
    {
        // 2. Posicionar y configurar las casillas
        PlaceAndConfigureTiles(); // Ya no necesita 'coords'
        StartCoroutine(StartFlipSequence(onGenerationComplete));
    }

    System.Collections.IEnumerator StartFlipSequence(Action onCompleteCallback)
    {
        yield return null;

        foreach (HexTile tile in allGeneratedTiles)
        {
            // Asumimos que tienes StartFlipAnimation() en HexTile
            tile.StartFlipAnimation(); 
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        onCompleteCallback?.Invoke();
    }

    /// <summary>
    /// Genera una lista de coordenadas para un hexágono de 'radius' anillos.
    /// radius = 1 -> 1 casilla
    /// radius = 2 -> 7 casillas
    /// radius = 3 -> 19 casillas
    /// </summary>
    private List<Vector2Int> GenerateHexCoordinates(int radius)
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        // CORREGIDO: Esta fórmula genera un hexágono de 'radius' anillos (ej. 0, 1, 2)
        for (int q = -radius + 1; q < radius; q++)
        {
            for (int r = -radius + 1; r < radius; r++)
            {
                int s = -q - r;
                // La condición clave para un hexágono de radio 'R-1' (ej. 3-1=2)
                if (Mathf.Abs(q) < radius && Mathf.Abs(r) < radius && Mathf.Abs(s) < radius)
                {
                    coords.Add(new Vector2Int(q, r));
                }
            }
        }

        // CORREGIDO: El bucle 'while' que forzaba 21 casillas se ha eliminado.
        return coords;
    }

    private Vector3 AxialToWorldPosition(int q, int r)
    {
        float size = hexRadius;
        float width = Mathf.Sqrt(3) * size;
        float height = 2f * size;
        float x = q * width + r * width * 0.5f;
        float z = r * height * 0.75f;
        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// REESCRITO: Ahora genera casillas de tierra Y un borde de agua dinámicamente.
    /// </summary>
    private void PlaceAndConfigureTiles()
    {
        // 1. Obtener el prefab de Agua
        GameObject waterPrefab = resourcePrefabs.FirstOrDefault(m => m.type == ResourceType.Agua)?.prefab;
        if (waterPrefab == null)
        {
            Debug.LogError("[ERROR] No se encontró el prefab de Agua. Abortando generación.");
            return;
        }

        // 2. Generar coordenadas
        // Radio total = tierra + 1 anillo de agua
        int totalRadius = boardRadius + 1;
        List<Vector2Int> allCoords = GenerateHexCoordinates(totalRadius);
        // Coordenadas solo de tierra (usa un HashSet para búsquedas rápidas)
        HashSet<Vector2Int> landCoords = new HashSet<Vector2Int>(GenerateHexCoordinates(boardRadius));

        // 3. Crear el pool de recursos de tierra dinámicamente
        List<ResourceType> landResourcePool = new List<ResourceType>();
        int landTileCount = landCoords.Count;
        int basePoolIndex = 0;
        for (int i = 0; i < landTileCount; i++)
        {
            // Repetir el pool base si es necesario
            if (basePoolIndex >= baseResourcePool.Count)
            {
                basePoolIndex = 0;
            }
            landResourcePool.Add(baseResourcePool[basePoolIndex]);
            basePoolIndex++;
        }
        Shuffle(landResourcePool); // Aleatorizar el pool de tierra

        // 4. Inicializar el BoardManager con el radio TOTAL
        if (BoardManager.Instance == null)
        {
            Debug.LogError("[ERROR] BoardManager.Instance es NULL.");
            return;
        }
        BoardManager.Instance.InitialiceGrid(boardRadius);

        // 5. Instanciar y configurar todas las casillas (Tierra y Agua)
        int landPoolIndex = 0; // Índice para el pool de tierra aleatorizado

        foreach (Vector2Int coord in allCoords)
        {
            Vector3 worldPos = AxialToWorldPosition(coord.x, coord.y);
            GameObject prefabToUse;
            ResourceType currentType;

            // Comprobar si esta coordenada está en el set de tierra
            if (landCoords.Contains(coord))
            {
                // Es TIERRA
                currentType = landResourcePool[landPoolIndex];
                prefabToUse = resourcePrefabs.FirstOrDefault(m => m.type == currentType)?.prefab;
                landPoolIndex++;
            }
            else
            {
                // Es AGUA (el borde)
                currentType = ResourceType.Agua;
                prefabToUse = waterPrefab;
            }

            // Fallback por si falta un prefab de tierra (usa agua en su lugar)
            if (prefabToUse == null)
            {
                Debug.LogWarning($"[WARNING] No se encontró prefab para: {currentType}. Usando Agua por defecto.");
                prefabToUse = waterPrefab;
                currentType = ResourceType.Agua;
            }

            // --- Instanciación (igual que antes) ---
            GameObject newTileGO = Instantiate(prefabToUse, worldPos, Quaternion.identity);
            newTileGO.name = $"HexTile ({coord.x},{coord.y}) - {currentType}";

            if (hexParent != null)
            {
                newTileGO.transform.SetParent(hexParent);
            }

            HexTile hexTile = newTileGO.GetComponent<HexTile>();
            hexTile.Initialize(currentType, coord);

            CellData cell = new CellData(currentType, coord);
            cell.visualTile = hexTile;

            // Solo guardamos en la matriz si es una celda de tierra
            if (landCoords.Contains(coord))
            {
                BoardManager.Instance.SetCell(coord, cell);
            }

            allGeneratedTiles.Add(hexTile);
        }

        Debug.Log($"[OK] Tablero de {landTileCount} casillas de tierra y {allCoords.Count - landTileCount} de agua generado.");

        BoardManager.Instance.PrintGridData();
    }


    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1); // Rango inclusivo
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}