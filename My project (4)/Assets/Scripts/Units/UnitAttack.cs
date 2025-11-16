using UnityEngine;
using System.Collections; // Para la corutina

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Animator))]
public class UnitAttack : MonoBehaviour
{
    private Unit unitData;
    private Animator animator;

    [Header("Configuración de ataque")]
    [Tooltip("Tiempo que dura la animación de ataque en segundos")]
    public float attackDuration = 0.5f;

    void Awake()
    {
        unitData = GetComponent<Unit>();
        animator = GetComponent<Animator>();
    }

    public bool PuedeAtacar(Unit objetivo)
    {
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
            Debug.Log("Objetivo fuera de rango");
            return;
        }

        // Inicia la corutina de ataque para animación y daño
        StartCoroutine(AtacarCoroutine(objetivo));
    }

    private IEnumerator AtacarCoroutine(Unit objetivo)
    {
        if (animator != null)
        {
            // Activamos el bool isAttacking
            animator.SetBool("isAttacking", true);
        }

        // Esperar la mitad del tiempo para sincronizar el golpe
        yield return new WaitForSeconds(attackDuration / 2f);

        // Aplicar daño
        int dano = unitData.statsBase.ataque;
        objetivo.RecibirDano(dano);
        Debug.Log($"{unitData.statsBase.nombreUnidad} atacó a {objetivo.statsBase.nombreUnidad} e hizo {dano} de daño");

        // Esperar a que termine la animación
        yield return new WaitForSeconds(attackDuration / 2f);

        // Desactivar el bool isAttacking para volver a Idle
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
    }
}
