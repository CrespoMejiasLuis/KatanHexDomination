using UnityEngine;

public class PlayerHuman : Player
{
    /// <summary>
    /// Implementación del turno del jugador humano.
    /// </summary>
    public override void BeginTurn()
    {
        Debug.Log($"--- Turno del Jugador {playerName} (Humano) ---");

        // El GameManager será el responsable de activar el PlayerInput
        // y la UI cuando el estado cambie a PlayerTurn.
        // Esta función principalmente notifica que el turno ha comenzado.
    }

    // El jugador humano no necesita más lógica aquí.
    // La lógica de "qué hacer" (clics, etc.) se maneja en los scripts
    // 'PlayerInput' y 'UIManager', que reaccionan a los eventos del GameManager.
}