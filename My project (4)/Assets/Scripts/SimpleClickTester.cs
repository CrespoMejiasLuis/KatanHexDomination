// 📁 SimpleClickTester.cs (Modificado)
using UnityEngine;
using UnityEngine.EventSystems; // (Esta ya la deberias tener)

public class SimpleClickTester : MonoBehaviour
{
    public Unit unidadSeleccionada;
    private Camera camaraPrincipal;

    [Header("UI y referencias")]
    public GameObject unitActionMenu;

    [Header("Configuracion de Capas")]
    public LayerMask unitLayerMask;
    public LayerMask gridLayerMask; // Mascara para la capa del tablero

    private readonly int PLAYER_ID = 0; //human player

    void Start()
    {
        camaraPrincipal = Camera.main;

        if(unitActionMenu!=null) unitActionMenu.SetActive(false);
    }

    void Update()
    {
        if (unidadSeleccionada == null) { return; }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            //1.Intentar clickar una unidad
            if(Physics.Raycast(rayo, out hit, float.MaxValue, unitLayerMask))
            {
                Unit unidadClickada = hit.collider.GetComponentInParent<Unit>();
                if(unidadClickada !=null)
                {
                    HandleUnitSelection(unidadClickada);
                    return;
                }
            }

            //2.Intentar clicka casilla
            if (Physics.Raycast(rayo, out hit, float.MaxValue, gridLayerMask))
            {
                HexTile casillaClicada = hit.collider.GetComponentInParent<HexTile>();

                if (casillaClicada != null)
                {
                    Vector2Int coord = casillaClicada.AxialCoordinates;
                    CellData cellData = BoardManager.Instance.GetCell(coord);

                    if(cellData == null)
                    {
                        Debug.Log("Error, no se ha encontrado CellData para la casilla");
                        return;
                    }

                    if(unidadSeleccionada != null)
                    {
                        // 6. ¡Llamar al script de movimiento de la unidad!
                        // (Obtenemos su componente de movimiento)
                        UnitMovement mover = unidadSeleccionada.GetComponent<UnitMovement>();
                        if (mover != null)
                        {
                            mover.IntentarMover(casillaClicada);
                        }
                    }
                    else
                    {
                        if(cellData.unitOnCell != null)
                        {
                            HandleUnitSelection(cellData.unitOnCell);
                        }
                    }

                    Debug.Log("Clic en casilla: " + casillaClicada.name);
                                        
                }
            }
            
            //3.Click en cualquier parte deseleccionar
            DeseleccionarUnit();
        }
    }

    //para que si clickas una unidad te salga el menu de acciones que puede realizar
    private void HandleUnitSelection(Unit unitClickada)
    {
        DeseleccionarUnit();

        unidadSeleccionada = unitClickada;

        if(unitClickada.ownerID == PLAYER_ID)
        {
            if(unitActionMenu!=null) unitActionMenu.SetActive(true);
        }
        else
        {
            if(unitActionMenu!=null) unitActionMenu.SetActive(false);
        }
            
    }

    //para que desaparezca el de una unidad no clickada
    private void DeseleccionarUnit()
    {
        if(unidadSeleccionada!= null) unidadSeleccionada = null;

        if(unitActionMenu!=null) unitActionMenu.SetActive(false);
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