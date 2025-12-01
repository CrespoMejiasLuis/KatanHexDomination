using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    public static PathVisualizer Instance { get; private set; }

    private LineRenderer lineRenderer;

    [Header("Configuración")]
    public float heightOffset = 0.5f; // Altura sobre el suelo para que no se solape

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; // Empezar apagado
    }

    /// <summary>
    /// Dibuja una línea que conecta las casillas de la ruta.
    /// </summary>
    public void DrawPath(List<Vector2Int> pathCoords)
    {
        if (pathCoords == null || pathCoords.Count < 2)
        {
            HidePath();
            return;
        }

        lineRenderer.positionCount = pathCoords.Count;

        for (int i = 0; i < pathCoords.Count; i++)
        {
            // 1. Obtener la celda para saber su posición en el mundo
            CellData cell = BoardManager.Instance.GetCell(pathCoords[i]);

            if (cell != null && cell.visualTile != null)
            {
                // 2. Calcular posición (añadiendo un poco de altura Y)
                Vector3 worldPos = cell.visualTile.transform.position;
                worldPos.y += heightOffset;

                lineRenderer.SetPosition(i, worldPos);
            }
        }
    }

    public void HidePath()
    {
        lineRenderer.positionCount = 0;
    }
}