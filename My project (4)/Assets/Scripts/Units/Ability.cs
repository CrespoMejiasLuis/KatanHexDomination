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

        if (targetCell.owner == -1)
        {
            Debug.Log("No puedes saquear territorio neutral.");
            return;
        }

        if (targetCell.isRaided)
        {
            Debug.Log("Esta casilla ya ha sido saqueada recientemente.");
            return;
        }

        // Marcar la casilla como saqueada (impide recursos 1 turno)
        targetCell.isRaided = true; 
        targetCell.lootedCooldown = 2; // Dura 2 fases (Turno Jugador + Turno IA) para bloquear producción
        targetCell.UpdateVisual();

        // Dar un recurso al jugador correcto (el dueño de la unidad que saquea)
        Player jugador = GameManager.Instance.GetPlayer(unitData.ownerID);
        if (jugador == null)
        {
            Debug.LogError($"No se pudo obtener el jugador para ownerID {unitData.ownerID}");
            return;
        }
        
        // Solo dar recurso si NO es desierto
        if (targetCell.resource != ResourceType.Desierto)
        {
            jugador.AddResource(targetCell.resource, 1);
            Debug.Log($"Saqueaste la casilla {targetCell.coordinates} y ganaste 1 {targetCell.resource}");
        }
        else
        {
            Debug.Log($"Saqueaste la casilla {targetCell.coordinates} pero es Desierto (0 recursos).");
        }

        // Gastar un punto de movimiento
        unitData.GastarPuntoDeMovimiento(targetCell.cost);

        Debug.Log($"Saqueaste la casilla {targetCell.coordinates} y ganaste 1 {targetCell.resource}");
    }
}