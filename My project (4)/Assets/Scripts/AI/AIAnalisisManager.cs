using UnityEngine;

public class AIAnalysisManager : MonoBehaviour
{

    [Header("Debug Visual")]
    public MapType debugMapToDraw = MapType.None;
    [Range(0.1f, 1f)]
    public float gizmoRadius = 0.6f;

    // --- LAS 3 CAPAS DE VISI�N ---
    public float[,] threatMap;
    public float[,] resourceMap;
    public float[,] territoryMap;

    private int width;
    private int height;

    // Necesitamos esto para mirar a los vecinos
    private static readonly Vector2Int[] axialNeighborDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    public void InitializeMaps()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.gridData == null) return;

        CellData[,] grid = BoardManager.Instance.gridData;
        width = grid.GetLength(0);
        height = grid.GetLength(1);

        threatMap = new float[width, height];
        resourceMap = new float[width, height];
        territoryMap = new float[width, height];
    }

    public void CalculateBaseMaps(int aiPlayerID)
    {
        if (threatMap == null) InitializeMaps();

        System.Array.Clear(threatMap, 0, threatMap.Length);
        System.Array.Clear(resourceMap, 0, resourceMap.Length);
        System.Array.Clear(territoryMap, 0, territoryMap.Length);

        CellData[,] grid = BoardManager.Instance.gridData;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = grid[x, y];
                if (cell == null) continue;

                // --- A. MAPA DE RECURSOS (Valoraci�n Econ�mica) ---
                float resourceValue = 0;

                // 1. Valor Base del Recurso
                switch (cell.resource)
                {
                    case ResourceType.Madera:
                    case ResourceType.Arcilla: resourceValue = 10f; break;
                    case ResourceType.Trigo:
                    case ResourceType.Oveja: resourceValue = 15f; break;
                    case ResourceType.Roca: resourceValue = 20f; break;
                    default: resourceValue = 0f; break;
                }

                // 2. Penalizaci�n: �Ya hay una ciudad AQU�?
                bool isOccupied = cell.typeUnitOnCell == TypeUnit.Poblado || cell.typeUnitOnCell == TypeUnit.Ciudad;
                if (isOccupied) resourceValue = 0;

                // 3. Penalizaci�n: �Hay una ciudad VECINA? (�NUEVO!)
                // Si un vecino es ciudad, este recurso ya est� siendo explotado (o bloqueado para construir).
                if (resourceValue > 0) // Solo comprobamos si la casilla vale la pena
                {
                    foreach (Vector2Int dir in axialNeighborDirections)
                    {
                        Vector2Int neighborCoords = cell.coordinates + dir;
                        CellData neighbor = BoardManager.Instance.GetCell(neighborCoords);

                        if (neighbor != null)
                        {
                            bool neighborHasCity = neighbor.typeUnitOnCell == TypeUnit.Poblado ||
                                                   neighbor.typeUnitOnCell == TypeUnit.Ciudad;

                            if (neighborHasCity)
                            {
                                // El recurso est� bloqueado por una ciudad adyacente
                                resourceValue = 0;
                                break; // Dejamos de buscar, ya est� invalidado
                            }
                        }
                    }
                }

                resourceMap[x, y] = resourceValue;


                // --- B. MAPA DE AMENAZA (Militar) ---
                if (cell.unitOnCell != null)
                {
                    if (cell.unitOnCell.ownerID != aiPlayerID)
                    {
                        // Unidades enemigas generan amenaza
                        // (Poblados enemigos tambi�n podr�an generar amenaza si quieres)
                        threatMap[x, y] = 50f;
                    }
                }

                // --- C. MAPA DE TERRITORIO (Expansi�n) ---
                if (cell.owner != -1)
                {
                    if (cell.owner == aiPlayerID)
                        territoryMap[x, y] = 1f;
                    else
                        territoryMap[x, y] = -1f;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // (Mismo c�digo de visualizaci�n que ten�as antes)
        if (debugMapToDraw == MapType.None || BoardManager.Instance == null) return;
        CellData[,] grid = BoardManager.Instance.gridData;
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = grid[x, y];
                if (cell == null || cell.visualTile == null) continue;

                float value = 0;
                Color color = Color.clear;

                switch (debugMapToDraw)
                {
                    case MapType.Threat:
                        if (threatMap != null) value = threatMap[x, y];
                        color = Color.red;
                        break;
                    case MapType.Resources:
                        if (resourceMap != null) value = resourceMap[x, y];
                        color = Color.magenta;
                        break;
                    case MapType.Territory:
                        if (territoryMap != null) value = territoryMap[x, y];
                        color = Color.blue;
                        break;
                }

                if (Mathf.Abs(value) > 0.1f)
                {
                    color.a = Mathf.Clamp(Mathf.Abs(value) / 20f, 0.3f, 0.9f);
                    Gizmos.color = color;
                    Vector3 drawPos = cell.visualTile.transform.position + Vector3.up * 1.5f;
                    Gizmos.DrawSphere(drawPos, gizmoRadius);
                }
            }
        }
    }
}