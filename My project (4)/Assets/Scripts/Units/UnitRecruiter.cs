using UnityEngine;
using System.Collections.Generic;

public class UnitRecruiter : MonoBehaviour
{
    [Header("Prefabs de unidades")]
    public GameObject artilleroPrefab;

    /// <summary>
    /// Intenta construir un Artillero en la celda de la unidad creadora (normalmente una ciudad/poblado).
    /// </summary>
    public void ConstruirArtillero(Unit unidadCreadora)
    {
        if (artilleroPrefab == null)
        {
            Debug.LogError("⚠️ No hay prefab de Artillero asignado.");
            return;
        }

        // 1. Obtener las coordenadas de la unidad creadora (la ciudad/poblado)
        Vector2Int ciudadCoords = unidadCreadora.misCoordenadasActuales;

        // 2. Obtener la celda correcta del tablero
        CellData ciudadCell = BoardManager.Instance.GetCell(ciudadCoords);
        if (ciudadCell == null)
        {
            Debug.LogError("⚠️ Celda de la ciudad/poblado inválida.");
            return;
        }

        // 3. Obtener stats del prefab
        Unit artilleroUnitPrefab = artilleroPrefab.GetComponent<Unit>();
        if (artilleroUnitPrefab.statsBase == null)
        {
            Debug.LogError("⚠️ El Artillero no tiene UnitStats asignado.");
            return;
        }

        // 4. Verificar recursos
        Player jugador = GameManager.Instance.humanPlayer; // o unidadCreadora.ownerID
        Dictionary<ResourceType, int> coste = artilleroUnitPrefab.statsBase.GetProductCost();

        if (!jugador.CanAfford(coste))
        {
            Debug.Log("No hay recursos suficientes para construir Artillero.");
            return;
        }

        // 5. Gastar recursos
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

        Debug.Log("Artillero creado en la celda " + unidadCreadora.misCoordenadasActuales);
    }
}
