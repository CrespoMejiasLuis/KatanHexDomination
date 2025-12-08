// 📁 UnitBuilder.cs (VERSIÓN 5.0 - Lógica de Juego Completa)
using UnityEngine;
using System.Collections.Generic;
// (Ya no necesitamos Corutinas, la animación está en el prefab)

[RequireComponent(typeof(Unit))]
public class UnitBuilder : MonoBehaviour
{
    [Header("Configuración de Construcción")]
    [Tooltip("Arrastra aquí el Prefab de tu 'Poblado' (que ya tiene Unit.cs)")]
    public GameObject pobladoPrefab; 

    private Unit unitCerebro;
    private bool isBuilding = false;

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
    }

    public void IntentarConstruirPoblado()
    {
        if (isBuilding) return; 
        if (pobladoPrefab == null) { /* ... error ... */ return; }
        if (unitCerebro.ownerID == -1)
        {
            Debug.LogError("¡Este Colono no tiene dueño (ownerID)! No puede construir.");
            return;
        }

        // 1. OBTENER DATOS DE LA CASILLA ACTUAL
        CellData cellDondeEstamos = BoardManager.Instance.GetCell(unitCerebro.misCoordenadasActuales);
        if (cellDondeEstamos == null) { /* ... error ... */ return; }
        if (cellDondeEstamos.typeUnitOnCell == TypeUnit.Ciudad|| cellDondeEstamos.typeUnitOnCell == TypeUnit.Poblado)
        {
            Debug.Log("¡Ya hay una ciudad en esta casilla!");
            return;
        }

        if(cellDondeEstamos.owner != -1 ) return; 

        //Necesitamos el componente Unit del prefab
        Unit pobladoUnitPrefab = pobladoPrefab.GetComponent<Unit>();
        bool recursosNecesarios = unitCerebro.RecursosNecesarios(pobladoUnitPrefab);

        if (!recursosNecesarios)
        {
            Debug.Log("No tiene recursos necesarios para poblado");
            return;
        } 

        //Gastar recursos
        Player jugador = (unitCerebro.ownerID == 1) ? GameManager.Instance.IAPlayer : GameManager.Instance.humanPlayer;
        Dictionary<ResourceType, int> productionCost = pobladoUnitPrefab.statsBase.GetProductCost();

        // 🔧 FIX: Coste de poblados SIN incremento lineal
        // El coste se mantiene constante en el valor base
        // ELIMINADO: Escalado que impedía expansión
        // if(jugador.numPoblados > 1)
        // {
        //     productionCost = pobladoUnitPrefab.actualizarCostes(productionCost, jugador);
        // }

        bool recursosGastados = jugador.SpendResources(productionCost);
        if(!recursosGastados) return; 

        // --- ¡ACCIÓN! ---
        isBuilding = true; 
        HexTile tileVisual = cellDondeEstamos.visualTile;

        if (tileVisual == null)
        {
            Debug.LogError($"Error: La CellData en {cellDondeEstamos.coordinates} no tiene referencia a la HexTile visual. Construcción cancelada.");
            return;
        }

        // 2. OCULTAR LA CASILLA VIEJA
        // Desactiva todos los Renderers (modelos 3D) de la casilla de terreno
        foreach (Renderer r in tileVisual.GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
        
        // 3. CREAR EL POBLADO NUEVO
        GameObject nuevoPobladoGO = Instantiate(
            pobladoPrefab, 
            tileVisual.transform.position, 
            Quaternion.identity
        );
        
        // 4. ASIGNAR DUEÑO AL NUEVO POBLADO
        // Le pasamos la propiedad del Colono al nuevo Poblado
        Unit pobladoUnit = nuevoPobladoGO.GetComponent<Unit>();
        if (pobladoUnit != null)
        {
            pobladoUnit.ownerID = unitCerebro.ownerID;
            jugador.ArmyManager.RegisterUnit(pobladoUnit);
            //jugador.ArmyManager.RegisterUnit(pobladoUnit);

            if (pobladoUnit.statsBase.buildSound != null)
            {
                AudioSource.PlayClipAtPoint(pobladoUnit.statsBase.buildSound, Camera.main.transform.position);
            }
        }

        // 5. ACTUALIZAR EL BOARDMANAGER 
        pobladoUnit.misCoordenadasActuales = cellDondeEstamos.coordinates;
        claimTerritory(cellDondeEstamos.coordinates, unitCerebro.ownerID);
        // Asumimos que el colono era la 'tropa' en esta casilla
        cellDondeEstamos.typeUnitOnCell = TypeUnit.Poblado;
        cellDondeEstamos.unitOnCell = pobladoUnit;
        jugador.AddVictoryPoints(1);
        // UIManager.Instance.UpdateVictoryPointsText(jugador.victoryPoints);
        BoardManager.Instance.UpdateAllBorders();
        
        Debug.Log($"[OK] Poblado construido exitosamente en {cellDondeEstamos.coordinates}");
        // 6. CONSUMIR EL COLONO
        jugador.ArmyManager.DeregisterUnit(unitCerebro);
        Destroy(gameObject);
    }

    public void claimTerritory(Vector2Int settlementCoords, int newOwnerID)
    {
        List<Vector2Int> coordToClaim = new List<Vector2Int>();

        //1.casilla central
        coordToClaim.Add(settlementCoords);

        //2.add las de alrededor
        foreach(Vector2Int dir in GameManager.axialNeighborDirections)
        {
            coordToClaim.Add(settlementCoords+dir);
        }

        //3.iterar el boardmanager y los visuales
        foreach(Vector2Int coord in coordToClaim)
        {
            CellData cell = BoardManager.Instance.GetCell(coord);

            if (cell == null) continue;

            if(cell.owner == -1)
            {
                cell.owner = newOwnerID;
                cell.UpdateVisual();
            }
        }
    }
}