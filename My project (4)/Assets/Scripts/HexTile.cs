using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Propiedad del Recurso")]
    public ResourceType resourceType;

    // Referencia al Animator
    private Animator animator;
    private const string FLIP_ANIMATION_NAME = "TileFlip"; // Nombre del clip de animaci�n

    void Awake()
    {
        // Obtener el componente Animator (adjunto al objeto principal)
        animator = GetComponent<Animator>();

        // OPCIONAL: Establecer la rotaci�n inicial del FlipContainer a 180 grados 
        // si la animaci�n no lo hace por defecto.
    }

    public void Initialize(ResourceType type)
    {
        this.resourceType = type;
        this.gameObject.name = $"HexTile - {type}";
    }

    /// <summary>
    /// Inicia la animaci�n de volteo.
    /// Esta funci�n es llamada desde HexGridGenerator.
    /// </summary>
    public void StartFlipAnimation()
    {
        if (animator != null)
        {
            animator.Play(FLIP_ANIMATION_NAME);
        }
        else
        {
            Debug.LogError("Animator no encontrado en la casilla: " + gameObject.name);
        }
    }
}