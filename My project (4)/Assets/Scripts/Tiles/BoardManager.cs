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
            Debug.LogError("⚠️ gridData está vacío o no inicializado.");
            return;
        }

        int width = gridData.GetLength(0);
        int height = gridData.GetLength(1);

        Debug.Log($"📋 Imprimiendo tablero {width}x{height}");

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
            Debug.Log($"Fila {x}: {row}");
        }
    }
    public int Distance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        int dz = -(a.x + a.y) - -(b.x + b.y);

        return (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz)) / 2;
    }

}
