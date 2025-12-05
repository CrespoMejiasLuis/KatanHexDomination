using UnityEngine;
[RequireComponent(typeof(Animator))]
public class UnitAttack : MonoBehaviour
{
    private Unit unitData;


    void Awake()
    {
        unitData = GetComponent<Unit>();
    }

    public bool PuedeAtacar(Unit objetivo)
    {
        if (unitData.movimientosRestantes <= 0) return false;
        if (objetivo == null) return false;

        int distancia = BoardManager.Instance.Distance(
            unitData.misCoordenadasActuales,
            objetivo.misCoordenadasActuales
        );

        return distancia <= unitData.statsBase.rangoAtaque;

    }

    public void Atacar(Unit objetivo)
    {
        if (!PuedeAtacar(objetivo))
        {
            Debug.Log("Objetivo fuera de rango o sin movimientos");
            return;
        }

        int dano = unitData.statsBase.ataque;
        objetivo.RecibirDano(dano);
        unitData.movimientosRestantes = 0;

        Debug.Log($"{unitData.statsBase.nombreUnidad} atac� a {objetivo.statsBase.nombreUnidad} e hizo {dano} de da�o");
    }
}
