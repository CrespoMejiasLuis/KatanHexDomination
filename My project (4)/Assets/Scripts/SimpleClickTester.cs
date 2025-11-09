// 📁 SimpleClickTester.cs (Script de PRUEBA)
using UnityEngine;

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
}