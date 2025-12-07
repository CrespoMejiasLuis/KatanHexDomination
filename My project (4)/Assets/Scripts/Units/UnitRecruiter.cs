using UnityEngine;
using System.Collections.Generic;

public class UnitRecruiter : MonoBehaviour
{
    [Header("Prefabs de unidades")]
    public GameObject artilleroPrefab;
    public GameObject caballeroPrefab;
    public GameObject colonoPrefab;   // ← AÑADIDO
    private Player jugador; 
    private Unit unitCerebro;

    
    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
        jugador = (unitCerebro.ownerID == 1) ? GameManager.Instance.IAPlayer : GameManager.Instance.humanPlayer;
    }

    // -----------------------------
    //        ARTILLERO
    // -----------------------------
    public void ConstruirArtillero(Unit unidadCreadora)
    {
        if (artilleroPrefab == null)
        {
            Debug.LogError("⚠️ No hay prefab de Artillero asignado.");
            return;
        }

        Vector2Int ciudadCoords = unidadCreadora.misCoordenadasActuales;
        CellData ciudadCell = BoardManager.Instance.GetCell(ciudadCoords);

        if (ciudadCell == null)
        {
            Debug.LogError("⚠️ Celda de la ciudad/poblado inválida.");
            return;
        }

        Unit artilleroUnitPrefab = artilleroPrefab.GetComponent<Unit>();
        if (artilleroUnitPrefab.statsBase == null)
        {
            Debug.LogError("⚠️ El Artillero no tiene UnitStats asignado.");
            return;
        }

        Player jugador = GetOwnerPlayer(unidadCreadora.ownerID);

        if (jugador == null) return;

        // REMOVIDO EL BLOQUEO DIRECTO, USAMOS GetValidSpawnPosition LUEGO
        /*
        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }
        */

        //Check Unit on cell
        Unit unidadEnCasilla = ciudadCell.unitOnCell;
        // YA NO BLOQUEAMOS AQUÍ, porque buscamos vecino
        //if(unidadEnCasilla != null && unidadEnCasilla != unidadCreadora) ...


        Dictionary<ResourceType, int> coste = artilleroUnitPrefab.statsBase.GetProductCost();

        if(jugador.numPoblados > 1)
        {
            coste = artilleroUnitPrefab.actualizarCostes(coste, jugador);
        }

        if (!jugador.CanAfford(coste))
        {
            Debug.Log("No hay recursos suficientes para construir Artillero.");
            return;
        }

        // --- NUEVO: Buscar posición válida (centro o vecinos) ---
        Vector2Int spawnCoords = GetValidSpawnPosition(ciudadCoords, unidadCreadora);
        if (spawnCoords == new Vector2Int(-999, -999))
        {
             Debug.Log("⚠️ No hay espacio para instanciar Artillero (ciudad rodeada).");
             return; // Cancelar si no hay sitio
        }

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        //Debug.Log("Artillero creado con éxito en " + spawnCoords);

        // Instanciar en la posición visual de la celda encontrada
        CellData spawnCell = BoardManager.Instance.GetCell(spawnCoords);
        Vector3 spawnPos = spawnCell.visualTile.transform.position + Vector3.up * 0.5f;

        GameObject nuevoArtilleroGO = Instantiate(artilleroPrefab, spawnPos, Quaternion.identity);
        Unit nuevoArtillero = nuevoArtilleroGO.GetComponent<Unit>();

        if (nuevoArtillero != null)
        {
            nuevoArtillero.ownerID = unidadCreadora.ownerID;
            nuevoArtillero.misCoordenadasActuales = spawnCoords;
            nuevoArtillero.startWithZeroMovement = true;
            
            // Registrar unidad en la celda
            spawnCell.unitOnCell = nuevoArtillero;
            
            // Si NO es la celda de la ciudad, actualizamos el tipo de unidad en la celda
            // (Si spawnea en la ciudad, NO sobrescribimos 'Ciudad' con 'Artillero')
            if (spawnCell.typeUnitOnCell != TypeUnit.Ciudad && spawnCell.typeUnitOnCell != TypeUnit.Poblado)
            {
                spawnCell.typeUnitOnCell = nuevoArtillero.statsBase.nombreUnidad;
            }
        }
        jugador.ArmyManager.RegisterUnit(nuevoArtillero);
    }

    // -----------------------------
    //        CABALLERO
    // -----------------------------
    public void ConstruirCaballero(Unit unidadCreadora)
    {
        if (caballeroPrefab == null)
        {
            Debug.LogError("⚠️ No hay prefab de Caballero asignado.");
            return;
        }

        Vector2Int ciudadCoords = unidadCreadora.misCoordenadasActuales;
        CellData ciudadCell = BoardManager.Instance.GetCell(ciudadCoords);

        if (ciudadCell == null)
        {
            Debug.LogError("⚠️ Celda de la ciudad/poblado inválida.");
            return;
        }

        Unit caballeroUnitPrefab = caballeroPrefab.GetComponent<Unit>();
        if (caballeroUnitPrefab.statsBase == null)
        {
            Debug.LogError("⚠️ El Caballero no tiene UnitStats asignado.");
            return;
        }

        Player jugador = GetOwnerPlayer(unidadCreadora.ownerID);

        if (jugador == null) return;
        
        // REMOVIDO BLOQUEO
        /*
        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }
        */

        Dictionary<ResourceType, int> coste = caballeroUnitPrefab.statsBase.GetProductCost();

        if(jugador.numPoblados > 1)
        {
            coste = caballeroUnitPrefab.actualizarCostes(coste, jugador);
        }

        if (!jugador.CanAfford(coste))
        {
            Debug.Log("No hay recursos suficientes para construir Caballero.");
            return;
        }

        Vector2Int spawnCoords = GetValidSpawnPosition(ciudadCoords, unidadCreadora);
        if (spawnCoords == new Vector2Int(-999, -999))
        {
             Debug.Log("⚠️ No hay espacio para instanciar Caballero.");
             return;
        }

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        CellData spawnCell = BoardManager.Instance.GetCell(spawnCoords);
        Vector3 spawnPos = spawnCell.visualTile.transform.position + Vector3.up * 0.5f;

        GameObject nuevoCaballeroGO = Instantiate(caballeroPrefab, spawnPos, Quaternion.identity);
        Unit nuevoCaballero = nuevoCaballeroGO.GetComponent<Unit>();
        
        if (nuevoCaballero != null)
        {
            nuevoCaballero.ownerID = unidadCreadora.ownerID;
            nuevoCaballero.misCoordenadasActuales = spawnCoords;
            nuevoCaballero.startWithZeroMovement = true;
            
            spawnCell.unitOnCell = nuevoCaballero;
            if (spawnCell.typeUnitOnCell != TypeUnit.Ciudad && spawnCell.typeUnitOnCell != TypeUnit.Poblado)
            {
                spawnCell.typeUnitOnCell = nuevoCaballero.statsBase.nombreUnidad;
            }
        }

        jugador.ArmyManager.RegisterUnit(nuevoCaballero);
    }

    // -----------------------------
    //        COLONO (AÑADIDO)
    // -----------------------------
    public void ConstruirColono(Unit unidadCreadora)
    {
        if (colonoPrefab == null)
        {
            Debug.LogError("⚠️ No hay prefab de Colono asignado.");
            return;
        }

        Vector2Int ciudadCoords = unidadCreadora.misCoordenadasActuales;
        CellData ciudadCell = BoardManager.Instance.GetCell(ciudadCoords);

        if (ciudadCell == null)
        {
            Debug.LogError("⚠️ Celda de la ciudad/poblado inválida.");
            return;
        }

        Unit colonoUnitPrefab = colonoPrefab.GetComponent<Unit>();
        if (colonoUnitPrefab.statsBase == null)
        {
            Debug.LogError("⚠️ El Colono no tiene UnitStats asignado.");
            return;
        }

        Player jugador = GetOwnerPlayer(unidadCreadora.ownerID);

        if (jugador == null) return;

        // REMOVIDO BLOQUEO
        /*
        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }
        */

        Dictionary<ResourceType, int> coste = colonoUnitPrefab.statsBase.GetProductCost();

        if(jugador.numPoblados > 1)
        {
            coste = colonoUnitPrefab.actualizarCostes(coste, jugador);
        }

        if (!jugador.CanAfford(coste))
        {
            Debug.Log("No hay recursos suficientes para construir Colono.");
            return;
        }

        Vector2Int spawnCoords = GetValidSpawnPosition(ciudadCoords, unidadCreadora);
        if (spawnCoords == new Vector2Int(-999, -999))
        {
             Debug.Log("⚠️ No hay espacio para instanciar Colono.");
             return;
        }

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        CellData spawnCell = BoardManager.Instance.GetCell(spawnCoords);
        Vector3 spawnPos = spawnCell.visualTile.transform.position + Vector3.up * 0.5f;

        GameObject nuevoColonoGO = Instantiate(colonoPrefab, spawnPos, Quaternion.identity);
        Unit nuevoColono = nuevoColonoGO.GetComponent<Unit>();
        
        if (nuevoColono != null)
        {
            nuevoColono.ownerID = unidadCreadora.ownerID;
            nuevoColono.misCoordenadasActuales = spawnCoords;
            nuevoColono.startWithZeroMovement = true;

            spawnCell.unitOnCell = nuevoColono;
            if (spawnCell.typeUnitOnCell != TypeUnit.Ciudad && spawnCell.typeUnitOnCell != TypeUnit.Poblado)
            {
                spawnCell.typeUnitOnCell = nuevoColono.statsBase.nombreUnidad;
            }
        }

        jugador.ArmyManager.RegisterUnit(nuevoColono);
    }

    private Player GetOwnerPlayer(int ownerID)
    {
        // Asumiendo que 0 es Humano y 1 es IA
        if (ownerID == 0) return GameManager.Instance.humanPlayer;
        else if (ownerID == 1) return GameManager.Instance.IAPlayer;
        
        Debug.LogError($"UnitRecruiter: ownerID desconocido ({ownerID}).");
        return null;
    }

    /// <summary>
    /// Busca una posición para spawnear:
    /// 1. La propia ciudad si está vacía de UNIDADES MÓVILES.
    /// 2. Un vecino libre si la ciudad está ocupada.
    /// </summary>
    public static Vector2Int GetValidSpawnPosition(Vector2Int center, Unit cityUnit)
    {
        CellData centerCell = BoardManager.Instance.GetCell(center);
        
        // 1. Chequear Centro
        // Es válido si NO tiene unidad, O si la unidad es la propia ciudad (factory)
        // PERO cuidado: si ya spawneamos una unidad encima, unitOnCell será esa unidad, no la ciudad.
        // Así que: si unitOnCell == cityUnit (la ciudad misma), está libre para spawn.
        // Si unitOnCell == null, libre.
        // Si unitOnCell != cityUnit && unitOnCell != null => Ocupado por otra tropa.
        
        if (centerCell.unitOnCell == null || centerCell.unitOnCell == cityUnit)
        {
            return center;
        }

        // 2. Si está ocupado, buscar vecinos libres
        foreach (Vector2Int dir in GameManager.axialNeighborDirections)
        {
            Vector2Int neighborPos = center + dir;
            CellData neighbor = BoardManager.Instance.GetCell(neighborPos);
            
            if (neighbor != null)
            {
                // Condiciones: 
                // - Sin unidad
                // - Que sea transitable (no agua/montaña impasable si aplica) - Asumimos ResourceType check si necesario
                // - No ser territorio enemigo (opcional, pero seguro)
                
                // NOTA: Si el vecino tiene una ciudad/poblado, unitOnCell será esa ciudad.
                // No queremos spawnear ENCIMA de otro poblado vecino.
                if (neighbor.unitOnCell == null && neighbor.resource != ResourceType.Desierto) // Asumiendo Desierto no transitable? O solo visual.
                {
                    return neighborPos;
                }
            }
        }

        // 3. Fallo total
        return new Vector2Int(-999, -999);
    }
}
