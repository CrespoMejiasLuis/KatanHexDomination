using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Unit))]
public class SettlementUnit : MonoBehaviour
{
    [Header("Componentes de poblado")]
    public GameObject tradeMenu;

    private Unit unitCerebro;

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
        //como no se tiene que mover destruimos el script de movimiento
        Destroy(GetComponent<UnitMovement>());

        if (UIManager.Instance != null)
        {
            tradeMenu = UIManager.Instance.constructionPanelContainer;
        }
    }

    public void RecibirDano(int cantidad, Unit atacante)
    {
        unitCerebro.RecibirDano(cantidad); //llamar logica de restar vida

        if(unitCerebro.vidaActual > 0 && unitCerebro.statsBase.ataque >0 && atacante!=null)
        {
            int danoContraataque = unitCerebro.statsBase.ataque;

            atacante.RecibirDano(danoContraataque);
        }
    }

    public void OpenTradeMenu()
    {
        if(tradeMenu !=null)
        {
            tradeMenu.SetActive(true);
        }
    }

    public Unit getUnitCerebro()
    {
        return unitCerebro;
    }

}
