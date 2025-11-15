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

    public bool RecursosNecesarios(Unit unitPrefabToRecruit)
    {
        //1.Obtener referenccia al jugador
        Player activePlayer = GameManager.Instance.humanPlayer;

        if(activePlayer == null) return false;

        //2.Obtener estadisticas de unidad
        UnitStats stats = unitPrefabToRecruit.statsBase;

        if(stats == null)
        {
            Debug.Log($"No tiene unitStats el prefab {unitPrefabToRecruit.name}");
            return false;
        }

        Dictionary<ResourceType, int> productionCost = stats.GetProductCost();

        return activePlayer.CanAfford(productionCost);
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
        if (cellDondeEstamos.hasCity)
        {
            Debug.Log("¡Ya hay una ciudad en esta casilla!");
            return;
        }

        //Necesitamos el componente Unit del prefab
        Unit pobladoUnitPrefab = pobladoPrefab.GetComponent<Unit>();
        bool recursosNecesarios = RecursosNecesarios(pobladoUnitPrefab);
        if(!recursosNecesarios) return;
        
        //Gastar recursos
        Player jugador = GameManager.Instance.humanPlayer;
        Dictionary<ResourceType, int> productionCost = pobladoUnitPrefab.statsBase.GetProductCost();

        bool recursosGastados = jugador.SpendResources(productionCost);
        if(!recursosGastados) return; 

        // --- ¡ACCIÓN! ---
        isBuilding = true; 
        HexTile tileVisual = cellDondeEstamos.visualTile;
        
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
        }

        // 5. ACTUALIZAR EL BOARDMANAGER (¡LO MÁS IMPORTANTE!)
        // Esto es lo que hablaréis mañana, pero esta es la lógica:
        cellDondeEstamos.hasCity = false;
        cellDondeEstamos.owner = unitCerebro.ownerID;
        // Asumimos que el colono era la 'tropa' en esta casilla
        cellDondeEstamos.unitOnCell = unitCerebro.statsBase.nombreUnidad; 
        jugador.victoryPoints +=1;
        UIManager.Instance.UpdateVictoryPointsText(jugador.victoryPoints);

        // 6. CONSUMIR EL COLONO
        Destroy(gameObject);
    }
}