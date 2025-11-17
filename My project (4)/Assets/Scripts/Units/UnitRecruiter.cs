using UnityEngine;
using System.Collections.Generic;

public class UnitRecruiter : MonoBehaviour
{
    [Header("Prefabs de unidades")]
    public GameObject artilleroPrefab;
    public GameObject caballeroPrefab;
    public GameObject colonoPrefab;   // ← AÑADIDO

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

        Player jugador = GameManager.Instance.humanPlayer;
        Dictionary<ResourceType, int> coste = artilleroUnitPrefab.statsBase.GetProductCost();

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
        }
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

        Player jugador = GameManager.Instance.humanPlayer;
        Dictionary<ResourceType, int> coste = caballeroUnitPrefab.statsBase.GetProductCost();

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
        }
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

        Player jugador = GameManager.Instance.humanPlayer;
        Dictionary<ResourceType, int> coste = colonoUnitPrefab.statsBase.GetProductCost();

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
        }
    }
}
