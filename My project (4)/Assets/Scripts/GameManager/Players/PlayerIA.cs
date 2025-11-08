using UnityEngine;
using System.Collections; // Necesario para Coroutines

public class PlayerIA : Player
{
    /// <summary>
    /// Implementación del turno del jugador IA.
    /// </summary>
    public override void BeginTurn()
    {
        Debug.Log($"--- Turno del Jugador {playerName} (IA) ---");

        // Inicia la corutina que tomará las decisiones
        StartCoroutine(ExecuteAITurn());
    }

    /// <summary>
    /// Corutina que simula el proceso de pensamiento y acción de la IA.
    /// </summary>
    private IEnumerator ExecuteAITurn()
    {
        // 1. PENSAR
        Debug.Log("IA está 'pensando'...");
        // Aquí iría la lógica compleja de decisión:
        // - ¿Qué recursos tengo? (accede a 'this.resources')
        // - ¿Dónde puedo construir?
        // - ¿Debo atacar?

        // Simula un tiempo de pensamiento
        yield return new WaitForSeconds(1.5f);

        // 2. ACTUAR (Ejemplo de acción)
        // Ejemplo: Si tengo 2 de madera, construyo algo.
        if (HasEnoughResources(HexTile.ResourceType.Madera, 2))
        {
            SpendResources(HexTile.ResourceType.Madera, 2);
            Debug.Log("IA ha decidido construir algo.");
            // Lógica para instanciar un edificio...
        }
        else
        {
            Debug.Log("IA no tiene suficientes recursos, pasa el turno.");
        }

        yield return new WaitForSeconds(1.0f); // Pausa para ver la acción

        // 3. TERMINAR TURNO
        Debug.Log("IA termina su turno.");
        GameManager.Instance.EndPlayerTurn(); // Llama al GameManager para pasar el turno
    }
}