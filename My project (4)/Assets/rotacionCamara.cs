using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    // --- Configuración de Rotación y Posición ---
    [Header("1. Configuración de Rotación y Posición")]
    public float rotationDuration = 1.5f;
    public float playerYRotation = 0f;
    public float aiYRotation = 180f;
    public float aiZOffset = 14f; // Desplazamiento en la posición Z local del pivote

    private Quaternion playerRotation;
    private Quaternion aiRotation;
    private Vector3 playerPosition;
    private Vector3 aiPosition;

    private Transform pivotTransform; // Referencia al CameraPivot

    // --- Configuración de Zoom de Usuario ---
    [Header("2. Configuración de Zoom (Rueda del Ratón)")]
    public float zoomSpeed = 50f;
    public float minFOV = 30f; // El FOV mínimo será el FOV calculado, pero con un margen
    public float maxFOV = 90f; // Valor de inicio para el FOV máximo

    private Camera mainCamera;

    // --- Configuración de Transición de Turno ---
    [Header("3. Configuración de Transición de Turno")]
    public float normalFOV = 78f;      // Será sobrescrito por el cálculo dinámico
    public float transitionFOV = 100f; // FOV amplio durante la rotación
    public float fovAnimationDuration = 0.4f; // Duración de la animación del FOV

    // --- Configuración de Paneo ---
    [Header("4. Configuración de Paneo (Clic Derecho)")]
    public float panSpeed = 0.5f;
    public float panLimitX = 10f;
    public float panLimitZ = 10f;

    private Vector3 dragOrigin;

    // --- Referencia para el radio del tablero ---
    [Header("5. Referencias de Tablero")]
    private HexGridGenerator _gridGenerator; // Referencia al script del tablero
    // No necesitamos 'boardRadius' aquí, lo calculamos en Start.

    void Awake()
    {
        // Buscar el generador de la rejilla
        _gridGenerator = FindFirstObjectByType<HexGridGenerator>();
    }

    void Start()
    {
        // 1. Inicialización de rotaciones y posiciones
        playerRotation = Quaternion.Euler(0, playerYRotation, 0);
        aiRotation = Quaternion.Euler(0, aiYRotation, 0);

        playerPosition = Vector3.zero;
        aiPosition = new Vector3(0, 0, aiZOffset);

        // 2. OBTENCIÓN DE REFERENCIAS DE LA CÁMARA
        pivotTransform = transform.GetChild(0);
        if (pivotTransform != null && pivotTransform.childCount > 0 && pivotTransform.GetChild(0).childCount > 0)
        {
            mainCamera = pivotTransform.GetChild(0).GetChild(0).GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("No se pudo encontrar el componente Camera. Revisa la jerarquía.");
            return;
        }

        // 3. CÁLCULO DINÁMICO DEL FOV
        if (_gridGenerator != null)
        {
            // El radio del tablero (la longitud desde el centro hasta el borde)
            // es el número de anillos (boardRadius) * el tamaño de cada hexágono (hexRadius).
            // Sumamos hexRadius para incluir el borde exterior de la última fila de hexágonos.
            float boardSizeRadius = (_gridGenerator.boardRadius - 0.5f) * _gridGenerator.hexRadius * 2f;

            // Calculamos la distancia real D (hipotenusa) de la cámara al pivote
            Transform cameraTransform = mainCamera.transform;
            float cameraDistance = Mathf.Sqrt(
                cameraTransform.localPosition.y * cameraTransform.localPosition.y +
                cameraTransform.localPosition.z * cameraTransform.localPosition.z
            );

            if (cameraDistance > 0.01f) // Evitar división por cero
            {
                // Fórmula FOV (semi-ángulo)
                float requiredFOVRadians = 2f * Mathf.Atan(boardSizeRadius / cameraDistance);

                // Asignamos el resultado a normalFOV (se convierte a grados)
                normalFOV = requiredFOVRadians * Mathf.Rad2Deg;

                // Ajustamos el maxFOV para que el usuario pueda alejar un poco más (ej. 20 grados más)
                maxFOV = normalFOV + 20f;

                // Aseguramos que el minFOV no sea mayor que el normalFOV
                minFOV = Mathf.Min(minFOV, normalFOV - 10f);
            }
        }
        else
        {
            // Si el generador no se encuentra, usamos los valores por defecto del Inspector
            Debug.LogWarning("Usando FOV por defecto (78). No se encontró el HexGridGenerator.");
        }

        // 4. Inicializa al Jugador
        mainCamera.fieldOfView = normalFOV;
        pivotTransform.rotation = playerRotation;
        pivotTransform.localPosition = playerPosition;
    }

    // --- (El resto de los métodos se mantienen igual) ---

    void Update()
    {
        HandleZoom();
        HandlePanning();
    }

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

    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(1)) { dragOrigin = Input.mousePosition; return; }
        if (!Input.GetMouseButton(1)) return;
        Vector3 delta = Input.mousePosition - dragOrigin;
        Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -panLimitX, panLimitX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -panLimitZ, panLimitZ);
        transform.position = clampedPosition;
        dragOrigin = Input.mousePosition;
    }

    public void ChangePerspective(bool isPlayerTurn)
    {
        if (pivotTransform == null || mainCamera == null)
        {
            Debug.LogError("Las referencias pivotTransform o mainCamera no están asignadas.");
            return;
        }

        Quaternion targetRotation = isPlayerTurn ? playerRotation : aiRotation;
        Vector3 targetPosition = isPlayerTurn ? playerPosition : aiPosition;

        if (pivotTransform.rotation != targetRotation || pivotTransform.localPosition != targetPosition)
        {
            StopAllCoroutines();
            StartCoroutine(PerformTurnTransition(targetRotation, targetPosition));
        }
    }

    private IEnumerator PerformTurnTransition(Quaternion targetRotation, Vector3 targetPosition)
    {
        yield return StartCoroutine(AnimateFOV(transitionFOV, fovAnimationDuration));
        yield return StartCoroutine(TransitionPivot(targetRotation, targetPosition));
        yield return StartCoroutine(AnimateFOV(normalFOV, fovAnimationDuration));
        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, minFOV, maxFOV);
    }

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

    private IEnumerator TransitionPivot(Quaternion targetRotation, Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Quaternion startRotation = pivotTransform.rotation;
        Vector3 startPosition = pivotTransform.localPosition;

        while (elapsedTime < rotationDuration)
        {
            float t = elapsedTime / rotationDuration;
            pivotTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            pivotTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pivotTransform.rotation = targetRotation;
        pivotTransform.localPosition = targetPosition;
    }
}