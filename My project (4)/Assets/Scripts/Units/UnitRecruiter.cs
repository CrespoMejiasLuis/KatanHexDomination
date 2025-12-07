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

        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }

        //Check Unit on cell
        Unit unidadEnCasilla = ciudadCell.unitOnCell;
        if(unidadEnCasilla != null && unidadEnCasilla != unidadCreadora)
        {
             Debug.Log("Casilla ocupada");
             return;
        }

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

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        Debug.Log("Artillero creado con éxito en la celda " + ciudadCoords);

        Vector3 spawnPos = unidadCreadora.transform.position + Vector3.up * 1.0f;
        GameObject nuevoArtilleroGO = Instantiate(artilleroPrefab, spawnPos, Quaternion.identity);

        Unit nuevoArtillero = nuevoArtilleroGO.GetComponent<Unit>();
        if (nuevoArtillero != null)
        {
            nuevoArtillero.ownerID = unidadCreadora.ownerID;
            nuevoArtillero.misCoordenadasActuales = unidadCreadora.misCoordenadasActuales;
            nuevoArtillero.startWithZeroMovement = true;
            
            // --- FIX: Asegurar que la Celda Lógica sepa que ahora hay una unidad ---
            ciudadCell.unitOnCell = nuevoArtillero;
            // Solo cambiamos el TIPO si NO es ciudad/poblado
            if (ciudadCell.typeUnitOnCell != TypeUnit.Ciudad && ciudadCell.typeUnitOnCell != TypeUnit.Poblado)
            {
               ciudadCell.typeUnitOnCell = nuevoArtillero.statsBase.nombreUnidad;
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
        
        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }

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

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        Debug.Log("Caballero creado con éxito en la celda " + ciudadCoords);

        Vector3 spawnPos = unidadCreadora.transform.position + Vector3.up * 1.0f;
        GameObject nuevoCaballeroGO = Instantiate(caballeroPrefab, spawnPos, Quaternion.identity);

        Unit nuevoCaballero = nuevoCaballeroGO.GetComponent<Unit>();
        if (nuevoCaballero != null)
        {
            nuevoCaballero.ownerID = unidadCreadora.ownerID;
            nuevoCaballero.misCoordenadasActuales = unidadCreadora.misCoordenadasActuales;
            nuevoCaballero.startWithZeroMovement = true;
            
             // --- FIX: Asegurar que la Celda Lógica sepa que ahora hay una unidad ---
            ciudadCell.unitOnCell = nuevoCaballero;
            
             // Solo cambiamos el TIPO si NO es ciudad/poblado
            if (ciudadCell.typeUnitOnCell != TypeUnit.Ciudad && ciudadCell.typeUnitOnCell != TypeUnit.Poblado)
            {
                ciudadCell.typeUnitOnCell = nuevoCaballero.statsBase.nombreUnidad;
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

        if (ciudadCell.unitOnCell != null && ciudadCell.unitOnCell != unidadCreadora)
        {
            Debug.Log("Poblado ocupado, no se puede crear unidad.");
            return;
        }

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

        bool gasto = jugador.SpendResources(coste);
        if (!gasto) return;

        Debug.Log("Colono creado con éxito en la celda " + ciudadCoords);

        Vector3 spawnPos = unidadCreadora.transform.position + Vector3.up * 1.0f;
        GameObject nuevoColonoGO = Instantiate(colonoPrefab, spawnPos, Quaternion.identity);

        Unit nuevoColono = nuevoColonoGO.GetComponent<Unit>();
        if (nuevoColono != null)
        {
            nuevoColono.ownerID = unidadCreadora.ownerID;
            nuevoColono.misCoordenadasActuales = unidadCreadora.misCoordenadasActuales;
            nuevoColono.startWithZeroMovement = true;

             // --- FIX: Asegurar que la Celda Lógica sepa que ahora hay una unidad ---
            ciudadCell.unitOnCell = nuevoColono;

            // Solo cambiamos el TIPO si NO es ciudad/poblado
            if (ciudadCell.typeUnitOnCell != TypeUnit.Ciudad && ciudadCell.typeUnitOnCell != TypeUnit.Poblado)
            {
                ciudadCell.typeUnitOnCell = nuevoColono.statsBase.nombreUnidad;
            }
        }

        jugador.ArmyManager.RegisterUnit(nuevoColono);
    }

    private Player GetOwnerPlayer(int ownerID)
    {
        // Asumiendo que 0 es Humano y 1 es IA (como en tu GameManager.CollectTurnResources)
        if (ownerID == 0)
        {
            return GameManager.Instance.humanPlayer;
        }
        else if (ownerID == 1) // O el ID que use tu IA
        {
            return GameManager.Instance.IAPlayer;
        }
        else
        {
            Debug.LogError($"UnitRecruiter: ownerID desconocido ({ownerID}). No se puede cobrar recursos.");
            return null;
        }
    }
}
