using UnityEngine;
using System.Collections; // Necesario para las Corutinas

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Animator))] 
public class UnitMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 5f; 
    
    // --- ¡NUEVA VARIABLE! ---
    [Tooltip("Velocidad de giro de la unidad al moverse.")]
    public float rotationSpeed = 10f; // Velocidad de rotación

    // --- REFERENCIAS INTERNAS ---
    private Unit unitCerebro;
    private Animator animator;

    // --- ESTADO PRIVADO ---
    public bool isMoving = false; 

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
        animator = GetComponent<Animator>(); 
    }

    public bool IntentarMover(HexTile casillaDestino)
    {
        if (isMoving)
        {
            Debug.Log("¡Ya estoy en movimiento!");
            return false;
        }

        CellData cellDestino = GetCellDataFromTile(casillaDestino);
        if(cellDestino== null)
        {
            Debug.Log("No existe esta casilla o no se pudo encontrar");
            return false;
        }

        int costeMovimiento = cellDestino.cost;

        if (unitCerebro.movimientosRestantes < costeMovimiento)
        {
            Debug.Log("¡No quedan puntos de movimiento!");
            return false;
        }

        // Comprobar si ya estamos en esa casilla (evita gastar turno en moverse a la misma casilla)
        if (Vector3.Distance(transform.position, casillaDestino.transform.position) < 0.1f)
        {
            Debug.Log("Ya estás en esta casilla.");
            return false;
        }

        // 3. (FUTURO) Comprobar adyacencia, etc.
        Vector2Int coordActual = unitCerebro.misCoordenadasActuales;
        Vector2Int coordDest = cellDestino.coordinates;

        bool isAdyacent = IsAdyacent(coordActual, coordDest);

        if(!isAdyacent)
        {
            Debug.Log("solo te puedes mover a una casilla adyacente");
            return false;
        }

        // 4. (FUTURO) Comprobar si la casilla está ocupada
        if (cellDestino.unitOnCell != null) return false;

        // 5. ¡Todo OK! Gastar el recurso e iniciar el movimiento
        unitCerebro.GastarPuntoDeMovimiento(costeMovimiento); 

        Debug.Log($"Iniciando movimiento a {casillaDestino.name}. Movimientos restantes: {unitCerebro.movimientosRestantes}");

        StartCoroutine(MoveCoroutine(casillaDestino));

        return true;
    }

    private bool IsAdyacent(Vector2Int coordIA, Vector2Int coordB)
    {
        Vector2Int delta = coordB-coordIA;

        //delta es la direction hacia una direccion

        foreach(var direction in GameManager.axialNeighborDirections)
        {
            if(delta == direction)
            {
                return true;
            }
        }

        return false;
    }

    // CORUTINA: Se encarga de mover Y ROTAR el transform suavemente y gestionar las animaciones.
    private IEnumerator MoveCoroutine(HexTile casillaDestino)
    {
        CellData cellLogica = GetCellDataFromTile(casillaDestino);
        BoardManager.Instance.HideAllBorders();                // Quita bordes antiguos
        if (cellLogica != null && cellLogica.visualTile != null)
        {
            cellLogica.visualTile.SetBorderVisible(true);      // Resalta la nueva posición
        }
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

        if (cellLogica != null)
        {
            // ¡Encontrada! Actualizamos el cerebro con las coordenadas CORRECTAS
            unitCerebro.misCoordenadasActuales = cellLogica.coordinates;
            // Debug.Log($"Movimiento completado. Nuevas coordenadas lógicas: {unitCerebro.misCoordenadasActuales}");
        }
        else
        {
            // Esto solo pasaría si el enlace 'visualTile' en CellData falló
            Debug.LogError("¡No se pudo encontrar la CellData para este HexTile! " + casillaDestino.name);
        }
        // --- FIN DE LÓGICA NUEVA ---
       

        isMoving = false;
    }
    
    private CellData GetCellDataFromTile(HexTile targetTile)
    {
        // 1. Comprobaciones de seguridad
        if (targetTile == null) return null;
        if (BoardManager.Instance == null || BoardManager.Instance.gridData == null)
        {
            Debug.LogError("BoardManager o gridData no están listos.");
            return null;
        }

        // 2. Recorremos toda la cuadrícula de datos lógicos
        // (gridData es un array 2D, así que 'foreach' es la forma más fácil)
        foreach (CellData cell in BoardManager.Instance.gridData)
        {
            // 3. Si la celda existe, tiene un visualTile asignado,
            //    y ese visualTile es EXACTAMENTE el mismo que nuestra casillaDestino...
            if (cell != null && cell.visualTile != null && cell.visualTile == targetTile)
            {
                return cell; // ¡La hemos encontrado! Devolvemos la CellData.
            }
        }

        // 4. Si el bucle termina, no se encontró ninguna coincidencia
        return null; 
    }

}