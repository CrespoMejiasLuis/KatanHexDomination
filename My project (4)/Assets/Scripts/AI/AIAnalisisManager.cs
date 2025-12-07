using System.Collections.Generic;
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
    public float strategicSecureMultiplier = 5.0f; // Valor alto para forzar la decisión

    [Header("Configuración de Expansión")]
    public int minDistanceBetweenCities = 2; // Distancia mínima en casillas (Regla Catan)
    public float distancePenalty = 0.5f;     // Cuánto valor pierde una casilla por cada paso de distancia
    
    // Multiplicador de escasez: Cuánto más deseo un recurso si tengo 0.
    private const float SCARCITY_MULTIPLIER = 3.0f;

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

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    // Mantener valor original
                    if (originalMap[x, y] > 0.1f)
                    {
                        nextMap[x, y] = originalMap[x, y]; // Mantiene el 50f
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
    
    public Vector2Int? GetBestPositionForExpansion(Unit builderUnit, Player aiPlayer)
    {
        if (resourceMap == null || builderUnit == null || aiPlayer == null) return null;

        float bestScore = -9999f;
        Vector2Int bestCoords = Vector2Int.zero;
        bool found = false;
        Vector2Int unitPos = builderUnit.misCoordenadasActuales;

        // 🔑 PASO NUEVO: Chequeo Estratégico Previo
        // ¿Tenemos asegurado el futuro? (¿Tenemos Piedra?)
        bool hasStoneSource = HasResourceSource(aiPlayer.playerID, ResourceType.Roca);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = BoardManager.Instance.gridData[x, y];
                if (cell == null) continue;

                // 1. Filtro de Legalidad (Tu función existente)
                if (!IsBuildLocationValid(cell, aiPlayer.playerID)) continue;

                // 2. Filtro de Recurso: NO construir en desiertos
                if (cell.resource == ResourceType.Desierto)
                {
                    continue; // Saltar casillas sin recursos
                }

                // 3. Datos base
                float baseValue = resourceMap[x, y];
                float threat = threatMap[x, y];
                if (threat > 20f) continue;

                // 3. UTILIDAD DINÁMICA (Modificada para recibir el dato de la piedra)
                // Le pasamos 'hasStoneSource' para que sepa si debe priorizar la roca
                float dynamicValue = CalculateDynamicUtility(cell, baseValue, aiPlayer, hasStoneSource);

                // 4. Verificar que tenga valor mínimo real (no solo valor heredado de convolución)
                if (dynamicValue < 1.0f) continue; // Ignorar casillas con valor muy bajo

                // 5. Penalización por distancia
                int dist = BoardManager.Instance.Distance(unitPos, cell.coordinates);
                float distPenalty = dist * distancePenalty;

                float finalScore = dynamicValue - (threat * 2.0f) - distPenalty;

                if (finalScore > bestScore)
                {
                    bestScore = finalScore;
                    bestCoords = cell.coordinates;
                    found = true;
                }
            }
        }

        if (found)
        {
            CellData selectedCell = BoardManager.Instance.gridData[bestCoords.x + (BoardManager.Instance.gridRadius - 1), bestCoords.y + (BoardManager.Instance.gridRadius - 1)];
            Debug.Log($"🎯 Mejor posición encontrada: {bestCoords} | Recurso: {selectedCell?.resource} | Score: {bestScore:F2}");
            return bestCoords;
        }
        
        Debug.LogWarning("⚠️ No se encontró ninguna posición válida para expansión");
        return null;
    }

    // --- HELPER 1: VALIDACIÓN DE REGLAS ---
    // --- AIAnalysisManager.cs (Helper Method) ---

    private bool IsBuildLocationValid(CellData cell, int playerID)
    {
        // ---------------------------------------------------------
        // 1. REGLA DE PROPIEDAD
        // ---------------------------------------------------------
        
        // Solo bloquear si la casilla NO es neutral (-1)
        // Antes permitiamos 'cell.owner == playerID', pero UnitBuilder lo prohibe.
        if (cell.owner != -1)
        {
            return false; // Es territorio enemigo O propio prohibido para expandir
        }

        // A. Debe estar vacía de unidades físicas
        if (cell.unitOnCell != null) return false;

        // B. No puede haber ciudad ni poblado ya construido (Lógica redundante con owner != -1 pero segura)
        if (cell.typeUnitOnCell == TypeUnit.Poblado || cell.typeUnitOnCell == TypeUnit.Ciudad) 
            return false;

        // ---------------------------------------------------------
        // 2. REGLA DEL MAR / BORDE DEL MAPA (Solución al problema del mar)
        // ---------------------------------------------------------
        
        // Iteramos los 6 vecinos inmediatos para ver si la casilla es "Costera" o "Borde"
        foreach (Vector2Int dir in GameManager.axialNeighborDirections)
        {
            Vector2Int neighborCoords = cell.coordinates + dir;
            CellData neighbor = BoardManager.Instance.GetCell(neighborCoords);

            // CONDICIÓN DE MAR:
            // Si el vecino es NULL, significa que estamos al borde del grid (Vacío/Mar).
            // OPCIONAL: Comentado para permitir construcción en bordes del mapa
            // Descomentar si quieres evitar construcción costera
            // if (neighbor == null) 
            // {
            //     return false; 
            // }

            // Opcional: Si tienes un ResourceType.Agua explícito:
            // if (neighbor.resource == ResourceType.Agua) return false;
        }

        // ---------------------------------------------------------
        // 3. REGLA DE DISTANCIA A OTRAS CIUDADES (Regla Catan)
        // ---------------------------------------------------------
        
        // Buscamos en un radio alrededor de la casilla candidata (ej. radio 2)
        List<CellData> neighborsInRange = BoardManager.Instance.GetCellsInRange(cell.coordinates, minDistanceBetweenCities);
        
        foreach (var n in neighborsInRange)
        {
            // Si encontramos CUALQUIER asentamiento (propio o enemigo) cerca...
            if (n.typeUnitOnCell == TypeUnit.Poblado || n.typeUnitOnCell == TypeUnit.Ciudad)
            {
                return false; // Hay un edificio demasiado cerca, zona inválida.
            }
        }

        return true; // La casilla cumple todas las reglas estrictas.
    }

    // --- HELPER 2: UTILIDAD DINÁMICA ---
    // 🔑 Añadimos el parámetro 'hasStoneSource'
    private float CalculateDynamicUtility(CellData cell, float baseMapScore, Player player, bool hasStoneSource)
    {
        float score = baseMapScore;
        ResourceType res = cell.resource;

        if (res != ResourceType.Desierto)
        {
            // --- LÓGICA DE FUTURO (PIEDRA) ---
            // Si el recurso es ROCA y NO tenemos ninguna fuente de roca...
            if (res == ResourceType.Roca && !hasStoneSource)
            {
                // ¡PRIORIDAD MÁXIMA!
                // Multiplicamos x5 (strategicSecureMultiplier). 
                // Esto hará que una casilla de Roca lejana valga más que una de Madera cercana.
                return score * strategicSecureMultiplier; 
            }

            // --- LÓGICA DE NECESIDAD ACTUAL (Tu lógica anterior) ---
            int currentStock = 0;
            if (player.HasResourceKey(res)) 
            {
                currentStock = player.GetResourceAmount(res); 
            }

            // Escasez normal (para madera, trigo, etc.)
            float scarcityFactor = 1.0f + (3.0f / (currentStock + 1));
            score *= scarcityFactor;
        }

        return score;
    }

    // Comprueba si el jugador ya es dueño de al menos una casilla de este recurso
    private bool HasResourceSource(int playerID, ResourceType typeToCheck)
    {
        CellData[,] grid = BoardManager.Instance.gridData;
        
        foreach (CellData cell in grid)
        {
            if (cell != null && cell.owner == playerID && cell.resource == typeToCheck)
            {
                return true; // Ya tenemos una fuente de esto
            }
        }
        return false; // No tenemos ninguna fuente
    }

    // ======================================================================
    // MÉTODOS DE COMBATE
    // ======================================================================

    /// <summary>
    /// Encuentra la posición más amenazada cerca de ciudades propias
    /// para enviar unidades defensivas
    /// </summary>
    public Vector2Int? GetBestDefensePosition(int aiPlayerID, Vector2Int unitPos)
    {
        if (threatMap == null || territoryMap == null) return null;

        float bestScore = -1f;
        Vector2Int bestCoords = Vector2Int.zero;
        bool found = false;

        // Buscar casillas propias con alta amenaza
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = BoardManager.Instance.gridData[x, y];
                if (cell == null) continue;

                // Solo defender territorio propio o neutral cerca de nuestras ciudades
                if (cell.owner != aiPlayerID && cell.owner != -1) continue;

                float threat = threatMap[x, y];
                float territory = territoryMap[x, y];

                // Buscar zonas con amenaza alta Y cerca de territorio propio
                if (threat > 15f && territory >= 0)
                {
                    // Priorizar casillas más cercanas
                    int dist = BoardManager.Instance.Distance(unitPos, cell.coordinates);
                    float score = threat - (dist * 2f);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCoords = cell.coordinates;
                        found = true;
                    }
                }
            }
        }

        if (found)
        {
            Debug.Log($"🛡️ Posición defensiva encontrada: {bestCoords} (Amenaza: {bestScore:F1})");
            return bestCoords;
        }

        return null;
    }

    /// <summary>
    /// Encuentra el mejor objetivo enemigo para atacar
    /// Prioriza: ciudades débiles > unidades aisladas > targets cercanos
    /// </summary>
    public Unit GetBestAttackTarget(int aiPlayerID, Vector2Int unitPos)
    {
        if (BoardManager.Instance == null) return null;

        CellData[,] grid = BoardManager.Instance.gridData;
        Unit bestTarget = null;
        float bestScore = -9999f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = grid[x, y];
                if (cell == null || cell.unitOnCell == null) continue;

                // Solo unidades enemigas
                if (cell.unitOnCell.ownerID == aiPlayerID) continue;

                Unit enemy = cell.unitOnCell;
                float score = 0f;

                // PRIORIDAD 1: Ciudades y poblados (objetivos estratégicos)
                if (cell.typeUnitOnCell == TypeUnit.Ciudad)
                {
                    score = 100f;
                }
                else if (cell.typeUnitOnCell == TypeUnit.Poblado)
                {
                    score = 80f;
                }
                else
                {
                    // PRIORIDAD 2: Unidades militares (según poder)
                    score = enemy.statsBase.ataque * 2f + enemy.statsBase.vidaMaxima * 0.5f;
                }

                // Penalización por distancia
                int dist = BoardManager.Instance.Distance(unitPos, cell.coordinates);
                score -= dist * 3f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
        }

        if (bestTarget != null)
        {
            Debug.Log($"⚔️ Objetivo de ataque encontrado: {bestTarget.name} en {bestTarget.misCoordenadasActuales}");
        }

        return bestTarget;
    }

    /// <summary>
    /// Encuentra una posición de patrulla en el borde del territorio
    /// </summary>
    public Vector2Int? GetPatrolPosition(int aiPlayerID, Vector2Int unitPos)
    {
        if (territoryMap == null) return null;

        CellData[,] grid = BoardManager.Instance.gridData;
        List<Vector2Int> borderTiles = new List<Vector2Int>();

        // Buscar casillas en el borde (propias con vecinos neutrales/enemigos)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData cell = grid[x, y];
                if (cell == null || cell.owner != aiPlayerID) continue;

                // Chequear si tiene vecinos no-propios
                foreach (Vector2Int dir in GameManager.axialNeighborDirections)
                {
                    Vector2Int neighborCoords = cell.coordinates + dir;
                    CellData neighbor = BoardManager.Instance.GetCell(neighborCoords);

                    if (neighbor != null && neighbor.owner != aiPlayerID)
                    {
                        borderTiles.Add(cell.coordinates);
                        break;
                    }
                }
            }
        }

        if (borderTiles.Count > 0)
        {
            // Elegir una posición aleatoria del borde
            return borderTiles[Random.Range(0, borderTiles.Count)];
        }

        return null;
    }
}