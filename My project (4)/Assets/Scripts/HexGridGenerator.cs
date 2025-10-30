using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Clase serializable para mapear el Tipo de Recurso con su Prefab 3D.
[System.Serializable]
public class HexPrefabMapping
{
    public HexTile.ResourceType type;
    public GameObject prefab;
}

public class HexGridGenerator : MonoBehaviour
{
    // --- Configuración Global ---
    [Header("Configuración Global del Tablero")]
    public float hexRadius = 1f;    // Radio del hexágono (determina el tamaño y espaciado).
    public Transform hexParent;     // Objeto vacío para mantener el orden en la jerarquía.

    private const int NUM_TILES = 19;
    private const int GRID_RADIUS = 3; // Radio del patrón base.
    
    [Header("Configuración de Animación")]
    public float delayBetweenTiles = 0.05f; // Tiempo de espera entre el volteo de cada casilla

    private List<HexTile> allGeneratedTiles = new List<HexTile>();

    // --- Configuración de Prefabs y Recursos ---
    [Header("Configuración de Prefabs por Recurso")]
    // Asigna aquí los 6 Prefabs en el Inspector.
    public List<HexPrefabMapping> resourcePrefabs;

    // Los 21 tipos de recursos (ejemplo de una mezcla estándar de Catan + 2 extra).
    private readonly List<HexTile.ResourceType> resourcePool = new List<HexTile.ResourceType>
    {
        // 4 Madera
        HexTile.ResourceType.Madera, HexTile.ResourceType.Madera, HexTile.ResourceType.Madera, HexTile.ResourceType.Madera, 
        // 3 Arcilla
        HexTile.ResourceType.Arcilla, HexTile.ResourceType.Arcilla, HexTile.ResourceType.Arcilla, 
        // 4 Trigo
        HexTile.ResourceType.Trigo, HexTile.ResourceType.Trigo, HexTile.ResourceType.Trigo, HexTile.ResourceType.Trigo, 
        // 4 Oveja
        HexTile.ResourceType.Oveja, HexTile.ResourceType.Oveja, HexTile.ResourceType.Oveja, HexTile.ResourceType.Oveja, 
        // 3 Roca
        HexTile.ResourceType.Roca, HexTile.ResourceType.Roca, HexTile.ResourceType.Roca, 
        // 3 Desierto (o casillas especiales sin recurso primario)
        HexTile.ResourceType.Desierto, HexTile.ResourceType.Desierto, HexTile.ResourceType.Desierto
    };

    // ---------------------------------------------------------------------

    void Start()
    {
        if (resourcePrefabs == null || resourcePrefabs.Count < 6)
        {
            Debug.LogError("🚨 ¡Error de Configuración! Asegúrate de asignar los 6 Prefabs de recursos en el Inspector.");
            return;
        }

        // 1. Generar los puntos de coordenadas (q, r)
        List<Vector2Int> hexCoordinates = GenerateHexCoordinates(GRID_RADIUS);

        // 2. Posicionar y configurar las casillas con aleatoriedad
        PlaceAndConfigureTiles(hexCoordinates);
        StartCoroutine(StartFlipSequence());
        Debug.Log($"✅ Tablero de {NUM_TILES} casillas generado aleatoriamente.");
    }

    /// <summary>
    /// Genera la lista de coordenadas axiales (q, r) para un tablero hexagonal.
    /// </summary>
    /// 

