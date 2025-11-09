using UnityEngine;
using UnityEngine.EventSystems; 

public class SimpleClickTester : MonoBehaviour
{
    public Unit unidadSeleccionada; // Arrastra tu Colono de la escena aquí

    private Camera camaraPrincipal;

    void Start()
    {
        camaraPrincipal = Camera.main;
    }

    void Update()
    {
        // 1. Si no hay unidad seleccionada, no hacer nada
        if (unidadSeleccionada == null)
        {
            return;
        }

        // 2. Detectar el clic izquierdo
        if (Input.GetMouseButtonDown(0)) 
        {
            // Si el ratón está sobre un objeto de la UI (un botón, un panel, etc.), no hagas nada.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return; // Salir de la función
            }
    
            // 3. Lanzar un rayo desde la cámara
            Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 4. Si el rayo golpea algo...
            if (Physics.Raycast(rayo, out hit))
            {
                // 5. ...comprobar si ese algo es una HexTile
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