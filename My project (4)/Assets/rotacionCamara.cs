using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    // --- Configuración de Rotación y Posición ---
    [Header("1. Configuración de Rotación y Posición")]
    public float rotationDuration = 1.5f;
    public float playerYRotation = 0f;
    public float aiYRotation = 180f;
    public float aiZOffset = 14f; // Desplazamiento en la posición Z local del pivote (el CameraPivot)

    private Quaternion playerRotation;
    private Quaternion aiRotation;
    private Vector3 playerPosition;
    private Vector3 aiPosition;

    private Transform pivotTransform; // Referencia al CameraPivot

    // --- Configuración de Zoom de Usuario ---
    [Header("2. Configuración de Zoom (Rueda del Ratón)")]
    public float zoomSpeed = 50f;
    public float minFOV = 30f; // Zoom más cercano que el usuario puede alcanzar
    public float maxFOV = 90f; // Zoom más lejano que el usuario puede alcanzar

    private Camera mainCamera;

    // --- Configuración de Transición de Turno ---
    [Header("3. Configuración de Transición de Turno")]
    public float normalFOV = 78f;      // FOV base al que debe volver
    public float transitionFOV = 100f; // FOV amplio durante la rotación
    public float fovAnimationDuration = 0.4f; // Duración de la animación del FOV

    // --- Configuración de Paneo ---
    [Header("4. Configuración de Paneo (Clic Derecho)")]
    public float panSpeed = 0.5f;
    public float panLimitX = 10f;
    public float panLimitZ = 10f;

    private Vector3 dragOrigin;

    void Start()
    {
        // Inicializa Rotaciones (solo Y)
        playerRotation = Quaternion.Euler(0, playerYRotation, 0);
        aiRotation = Quaternion.Euler(0, aiYRotation, 0);

        // Inicializa Posiciones Locales del Pivote
        playerPosition = Vector3.zero; // Posición base del jugador (normalmente 0,0,0)
        // La posición de la IA es el offset en Z (ej: 0, 0, 14)
        aiPosition = new Vector3(0, 0, aiZOffset);

        // OBTENER PIVOT (Hijo 0 del PanController)
        pivotTransform = transform.GetChild(0);

        // OBTENER CÁMARA (CameraPivot -> ZoomController -> Main Camera)
        if (pivotTransform != null && pivotTransform.childCount > 0 && pivotTransform.GetChild(0).childCount > 0)
        {
            mainCamera = pivotTransform.GetChild(0).GetChild(0).GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("No se pudo encontrar el componente Camera. Revisa la jerarquía.");
            return;
        }

        // Inicializa al Jugador
        mainCamera.fieldOfView = normalFOV;
        pivotTransform.rotation = playerRotation;
        pivotTransform.localPosition = playerPosition; // Asegura la posición inicial
    }

    void Update()
    {
        HandleZoom();
        HandlePanning();
    }

    // ===============================================
    //               ZOOM DE USUARIO (FOV)
    // ===============================================

    private void HandleZoom()
    {
        if (mainCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            float newFOV = mainCamera.fieldOfView - (scroll * zoomSpeed * Time.deltaTime);
            mainCamera.fieldOfView = Mathf.Clamp(newFOV, minFOV, maxFOV);
        }
    }

    // ===============================================
    //          PANEO (Clic derecho y arrastrar)
    // ===============================================

    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(1)) return;

        Vector3 delta = Input.mousePosition - dragOrigin;
        // Mueve el PanController (este objeto)
        Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;

        transform.Translate(move, Space.World);

        // Limitar el paneo global
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -panLimitX, panLimitX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -panLimitZ, panLimitZ);
        transform.position = clampedPosition;

        dragOrigin = Input.mousePosition;
    }

    // ===============================================
    //         ROTACIÓN Y TRANSICIÓN DE TURNO
    // ===============================================

    public void ChangePerspective(bool isPlayerTurn)
    {
        if (pivotTransform == null || mainCamera == null)
        {
            Debug.LogError("Las referencias pivotTransform o mainCamera no están asignadas.");
            return;
        }

        Quaternion targetRotation = isPlayerTurn ? playerRotation : aiRotation;
        Vector3 targetPosition = isPlayerTurn ? playerPosition : aiPosition;

        // Comprobar si hay que hacer transición
        if (pivotTransform.rotation != targetRotation || pivotTransform.localPosition != targetPosition)
        {
            StopAllCoroutines();
            StartCoroutine(PerformTurnTransition(targetRotation, targetPosition));
        }
    }

    /// <summary>
    /// Corrutina Maestra que gestiona la secuencia de transición.
    /// </summary>
    private IEnumerator PerformTurnTransition(Quaternion targetRotation, Vector3 targetPosition)
    {
        // 1. Ampliar FOV (Zoom Out)
        yield return StartCoroutine(AnimateFOV(transitionFOV, fovAnimationDuration));

        // 2. Rotar y Mover el Pivote
        yield return StartCoroutine(TransitionPivot(targetRotation, targetPosition));

        // 3. Restaurar FOV (Zoom In/Reset)
        yield return StartCoroutine(AnimateFOV(normalFOV, fovAnimationDuration));

        // 4. Asegurar el límite de zoom después de la transición
        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, minFOV, maxFOV);
    }

    /// <summary>
    /// Anima el cambio de Field of View (FOV) de la cámara.
    /// </summary>
    private IEnumerator AnimateFOV(float targetFOV, float duration)
    {
        float elapsedTime = 0f;
        float startFOV = mainCamera.fieldOfView;

        while (elapsedTime < duration)
        {
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        mainCamera.fieldOfView = targetFOV;
    }

    /// <summary>
    /// Transiciona el Pivote de la cámara, interpolando Rotación (Y) y Posición (Z offset).
    /// </summary>
    private IEnumerator TransitionPivot(Quaternion targetRotation, Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Quaternion startRotation = pivotTransform.rotation;
        Vector3 startPosition = pivotTransform.localPosition; // Usamos localPosition

        while (elapsedTime < rotationDuration)
        {
            float t = elapsedTime / rotationDuration;

            // Interpolación de Rotación (Y)
            pivotTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Interpolación de Posición (Z offset)
            pivotTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asegura el destino final exacto
        pivotTransform.rotation = targetRotation;
        pivotTransform.localPosition = targetPosition;
    }
}