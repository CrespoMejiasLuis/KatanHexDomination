// 📁 SimpleClickTester.cs (Modificado)
using UnityEngine;
using UnityEngine.EventSystems; // (Esta ya la deberías tener)

public class SimpleClickTester : MonoBehaviour
{
    public Unit unidadSeleccionada;
    private Camera camaraPrincipal;

    [Header("Configuración de Capas")]
    public LayerMask gridLayerMask; // Máscara para la capa del tablero

    void Start()
    {
        camaraPrincipal = Camera.main;
    }

    void Update()
    {
        if (unidadSeleccionada == null) { return; }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(rayo, out hit, float.MaxValue, gridLayerMask))
            {
                HexTile casillaClicada = hit.collider.GetComponentInParent<HexTile>();

                if (casillaClicada != null)
                {
                    Debug.Log("Clic en casilla: " + casillaClicada.name);
                    
                    // 6. ¡Llamar al script de movimiento de la unidad!
                    // (Obtenemos su componente de movimiento)
                    UnitMovement mover = unidadSeleccionada.GetComponent<UnitMovement>();
                    if (mover != null)
                    {
                        mover.IntentarMover(casillaClicada);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Esta función es pública para que un botón de la UI pueda llamarla.
    /// </summary>
    public void BotonConstruirPulsado()
    {
        // 1. ¿Tenemos una unidad seleccionada?
        if (unidadSeleccionada == null)
        {
            Debug.Log("¡No hay ninguna unidad seleccionada para construir!");
            return;
        }

        // 2. ¿Esa unidad es un Colono (tiene el script UnitBuilder)?
        UnitBuilder builder = unidadSeleccionada.GetComponent<UnitBuilder>();

        if (builder != null)
        {
            // 3. ¡Sí! Le damos la orden de construir
            builder.IntentarConstruirPoblado();
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede construir!");
        }
    }
}