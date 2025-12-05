using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para las Corutinas

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

        //Debug.Log($"Iniciando movimiento a {casillaDestino.name}. Movimientos restantes: {unitCerebro.movimientosRestantes}");

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

        // --- CORRECCIÓN: Actualizar ocupación en el BoardManager ---
        // 1. Liberar la casilla anterior
        CellData oldCell = BoardManager.Instance.GetCell(unitCerebro.misCoordenadasActuales);
        if (oldCell != null)
        {
            // --- FIX: Antes de borrar, comprobar si hay OTRA unidad (ej. Poblado/Ciudad) ---
            Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
            Unit otherUnit = null;
            foreach(var c in colliders)
            {
                Unit u = c.GetComponentInParent<Unit>();
                
                // Check if unit is moving via its movement component
                bool unitIsMoving = false;
                if(u != null)
                {
                     UnitMovement um = u.GetComponent<UnitMovement>();
                     if(um != null) unitIsMoving = um.isMoving;
                }

                if(u != null && u != unitCerebro && !unitIsMoving)
                {
                    // Asumimos que si hay otro, es el poblado/ciudad
                    otherUnit = u; 
                    break;
                }
            }

            if(otherUnit != null)
            {
                // Restauramos el poblado
                oldCell.unitOnCell = otherUnit;
                oldCell.typeUnitOnCell = otherUnit.statsBase.nombreUnidad;
            }
            else
            {
                // Si no hay nadie más, entonces sí limpiamos
                oldCell.unitOnCell = null;
                oldCell.typeUnitOnCell = TypeUnit.None;
            }
        }

        // 2. Ocupar la nueva casilla (para que nadie más entre mientras me muevo)
        if (cellLogica != null)
        {
            cellLogica.unitOnCell = unitCerebro;
            cellLogica.typeUnitOnCell = unitCerebro.statsBase.nombreUnidad;
        }
        // -----------------------------------------------------------

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

    /// <summary>
    /// NUEVO: Recibe una lista de coordenadas (Ruta) y mueve la unidad paso a paso.
    /// </summary>
    public void MoversePorRuta(List<Vector2Int> ruta)
    {
        if (isMoving) return;
        if (ruta == null || ruta.Count <= 1) return; // Ruta inválida o solo contiene el punto de inicio

        StartCoroutine(FollowPathCoroutine(ruta));
    }

    private IEnumerator FollowPathCoroutine(List<Vector2Int> ruta)
    {
        // Empezamos en índice 1 porque el 0 es donde ya estamos
        for (int i = 1; i < ruta.Count; i++)
        {
            // 1. Obtener la siguiente casilla visual
            CellData nextCell = BoardManager.Instance.GetCell(ruta[i]);

            if (nextCell != null && nextCell.visualTile != null)
            {
                // 2. Intentar mover a esa casilla adyacente
                // (Esto reutiliza tu lógica existente de gasto de puntos y validación)
                bool movio = IntentarMover(nextCell.visualTile);

                if (movio)
                {
                    // 3. Esperar a que termine la animación de este paso antes de dar el siguiente
                    // Como 'isMoving' se pone a true en IntentarMover y a false al acabar:
                    yield return new WaitWhile(() => isMoving);
                }
                else
                {
                    // Si falla un paso (ej. se acabaron los puntos), detenemos la ruta
                    Debug.Log("Ruta interrumpida (sin puntos o bloqueo).");
                    break;
                }
            }
        }
    }

}