using UnityEngine;

public class AIAnalysisManager : MonoBehaviour
{
    [Header("Debug Visual")]
    public MapType debugMapToDraw = MapType.None;
    [Range(0.1f, 1f)]
    public float gizmoRadius = 0.6f;

    [Header("Configuración de IA")]
    public int convolutionSteps = 2;
    public float decayFactor = 0.5f;

    // --- LAS 3 CAPAS DE VISIÓN ---
    public float[,] threatMap;
    public float[,] resourceMap;
    public float[,] territoryMap;

    private int width;
    private int height;

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

                // =================================================
                // 1. MAPA DE TERRITORIO (Base para los otros)
                // =================================================
                bool isEnemyTerritory = false;
                if (cell.owner != -1)
                {
                    if (cell.owner == aiPlayerID)
                    {
                        territoryMap[x, y] = 1f; // Mío
                    }
                    else
                    {
                        territoryMap[x, y] = -1f; // Enemigo
                        isEnemyTerritory = true;
                    }
                }

                // =================================================
                // 2. MAPA DE AMENAZA (Militar)
                // =================================================
                if (cell.unitOnCell != null)
                {
                    // Si hay una unidad y NO es mía
                    if (cell.unitOnCell.ownerID != aiPlayerID)
                    {
                        float threatValue = 50f; // Valor base para unidades moviles

                        // CORRECCIÓN! Los poblados enemigos son peligrosos (defensas)
                        // y estáticos, así que generan una amenaza constante alta.
                        if (cell.typeUnitOnCell == TypeUnit.Poblado || cell.typeUnitOnCell == TypeUnit.Ciudad)
                        {
                            threatValue = 80f; // Las ciudades asustan mas
                        }

                        threatMap[x, y] = threatValue;
                    }
                }

                // =================================================
                // 3. MAPA DE RECURSOS (Economico)
                // =================================================
                float resourceValue = 0;

                // Si es territorio enemigo, el recurso NO es accesible para la IA economica
                if (!isEnemyTerritory)
                {
                    switch (cell.resource)
                    {
                        case ResourceType.Madera: case ResourceType.Arcilla: resourceValue = 10f; break;
                        case ResourceType.Trigo: case ResourceType.Oveja: resourceValue = 15f; break;
                        case ResourceType.Roca: resourceValue = 20f; break;
                        default: resourceValue = 0f; break;
                    }

                    // Penalización: Si ya hay ciudad (propia o enemiga), valor 0
                    if (cell.typeUnitOnCell == TypeUnit.Poblado || cell.typeUnitOnCell == TypeUnit.Ciudad)
                    {
                        resourceValue = 0;
                    }

                    // Penalización: Si hay una ciudad VECINA (de CUALQUIERA), valor 0
                    // (No puedes construir pegado a otra ciudad)
                    if (resourceValue > 0)
                    {
                        foreach (Vector2Int dir in GameManager.axialNeighborDirections)
                        {
                            Vector2Int neighborCoords = cell.coordinates + dir;
                            CellData neighbor = BoardManager.Instance.GetCell(neighborCoords);

                            if (neighbor != null)
                            {
                                // Si el vecino es ciudad o ES TERRITORIO ENEMIGO, no queremos construir aquí
                                bool neighborBlocked =
                                    neighbor.typeUnitOnCell == TypeUnit.Poblado ||
                                    neighbor.typeUnitOnCell == TypeUnit.Ciudad ||
                                    (neighbor.owner != -1 && neighbor.owner != aiPlayerID); // Vecino es propiedad enemiga

                                if (neighborBlocked)
                                {
                                    resourceValue = 0;
                                    break;
                                }
                            }
                        }
                    }
                }

                resourceMap[x, y] = resourceValue;
            }
        }

        // --- APLICAR CONVOLUCIÓN ---
        resourceMap = ApplyConvolution(resourceMap, convolutionSteps, decayFactor);
        threatMap = ApplyConvolution(threatMap, convolutionSteps, decayFactor);
    }

    private float[,] ApplyConvolution(float[,] originalMap, int steps, float decay)
    {
        float[,] currentMap = originalMap;
        int w = currentMap.GetLength(0);
        int h = currentMap.GetLength(1);

        for (int step = 0; step < steps; step++)
        {
            float[,] nextMap = new float[w, h];
            System.Array.Copy(currentMap, nextMap, currentMap.Length);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (currentMap[x, y] > 0.1f)
                    {
                        float valueToPass = currentMap[x, y] * decay;
                        CellData centerCell = BoardManager.Instance.gridData[x, y];
                        if (centerCell == null) continue;

                        foreach (Vector2Int dir in GameManager.axialNeighborDirections)
                        {
                            Vector2Int neighborCoord = centerCell.coordinates + dir;
                            // TRUCO DE ÍNDICES (Mismo que en BoardManager)
                            int r = BoardManager.Instance.gridRadius;
                            int nX = neighborCoord.x + (r - 1);
                            int nY = neighborCoord.y + (r - 1);

                            if (nX >= 0 && nX < w && nY >= 0 && nY < h)
                            {
                                nextMap[nX, nY] += valueToPass;
                            }
                        }
                    }
                }
            }
            currentMap = nextMap;
        }
        return currentMap;
    }

    private void OnDrawGizmos()
    {
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
    
    public Vector2Int? GetBestPositionForExpansion()
    {
        if (resourceMap == null) return null;

        float bestValue = -1f;
        Vector2Int bestCoords = Vector2Int.zero;
        bool found = false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // La fórmula de decisión: Valor = Recursos - (Amenaza * Factor de Miedo)
                // Si hay amenaza (50), el valor bajará drásticamente.
                float score = resourceMap[x, y] - (threatMap[x, y] * 2.0f);

                // Solo nos interesan casillas válidas (con valor positivo)
                // Y que NO tengan ya una unidad nuestra (para no movernos encima de nosotros mismos)
                CellData cell = BoardManager.Instance.gridData[x, y];
                if (cell != null && cell.unitOnCell == null && score > bestValue)
                {
                    bestValue = score;
                    bestCoords = cell.coordinates;
                    found = true;
                }
            }
        }

        if (found) return bestCoords;
        return null;
    }
}