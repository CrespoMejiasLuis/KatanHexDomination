// 📁 UnitMovement.cs (VERSIÓN 2.1 - Con rotación)
using UnityEngine;
using System.Collections; // Necesario para las Corutinas

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Animator))] 
public class UnitMovement : MonoBehaviour
{
    // --- VARIABLES PÚBLICAS (Ajustables en el Inspector) ---
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 5f; 
    
    // --- ¡NUEVA VARIABLE! ---
    [Tooltip("Velocidad de giro de la unidad al moverse.")]
    public float rotationSpeed = 10f; // Velocidad de rotación

    // --- REFERENCIAS INTERNAS ---
    private Unit unitCerebro;
    private Animator animator;

    // --- ESTADO PRIVADO ---
    private bool isMoving = false; 

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
        animator = GetComponent<Animator>(); 
    }

    /// <summary>
    /// Comprueba si el movimiento es válido y, si lo es, inicia la Corutina de movimiento.
    /// </summary>
    public bool IntentarMover(HexTile casillaDestino)
    {
        if (isMoving)
        {
            Debug.Log("¡Ya estoy en movimiento!");
            return false;
        }

        if (unitCerebro.movimientosRestantes <= 0)
        {
            Debug.Log("¡No quedan puntos de movimiento!");
            return false;
        }

        // Añadido: Comprobar si ya estamos en esa casilla (evita gastar turno en moverse a la misma casilla)
        if (Vector3.Distance(transform.position, casillaDestino.transform.position) < 0.1f)
        {
            Debug.Log("Ya estás en esta casilla.");
            return false;
        }

        // 3. (FUTURO) Comprobar adyacencia, etc.
        // 4. (FUTURO) Comprobar si la casilla está ocupada

        // 5. ¡Todo OK! Gastar el recurso e iniciar el movimiento
        unitCerebro.GastarPuntoDeMovimiento(); 

        Debug.Log($"Iniciando movimiento a {casillaDestino.name}. Movimientos restantes: {unitCerebro.movimientosRestantes}");

        StartCoroutine(MoveCoroutine(casillaDestino));

        return true;
    }

    /// <summary>
    /// CORUTINA: Se encarga de mover Y ROTAR el transform suavemente y gestionar las animaciones.
    /// </summary>
    private IEnumerator MoveCoroutine(HexTile casillaDestino)
    {
        // --- INICIO DEL MOVIMIENTO ---
        isMoving = true; 
        animator.SetBool("isWalking", true); 

        Vector3 targetPosition = casillaDestino.transform.position;

        // --- BUCLE DE MOVIMIENTO ---
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // --- LÓGICA DE ROTACIÓN (NUEVA) ---
            // 1. Calcular el vector de dirección hacia el objetivo
            Vector3 direction = (targetPosition - transform.position).normalized;

            // 2. Asegurarse de que el vector de dirección no tenga componente Y (para evitar que la unidad se incline)
            direction.y = 0; 

            // 3. Si la dirección no es cero (es decir, no estamos ya en el destino)
            if (direction != Vector3.zero)
            {
                // 4. Calcular la rotación necesaria para mirar en esa dirección
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 5. Interpolar suavemente desde la rotación actual a la rotación objetivo
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
            
            // --- LÓGICA DE MOVIMIENTO (Existente) ---
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime 
            );
            
            yield return null; // Pausar la función hasta el siguiente frame
        }

        // --- FIN DEL MOVIMIENTO ---
        transform.position = targetPosition; 
        animator.SetBool("isWalking", false);
        isMoving = false; 
    }
}