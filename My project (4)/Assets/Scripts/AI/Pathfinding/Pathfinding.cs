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
    // En Pathfinding.cs

    public List<Vector2Int> FindSmartPath(Vector2Int start, Vector2Int goal, float[,] threatMap)
    {
        int width = threatMap.GetLength(0);
        int height = threatMap.GetLength(1);

        // Reiniciar estructuras
        gScore = InitializeScore(width, height);
        fScore = InitializeScore(width, height);
        cameFrom = new Vector2Int[width, height];

        // Inicializar cameFrom con un valor centinela claro
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cameFrom[x, y] = new Vector2Int(-9999, -9999);

        openSet = new List<Vector2Int>();

        // Índices del array
        Vector2Int startIdx = GetGridIndex(start);
        Vector2Int goalIdx = GetGridIndex(goal);

        // Validar límites antes de empezar
        if (startIdx.x < 0 || startIdx.x >= width || startIdx.y < 0 || startIdx.y >= height ||
            goalIdx.x < 0 || goalIdx.x >= width || goalIdx.y < 0 || goalIdx.y >= height)
        {
            Debug.LogError($"Pathfinding: Inicio {start} o Meta {goal} fuera de límites.");
            return new List<Vector2Int>();
        }

        gScore[startIdx.x, startIdx.y] = 0;
        fScore[startIdx.x, startIdx.y] = Heuristic(start, goal);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            // Encontrar nodo con menor F
            Vector2Int current = openSet[0];
            Vector2Int currentIdx = GetGridIndex(current);
            float lowestF = fScore[currentIdx.x, currentIdx.y];
            int bestIndex = 0;

            for (int i = 1; i < openSet.Count; i++)
            {
                Vector2Int nodeIdx = GetGridIndex(openSet[i]);
                float f = fScore[nodeIdx.x, nodeIdx.y];
                if (f < lowestF)
                {
                    lowestF = f;
                    current = openSet[i];
                    bestIndex = i;
                }
            }

            // Si llegamos al destino
            if (current == goal)
            {
                return ReconstructPath(current, start);
            }

            openSet.RemoveAt(bestIndex);
            currentIdx = GetGridIndex(current); // Recalcular índice actual

            foreach (Vector2Int neighbor in GetNeigthbors(current))
            {
                Vector2Int neighborIdx = GetGridIndex(neighbor);

                // Chequeo de seguridad de índices
                if (neighborIdx.x < 0 || neighborIdx.x >= width || neighborIdx.y < 0 || neighborIdx.y >= height)
                    continue;

                CellData neighborCell = BoardManager.Instance.GetCell(neighbor);
                if (neighborCell == null) continue; // No es transitable (agua, vacío)

                // Calcular coste
                float baseCost = (float)neighborCell.cost;
                float threat = threatMap[neighborIdx.x, neighborIdx.y] * THREAT_FACTOR;
                float tentativeGScore = gScore[currentIdx.x, currentIdx.y] + baseCost + threat;

                if (tentativeGScore < gScore[neighborIdx.x, neighborIdx.y])
                {
                    // ¡Camino encontrado! Guardamos de dónde venimos
                    cameFrom[neighborIdx.x, neighborIdx.y] = current;
                    gScore[neighborIdx.x, neighborIdx.y] = tentativeGScore;
                    fScore[neighborIdx.x, neighborIdx.y] = tentativeGScore + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        Debug.LogWarning("Pathfinding: No se encontró camino.");
        return new List<Vector2Int>(); // Retorna lista vacía si falla
    }

    private List<Vector2Int> ReconstructPath(Vector2Int current, Vector2Int start)
    {
        List<Vector2Int> totalPath = new List<Vector2Int>();
        totalPath.Add(current);

        Vector2Int currentIdx = GetGridIndex(current);
        Vector2Int startIdx = GetGridIndex(start);

        // Bucle de seguridad (max 1000 pasos)
        int safetyCount = 0;

        while (current != start && safetyCount < 1000)
        {
            Vector2Int previous = cameFrom[currentIdx.x, currentIdx.y];

            if (previous.x == -9999)
            {
                Debug.LogError("Error reconstruyendo camino: Eslabón perdido.");
                break;
            }

            totalPath.Add(previous);
            current = previous;
            currentIdx = GetGridIndex(current);
            safetyCount++;
        }

        totalPath.Reverse(); // Invertir para ir de Inicio -> Fin
        return totalPath;
    }
}
