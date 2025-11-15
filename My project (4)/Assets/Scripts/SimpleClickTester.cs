using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleClickTester : MonoBehaviour
{
    [HideInInspector] public Unit unidadSeleccionada;
    private Camera camaraPrincipal;

    [Header("UI y referencias")]
    public GameObject unitActionMenu;

    [Header("Configuracion de Capas")]
    public LayerMask unitLayerMask;
    public LayerMask gridLayerMask;

    private readonly int PLAYER_ID = 0; //human player

    private PlayerInputMode currentMode = PlayerInputMode.Selection;
    private SettlementUnit activePoblado = null;

    void Start()
    {
        camaraPrincipal = Camera.main;

        if (unitActionMenu != null) unitActionMenu.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return; // No hay clic, no hacer nada
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Clic en la UI, no hacer nada en el mundo
        }

        Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // --- LÓGICA DE CLIC EN EL MUNDO ---

        // 1. ¿He clicado en la capa de UNIDADES?
        if (Physics.Raycast(rayo, out hit, float.MaxValue, unitLayerMask))
        {
            Unit unidadClickada = hit.collider.GetComponentInParent<Unit>();
            if (unidadClickada != null)
            {
                // Clicar en una unidad SIEMPRE la selecciona (y resetea el modo)
                HandleUnitSelection(unidadClickada);
                return; // Acción de clic completada
            }
        }

        // 2. ¿He clicado en la capa de CASILLAS?
        if (Physics.Raycast(rayo, out hit, float.MaxValue, gridLayerMask))
        {
            HexTile casillaClicada = hit.collider.GetComponentInParent<HexTile>();
            if (casillaClicada != null)
            {
                // Clic en casilla. Qué hacer depende del MODO actual
                HandleGridClick(casillaClicada);
                return; // Acción de clic completada
            }
        }

        // 3. ¿He clicado en el vacío?
        // Si el código llega aquí, es que no se ha clicado ni en unidad ni en casilla
        DeseleccionarUnit();
    }

    private void HandleGridClick(HexTile casillaClicada)
    {
        // Comprobar en qué modo estamos
        switch (currentMode)
        {
            case PlayerInputMode.Selection:
                // Estamos en modo selección. Clicar en una casilla vacía
                // simplemente deselecciona la unidad actual.
                DeseleccionarUnit();
                break;

            case PlayerInputMode.MoveTargeting:
                // ¡Aha! Estábamos esperando una casilla para movernos
                if (unidadSeleccionada != null)
                {
                    UnitMovement mover = unidadSeleccionada.GetComponent<UnitMovement>();
                    if (mover != null)
                    {
                        mover.IntentarMover(casillaClicada);
                    }
                }
                // Haya funcionado o no, el "modo movimiento" ha terminado.
                // Volvemos al modo selección (la unidad sigue seleccionada).
                currentMode = PlayerInputMode.Selection;
                break;
        }
    }

    //para que si clickas una unidad te salga el menu de acciones que puede realizar
    private void HandleUnitSelection(Unit unitClickada)
    {
        DeseleccionarUnit(); // Deselecciona la anterior

        unidadSeleccionada = unitClickada;

        if(unitClickada.ownerID == PLAYER_ID)
        {
            SettlementUnit pobladoLogic = unitClickada.GetComponent<SettlementUnit>(); 
        
            if(pobladoLogic != null) 
            {
                // Es un poblado. Llamamos a su acción específica.
                activePoblado = pobladoLogic;
                pobladoLogic.OpenTradeMenu();
                Debug.Log("Poblado seleccionado. Menú de Intercambio activado.");
                return;
            }

            if(unitActionMenu != null) unitActionMenu.SetActive(true);
            
        }
        else
        {
            if(unitActionMenu != null) unitActionMenu.SetActive(false);
        }
        
        // ¡Importante! Al seleccionar una unidad, SIEMPRE volvemos al modo Selección
        currentMode = PlayerInputMode.Selection;
    }

    // MODIFICADA: Ahora también resetea el modo
    private void DeseleccionarUnit()
    {
        if(activePoblado!=null && activePoblado.tradeMenu!=null)
        {
            activePoblado.tradeMenu.SetActive(false);
            activePoblado = null;
        }

        if(unidadSeleccionada != null)
        {
            unidadSeleccionada = null;
        }

        if(unitActionMenu != null)
        {
            unitActionMenu.SetActive(false);
        }

        // ¡Importante! Si no hay nada seleccionado, volvemos al modo Selección
        currentMode = PlayerInputMode.Selection;
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
            unitActionMenu.SetActive(false);
        }
        else
        {
            Debug.Log("¡La unidad seleccionada (" + unidadSeleccionada.name + ") no puede construir!");
        }
    }

    public void BotonMoverPulsado()
    {
        if (unidadSeleccionada == null)
        {
            return; // No hay unidad seleccionada, no hacer nada
        }

        // Comprobar si la unidad se puede mover
        if (unidadSeleccionada.GetComponent<UnitMovement>() != null && unidadSeleccionada.movimientosRestantes > 0)
        {
            // ¡Sí puede! Cambiamos al modo de movimiento
            currentMode = PlayerInputMode.MoveTargeting;
            
            // Ocultamos el menú para que el jugador pueda ver el tablero
            if (unitActionMenu != null)
            {
                unitActionMenu.SetActive(false);
            }
            
            Debug.Log("MODO MOVER: Seleccione una casilla de destino.");
        }
        else
        {
            Debug.Log("¡Esta unidad no se puede mover o no tiene puntos de movimiento!");
        }
    }
}