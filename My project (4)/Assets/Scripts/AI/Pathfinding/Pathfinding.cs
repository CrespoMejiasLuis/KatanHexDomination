using UnityEngine;
using System;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    private float[,] gScore;
    private float[,] fScore;
    private Vector2Int[,] cameFrom; 
    private List<Vector2Int> openSet;

    private const float THREAT_FACTOR = 2.0f;
    private const float RESOURCE_BONUS_FACTOR = 0.5f;

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

        openSet = new List<Vector2Int>();

        //poner los valores de start
        gScore[start.x, start.y] = 0;
        fScore[start.x, start.y] = gScore[start.x, start.y] + Heuristic(start, goal);

        //add el start al openSet
        openSet.Add(start);

        //bucle principal
        Vector2Int current;
        while (openSet.Count > 0)
        {
            // buscar el nodo con menor fScore
            int bestIndex = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore[openSet[i].x, openSet[i].y] < fScore[openSet[bestIndex].x, openSet[bestIndex].y])
                    bestIndex = i;
            }

            current = openSet[bestIndex];
            openSet.RemoveAt(bestIndex);

            if(current.Equals(goal)) //si es el destino
                return ReconstructPath(current); //reconstruye el camino
            
            //encontrar vecinos
            List<Vector2Int> neigthbors = GetNeigthbors(current);
            //recorrer vecinos
            foreach(var neigthbor in neigthbors)
            {
                //obtener info de cells
                CellData currentCell = BoardManager.Instance.GetCell(current);
                CellData neighborCell = BoardManager.Instance.GetCell(neigthbor);

                //coste base de casilla por movimiento
                float baseCost = (float)neighborCell.cost;

                //si hay amenaza tiene un coste adicional la casilla
                float threatPenalty = threatMap[neigthbor.x, neigthbor.y] * THREAT_FACTOR;

                //coste dinamico movimiento
                float tentativeGscore = gScore[current.x, current.y] + baseCost + threatPenalty;

                if(tentativeGscore < gScore[neigthbor.x, neigthbor.y])
                {
                    //mejor camino
                    cameFrom[neigthbor.x, neigthbor.y] = current;
                    gScore[neigthbor.x, neigthbor.y] = tentativeGscore;

                    //coste total
                    fScore[neigthbor.x, neigthbor.y] = tentativeGscore + Heuristic(neigthbor, goal);

                    if(!openSet.Contains(neigthbor))
                    {
                        openSet.Add(neigthbor);
                    }
                }
            }
        }

        //devuelve lista vacia si no encuentra le camino
        return new List<Vector2Int>();
    }

    private float Heuristic(Vector2Int start, Vector2Int goal)
    {
        float dx = Mathf.Abs(start.x-goal.x);
        float dy = Mathf.Abs(start.y-goal.y);

        float axialDistance = dx + dy;
        
        return axialDistance;
    }

    private List<Vector2Int> GetNeigthbors(Vector2Int current)
    {
        List<Vector2Int> neigthbors = new List<Vector2Int>();
        Vector2Int neighbor;

        foreach (var dir in GameManager.axialNeighborDirections)
        {
            neighbor = current + dir;
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

        Vector2Int previous = cameFrom[current.x, current.y];

        while(!previous.Equals(Vector2Int.zero) && cameFrom[previous.x, previous.y] != Vector2Int.zero)
        {
            totalPath.Add(previous);
            previous = cameFrom[previous.x, previous.y];
        }

        totalPath.Reverse();
        return totalPath;
    }
}
