using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance {get; private set;}

    public int gridRadius;
    public CellData[,] gridData;

    private void Awake()
    {
        if (Instance!=null && Instance !=this)
        {
            Destroy(GameObject);
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

    public void GetCell(Vector2Int axialCoords)
    {
        int x = axialCoords.x + (gridRadius-1);
        int y = axialCoords.y + (gridRadius-1);
        if (x < 0 || y < 0 || x >= gridData.GetLength(0) || y >= gridData.GetLength(1))
            return null;

        return gridData[x, y];
    }
}
