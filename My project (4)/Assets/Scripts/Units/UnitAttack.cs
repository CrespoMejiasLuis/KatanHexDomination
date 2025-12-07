using UnityEngine;
[RequireComponent(typeof(Animator))]
public class UnitAttack : MonoBehaviour
{
    private Unit unitData;
    private Animator animator;
    private AudioSource audioSource;
    public AudioClip hitSound;

    void Awake()
    {
        unitData = GetComponent<Unit>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
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

        // --- ANIMACIÓN ---
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        int dano = unitData.statsBase.ataque;
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        objetivo.RecibirDano(dano);
        unitData.movimientosRestantes = 0;

        Debug.Log($"{unitData.statsBase.nombreUnidad} atacó a {objetivo.statsBase.nombreUnidad} e hizo {dano} de daño");
    }
}