    System.Collections.IEnumerator StartFlipSequence()
    {
        // Espera un pequeño momento antes de empezar la animación (e.g., para que todo cargue)
        yield return null;

        // Aleatorizar el orden de volteo para un efecto más dinámico (opcional)
        Shuffle(allGeneratedTiles);

        foreach (HexTile tile in allGeneratedTiles)
        {
            tile.StartFlipAnimation();
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        // Una vez terminado, puedes iniciar la lógica del juego
        // StartGameLogic(); 
    }
    private List<Vector2Int> GenerateHexCoordinates(int radius)
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        // Generar un patrón de panal (radio 3 genera 19 casillas)
        for (int q = -radius + 1; q < radius; q++)
        {
            for (int r = -radius + 1; r < radius; r++)
            {
                int s = -q - r;
                if (Mathf.Abs(q) < radius && Mathf.Abs(r) < radius && Mathf.Abs(s) < radius)
                {
                    coords.Add(new Vector2Int(q, r));
                }
            }
        }

        // Asegurarse de tener NUM_TILES (21)
        while (coords.Count < NUM_TILES)
        {
            // Añade casillas en el borde (ej. en el lado de la columna q=radius-1)
            coords.Add(new Vector2Int(radius - 1, -(radius - 1) - (coords.Count - 19)));
        }

        return coords.Take(NUM_TILES).ToList();
    }

    /// <summary>
    /// Convierte las coordenadas axiales (q, r) a posición de Unity (X, Z) para hexágonos "Flat Top".
    /// </summary>
    /// <summary>
    /// Convierte las coordenadas axiales (q, r) a posición de Unity (X, Z) para hexágonos "Pointy Top" (Estilo Catan).
    /// </summary>
    private Vector3 AxialToWorldPosition(int q, int r)
    {
        // Usamos 'hexRadius' como la distancia del centro al lado (apotema), o el radio externo.
        float size = hexRadius;

        // Fórmulas para Hexágonos de vértice (Pointy Top):

        // El ancho horizontal (X) es lo que define las columnas.
        float width = Mathf.Sqrt(3) * size; // ~1.732 * size

        // La altura vertical (Z) es lo que define el espaciado vertical.
        float height = 2f * size;

        // X: q se desplaza en X por el factor de ancho, y r también añade un pequeño desplazamiento.
        float x = q * width + r * width * 0.5f;

        // Z: r se desplaza en Z por el factor de altura, pero con el factor de 0.75f (1.5 / 2).
        float z = r * height * 0.75f;

        // La Y (altura) es 0 para un tablero plano
        return new Vector3(x, 0, z);
    }
    /// <summary>
    /// Instancia las casillas usando el prefab correcto y les asigna recursos.
    /// </summary>
    private void PlaceAndConfigureTiles(List<Vector2Int> coords)
    {
        // 1. Aleatorizar la lista de recursos
        Shuffle(resourcePool);

        // 2. Instanciar y configurar
        for (int i = 0; i < coords.Count; i++)
        {
            Vector2Int coord = coords[i];
            Vector3 worldPos = AxialToWorldPosition(coord.x, coord.y);

            // Recurso a asignar en esta posición
            HexTile.ResourceType currentType = resourcePool[i];

            // **Buscar el Prefab correcto**
            GameObject prefabToUse = resourcePrefabs
                .FirstOrDefault(m => m.type == currentType)?.prefab;

            if (prefabToUse == null)
            {
                Debug.LogError($"🚫 No se encontró el prefab para: {currentType}. Revisar 'resourcePrefabs'.");
                continue;
            }

            // Instanciar y nombrar la casilla
            GameObject newTileGO = Instantiate(prefabToUse, worldPos, Quaternion.identity);
            newTileGO.name = $"HexTile ({coord.x},{coord.y}) - {currentType}";

            if (hexParent != null)
            {
                newTileGO.transform.SetParent(hexParent);
            }

            // Inicializar la lógica de la casilla
            HexTile hexTile = newTileGO.GetComponent<HexTile>();
            if (hexTile != null)
            {
                // **LLAMADA SIMPLIFICADA**: Solo se inicializa el tipo de recurso
                hexTile.Initialize(currentType);
                allGeneratedTiles.Add(hexTile);
            }
        }
    }

    /// <summary>
    /// Algoritmo Fisher-Yates para aleatorizar una lista (shuffle).
    /// </summary>
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1); // Rango inclusivo
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}