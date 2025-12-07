using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance {get; private set;}

    [HideInInspector]public int gridRadius;
    [HideInInspector]public CellData[,] gridData;

    private void Awake()
    {
        if (Instance!=null && Instance !=this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitialiceGrid(int radius)
    {
        gridRadius = radius;
        int gridSize = gridRadius * 2 -1;
        gridData = new CellData[gridSize, gridSize];
    }

    public void SetCell(Vector2Int coords, CellData cell)
    {
        int x = coords.x + (gridRadius-1);
        int y = coords.y + (gridRadius-1);
        gridData[x, y] = cell;
    }

    public CellData GetCell(Vector2Int axialCoords)
    {
        int x = axialCoords.x + (gridRadius-1);
        int y = axialCoords.y + (gridRadius-1);
        if (x < 0 || y < 0 || x >= gridData.GetLength(0) || y >= gridData.GetLength(1))
            return null;

        return gridData[x, y];
    }

    public void PrintGridData()
    {
        if (gridData == null)
        {
            //Debug.LogError("⚠️ gridData está vacío o no inicializado.");
            return;
        }

        int width = gridData.GetLength(0);
        int height = gridData.GetLength(1);

        //Debug.Log($"📋 Imprimiendo tablero {width}x{height}");

        for (int x = 0; x < width; x++)
        {
            string row = "";
            for (int y = 0; y < height; y++)
            {
                CellData cell = gridData[x, y];
                if (cell != null)
                    row += $"{cell.resource.ToString()[0]} ";  // Solo primera letra del tipo (M, A, T...)
                else
                    row += "_ "; // vacío
            }
           // Debug.Log($"Fila {x}: {row}");
        }
    }
    public int Distance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        int dz = -(a.x + a.y) - -(b.x + b.y);

        return (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz)) / 2;
    }

    public List<CellData> GetAdjacents(Vector2Int axial)
    {
        List<CellData> resultado = new List<CellData>();

        foreach (var d in GameManager.axialNeighborDirections)
        {
            CellData c = GetCell(axial + d);
            if (c != null)
                resultado.Add(c);
        }

        return resultado;
    }

    public void HideAllBorders()
    {
        int width = gridData.GetLength(0);
        int height = gridData.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = gridData[x, y];

                if (cell != null && cell.visualTile != null)
                {
                    // Si la celda tiene dueño (es territorio de Ciudad/Poblado)
                    if (cell.owner != -1)
                    {
                        // Restauramos su estado visual de territorio (Azul/Rojo y Visible)
                        // Esto corrige cualquier color temporal de selección que tuviera
                        cell.UpdateVisual();
                    }
                    else
                    {
                        // Si es neutral...
                        if (cell.isRaided)
                        {
                            // Si está saqueada, mostramos borde negro
                            cell.UpdateVisual(); 
                        }
                        else
                        {
                            // Si no, ocultamos todo
                            cell.visualTile.SetBorderVisible(false);
                        }
                    }
                }
            }
        }
    }

    public List<CellData> GetCellsInRange(Vector2Int center, int range)
    {
        List<CellData> results = new List<CellData>();
        for (int q = -range; q <= range; q++)
        {
            for (int r = -range; r <= range; r++)
            {
                int s = -q - r;
                if (Mathf.Abs(q) <= range && Mathf.Abs(r) <= range && Mathf.Abs(s) <= range)
                {
                    Vector2Int neighborCoord = center + new Vector2Int(q, r);
                    // No incluir el centro si no quieres, pero para chequear ocupación da igual
                    CellData c = GetCell(neighborCoord);
                    if (c != null) results.Add(c);
                }
            }
        }
        return results;
    }   
    public void UpdateAllBorders()
    {
        if (gridData == null) return;

        // Definimos las 6 direcciones en el MISMO ORDEN que tu array de borderSegments en HexTile
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // 0: Derecha
            new Vector2Int(1, -1),  // 1: Arriba-Derecha
            new Vector2Int(0, -1),  // 2: Arriba-Izquierda
            new Vector2Int(-1, 0),  // 3: Izquierda
            new Vector2Int(-1, 1),  // 4: Abajo-Izquierda
            new Vector2Int(0, 1)    // 5: Abajo-Derecha
        };

        foreach (CellData cell in gridData)
        {
            if (cell == null || cell.visualTile == null) continue;

            // Si la celda está saqueada, PRIORIDAD TOTAL: Borde Negro
            if (cell.isRaided)
            {
                cell.visualTile.EnableFullBorder(Color.black);
                continue;
            }

            // Si la celda no tiene dueño, apagamos todos sus bordes
            if (cell.owner == -1)
            {
                cell.visualTile.SetBorderVisible(false);
                continue;
            }

            // Si TIENE dueño, miramos sus 6 vecinos para ver dónde pintar raya
            Color myColor = (cell.owner == 0) ? Color.blue : Color.red;

            for (int i = 0; i < 6; i++)
            {
                Vector2Int neighborPos = cell.coordinates + directions[i];
                CellData neighbor = GetCell(neighborPos);

                // ¿Debo dibujar frontera aquí?
                // SÍ, si el vecino NO existe (fin del mapa)
                // SÍ, si el vecino tiene un dueño DIFERENTE al mío (frontera enemiga o neutral)
                // NO, si el vecino tiene el MISMO dueño que yo (territorio interno)

                bool drawBorder = false;

                if (neighbor == null)
                {
                    drawBorder = true; // Borde del mapa
                }
                else if (neighbor.owner != cell.owner)
                {
                    drawBorder = true; // Frontera con otro territorio
                }

                // Aplicar al segmento específico
                cell.visualTile.SetSegmentVisible(i, drawBorder, myColor);
            }
        }
    }

}
