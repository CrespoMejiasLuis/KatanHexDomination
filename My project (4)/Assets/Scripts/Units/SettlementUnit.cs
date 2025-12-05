using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Unit))]
public class SettlementUnit : MonoBehaviour
{
    [Header("Componentes de poblado")]
    public GameObject tradeMenu;

    [HideInInspector]public Unit unitCerebro;

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
        //como no se tiene que mover destruimos el script de movimiento
        //Destroy(GetComponent<UnitMovement>());

        if (UIManager.Instance != null)
        {
            tradeMenu = UIManager.Instance.constructionPanelContainer;
        }
    }
    public Unit getUnitCerebro() {  return unitCerebro; }

    public void RecibirDano(int cantidad, Unit atacante)
    {
        unitCerebro.RecibirDano(cantidad); //llamar logica de restar vida

        if(unitCerebro.vidaActual > 0 && unitCerebro.statsBase.ataque >0 && atacante!=null)
        {
            if(atacante.statsBase.nombreUnidad == TypeUnit.Artillero) return;
            int danoContraataque = unitCerebro.statsBase.ataque;

            atacante.RecibirDano(danoContraataque);
        }
    }

    // Nueva lógica para restaurar la casilla al morir
    void Start()
    {
        if (unitCerebro != null)
        {
            unitCerebro.OnUnitDied += OnSettlementDied;
        }
    }

    void OnDestroy()
    {
        if (unitCerebro != null)
        {
            unitCerebro.OnUnitDied -= OnSettlementDied;
        }
    }

    private void OnSettlementDied(Unit unit)
    {
        // 1. Recuperar la celda donde estaba
        if (BoardManager.Instance == null) return;
        
        CellData cell = BoardManager.Instance.GetCell(unit.misCoordenadasActuales);
        if (cell != null)
        {
            // 2. Restaurar datos lógicos de la celda central
            cell.unitOnCell = null;
            cell.typeUnitOnCell = TypeUnit.None;
            cell.owner = -1; // Vuelve a ser neutral

            // 3. Restaurar visuales (Reactiva el terreno original)
            if (cell.visualTile != null)
            {
                foreach (Renderer r in cell.visualTile.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
                cell.UpdateVisual();
            }

            // 4. (NUEVO) Liberar territorio adyacente
            // Obtenemos las celdas adyacentes usando el BoardManager
            List<CellData> adjacents = BoardManager.Instance.GetAdjacents(unit.misCoordenadasActuales);
            
            foreach (CellData adj in adjacents)
            {
                // Si la casilla adyacente tiene OTRA ciudad o poblado, NO la des-reclamamos
                if (adj.typeUnitOnCell == TypeUnit.Ciudad || adj.typeUnitOnCell == TypeUnit.Poblado)
                    continue;

                // Solo des-reclamamos si pertenece al dueño de este poblado (para no borrar fronteras enemigas si hubiera overlap)
                if (adj.owner == unit.ownerID)
                {
                    adj.owner = -1;
                    adj.UpdateVisual();
                }
            }
        }
        
        // Actualizar fronteras globales por si acaso
        BoardManager.Instance.UpdateAllBorders();
    }

    public void OpenTradeMenu()
    {
        if(tradeMenu !=null)
        {
            tradeMenu.SetActive(true);
        }
    }
}
