using UnityEngine;
using System;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance{get; private set;}
    private float[,] gScore;
    private float[,] fScore;
    private Vector2Int[,] cameFrom; 
    private List<Vector2Int> openSet;

    private const float THREAT_FACTOR = 2.0f;
    private const float RESOURCE_BONUS_FACTOR = 0.5f;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // HELPER: Convierte Coordenada Axial (puede ser negativa) a Indice de Array (siempre positivo)
    private Vector2Int GetGridIndex(Vector2Int axialCoords)
    {
        int r = BoardManager.Instance.gridRadius;
        return new Vector2Int(axialCoords.x + (r - 1), axialCoords.y + (r - 1));
    }

    //inicializar matrices
    private float[,] InitializeScore(int width, int height)
    {
        float[,] matriz = new float[width, height];
        
        float infinity = float.PositiveInfinity;
        for(int i = 0; i<width; i++)
        {
            for(int j = 0; j<height; j++)
            {
                matriz[i, j] = infinity;
            }
        }
        return matriz;
    }

    //encontrar camino 
    public List<Vector2Int> FindSmartPath(Vector2Int start, Vector2Int goal, float[,] threatMap)
    {
        //inicializar
        int width = threatMap.GetLength(0);
        int height = threatMap.GetLength(1);
        gScore = InitializeScore(width, height);
        fScore = InitializeScore(width, height);

        cameFrom = new Vector2Int[width, height];
        for(int x=0; x<width; x++)
            for(int y=0; y<height; y++)
                cameFrom[x,y] = new Vector2Int(-9999, -9999); // Valor imposible

        openSet = new List<Vector2Int>();

        // CONVERSION: Para acceder a los arrays, usamos indices convertidos
        Vector2Int startIndex = GetGridIndex(start);

        //poner los valores de start
        gScore[start.x, start.y] = 0;
        fScore[start.x, start.y] = gScore[start.x, start.y] + Heuristic(start, goal);

        //add el start al openSet
        openSet.Add(start);

        //bucle principal
        while (openSet.Count > 0)
        {
            // buscar el nodo con menor fScore
            int bestIndex = 0;
            Vector2Int bestNodeIndex = GetGridIndex(openSet[0]);
            float lowestF = fScore[bestNodeIndex.x, bestNodeIndex.y];
            for (int i = 1; i < openSet.Count; i++)
            {
                Vector2Int currentIndex = GetGridIndex(openSet[i]);
                float currentF = fScore[currentIndex.x, currentIndex.y];
                
                if (currentF < lowestF)
                {
                    lowestF = currentF;
                    bestIndex = i;
                }
            }

            Vector2Int current = openSet[bestIndex];
            openSet.RemoveAt(bestIndex);

            if(current.Equals(goal)) //si es el destino
                return ReconstructPath(current); //reconstruye el camino
            
            //encontrar vecinos
            List<Vector2Int> neigthbors = GetNeigthbors(current);

            Vector2Int currentIdx = GetGridIndex(current);
            //recorrer vecinos
            foreach(var neigthbor in neigthbors)
            {
                //obtener info de cells
                CellData currentCell = BoardManager.Instance.GetCell(current);
                CellData neighborCell = BoardManager.Instance.GetCell(neigthbor);
                if (neighborCell == null) continue;

                //CONVERSION: Indice de array del vecino
                Vector2Int neighborIdx = GetGridIndex(neigthbor);
                // IMPORTANTE: Chequear limites del array antes de acceder
                if (neighborIdx.x < 0 || neighborIdx.x >= width || neighborIdx.y < 0 || neighborIdx.y >= height)
                    continue;

                //coste base de casilla por movimiento
                float baseCost = (float)neighborCell.cost;

                //si hay amenaza tiene un coste adicional la casilla
                float threatPenalty = threatMap[neighborIdx.x, neighborIdx.y] * THREAT_FACTOR;

                //coste dinamico movimiento
                float tentativeGScore = gScore[currentIdx.x, currentIdx.y] + baseCost + threatPenalty;

                if (tentativeGScore < gScore[neighborIdx.x, neighborIdx.y])
                {
                    cameFrom[neighborIdx.x, neighborIdx.y] = current;
                    gScore[neighborIdx.x, neighborIdx.y] = tentativeGScore;
                    fScore[neighborIdx.x, neighborIdx.y] = tentativeGScore + Heuristic(neigthbor, goal);

                    if (!openSet.Contains(neigthbor))
                    {
                        openSet.Add(neigthbor);
                    }
                }
            }
        }

        //devuelve lista vacia si no encuentra le camino
        return new List<Vector2Int>();
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // Distancia Axial (Correcta para hexagonos)
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs((-a.x - a.y) - (-b.x - b.y))) / 2.0f;
    }

    private List<Vector2Int> GetNeigthbors(Vector2Int current)
    {
        List<Vector2Int> neigthbors = new List<Vector2Int>();

        foreach (var dir in GameManager.axialNeighborDirections)
        {
            Vector2Int neighbor = current + dir;
            CellData neighborCell = BoardManager.Instance.GetCell(neighbor);

            if(neighborCell != null)
            {
                neigthbors.Add(neighbor);
            }
        }
        return neigthbors;
    }

    private List<Vector2Int> ReconstructPath(Vector2Int current)
    {
        List<Vector2Int> totalPath = new List<Vector2Int>();
        totalPath.Add(current);

        Vector2Int currentIdx = GetGridIndex(current);
        Vector2Int previous = cameFrom[currentIdx.x, currentIdx.y];

        while(previous.x != -9999) // Mientras sea un nodo valido
        {
            totalPath.Add(previous);

            currentIdx = GetGridIndex(previous);
            previous = cameFrom[currentIdx.x, currentIdx.y];

            if (totalPath.Count > 1000) break;
        }

        totalPath.Reverse();
        return totalPath;
    }
}
