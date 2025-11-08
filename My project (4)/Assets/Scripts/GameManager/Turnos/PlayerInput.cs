using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Esta variable 'flag' controla si el script debe procesar clics
    private bool canPlayerAct = false;

    // --- Suscripción a Eventos ---

    void OnEnable()
    {
        // Se suscribe a los eventos del GameManager
        GameManager.OnPlayerTurnStart += EnableInput;
        GameManager.OnPlayerTurnEnd += DisableInput;
        GameManager.OnAITurnStart += DisableInput; // Deshabilitar durante el turno de la IA
    }

    void OnDisable()
    {
        // Se desuscribe para evitar errores
        GameManager.OnPlayerTurnStart -= EnableInput;
        GameManager.OnPlayerTurnEnd -= DisableInput;
        GameManager.OnAITurnStart -= DisableInput;
    }

    // --- Lógica de Input ---

    void Update()
    {
        // Si no es el turno del jugador, no hacer nada
        if (!canPlayerAct) return;

        // Si es el turno del jugador, comprobar si ha hecho clic
        if (Input.GetMouseButtonDown(0))
        {
            // IMPORTANTE: Comprobar si el clic fue sobre un elemento de UI (como un botón)
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // El clic fue en la UI, ignorarlo en el mundo del juego
            }

            // Si el clic fue en el mundo, procesarlo
            ProcessWorldClick();
        }
    }

    private void ProcessWorldClick()
    {
        Debug.Log("Clic en el mundo 3D detectado. Lanzando Raycast...");

        // Aquí va tu lógica de Raycast para detectar qué casilla, camino o unidad se ha pulsado
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // if (Physics.Raycast(ray, out RaycastHit hit))
        // {
        //     // Comprobar si 'hit.collider.gameObject' es una HexTile, RoadSpot, etc.
        // }
    }

    // --- Controladores de Eventos ---

    private void EnableInput()
    {
        canPlayerAct = true;
        Debug.Log("Input del jugador HABILITADO.");
    }

    private void DisableInput()
    {
        canPlayerAct = false;
        Debug.Log("Input del jugador DESHABILITADO.");
    }
}