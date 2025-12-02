using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Propiedad del Recurso")]
    public ResourceType resourceType;

    [HideInInspector] public Vector2Int AxialCoordinates;
    
    [Header("Componentes Visuales")]
    [Tooltip("Arrastra aquí el objeto padre que contiene todos los cubos del borde ('bordes')")]
    public GameObject borderObject; 

    // Referencia al Animator
    private Animator animator;
    private const string FLIP_ANIMATION_NAME = "TileFlip";
    private List<Renderer> borderRenderers = new List<Renderer>();
    // Array para guardar todos los renderers de los trozos de borde
    private Renderer[] allBorderRenderers;
    public GameObject[] borderSegments;
    void Awake()
    {
        animator = GetComponent<Animator>();

        // CONFIGURACIÓN AUTOMÁTICA DE BORDES
        if (borderObject != null)
        {
            // 1. Buscamos TODOS los renderers en los hijos (Cube 1, Cube 2, etc.)
            allBorderRenderers = borderObject.GetComponentsInChildren<Renderer>();
            
            // 2. Apagamos el borde al inicio
            borderObject.SetActive(false);
        }
        if (borderSegments != null)
        {
            foreach (var segment in borderSegments)
            {
                if (segment != null)
                {
                    segment.SetActive(false);
                    var rend = segment.GetComponent<Renderer>();
                    if (rend != null) borderRenderers.Add(rend);
                }
            }
        }
    }

    public void Initialize(ResourceType type, Vector2Int coordinates)
    {
        this.resourceType = type;
        this.AxialCoordinates = coordinates;
        this.gameObject.name = $"HexTile - {type}";
    }

    public void StartFlipAnimation()
    {
        if (animator != null)
        {
            animator.Play(FLIP_ANIMATION_NAME);
        }
    }

    public void SetBorderVisible(bool visible)
    {
        if (borderObject != null)
            borderObject.SetActive(visible);
    }

    public void SetBorderColor(Color color)
    {
        // Si no hay renderers, salimos
        if (allBorderRenderers == null || allBorderRenderers.Length == 0) return;

        // Recorremos CADA trozo del borde (cada cubo) y le cambiamos el color
        foreach (Renderer rend in allBorderRenderers)
        {
            if (rend != null)
            {
                // CORRECCIÓN URP: Usamos SetColor con "_BaseColor" para asegurar compatibilidad
                // (Aunque material.color suele funcionar, esto es más seguro en URP Lit)
                rend.material.SetColor("_BaseColor", color);
                
                // Backup por si acaso el shader es estándar
                rend.material.color = color;
            }
        }
    }
    public void SetSegmentVisible(int directionIndex, bool visible, Color color)
    {
        if (directionIndex >= 0 && directionIndex < borderSegments.Length)
        {
            GameObject segment = borderSegments[directionIndex];
            if (segment != null)
            {
                segment.SetActive(visible);

                // Si lo encendemos, le ponemos el color correcto
                if (visible && borderRenderers.Count > directionIndex)
                {
                    // Usar _BaseColor para URP, o color para Standard
                    borderRenderers[directionIndex].material.color = color;
                    // borderRenderers[directionIndex].material.SetColor("_BaseColor", color);
                }
            }
        }
    }
}