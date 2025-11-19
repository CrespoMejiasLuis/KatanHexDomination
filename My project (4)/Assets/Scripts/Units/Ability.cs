using UnityEngine;

[RequireComponent(typeof(Unit))]
public class Ability : MonoBehaviour
{
    private Unit unitData;
    private readonly int PLAYER_ID = 0;

    void Awake()
    {
        unitData = GetComponent<Unit>();
    }

    /// <summary>
    /// Saquear: rompe la casilla objetivo, impide recursos 1 turno y da 1 recurso al jugador.
    /// </summary>
    public void Saquear(CellData targetCell)
    {
        if (unitData.movimientosRestantes <= 0)
        {
            Debug.Log("No tienes puntos de movimiento para saquear.");
            return;
        }

        if (targetCell == null)
        {
            Debug.Log("No hay casilla seleccionada para saquear.");
            return;
        }
        if (targetCell.owner == PLAYER_ID)
        {
            Debug.Log("No puedes saquear a tu pueblo");
            return;
        }

        // Marcar la casilla como saqueada (impide recursos 1 turno)
        targetCell.isRaided = true; // Necesitas agregar este bool en CellData

        // Dar un recurso al jugador
        Player jugador = GameManager.Instance.humanPlayer; // asumir jugador humano
        jugador.AddResource(targetCell.resource, 1);

        // Gastar un punto de movimiento
        unitData.GastarPuntoDeMovimiento(targetCell.cost);

        Debug.Log($"Saqueaste la casilla {targetCell.coordinates} y ganaste 1 {targetCell.resource}");
    }
}