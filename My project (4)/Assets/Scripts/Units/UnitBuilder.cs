// 📁 UnitBuilder.cs (VERSIÓN 2.0 - Con Lógica)
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitBuilder : MonoBehaviour
{
    [Header("Configuración de Construcción")]
    [Tooltip("Arrastra aquí el Prefab de tu 'Poblado' o 'Ciudad'")]
    public GameObject pobladoPrefab; // ¡Crea este campo!

    // Referencia al cerebro de la unidad
    private Unit unitCerebro;

    void Awake()
    {
        unitCerebro = GetComponent<Unit>();
    }

    /// <summary>
    /// Esta es la función principal que será llamada por un botón de la UI.
    /// </summary>
    public void IntentarConstruirPoblado()
    {
        // 1. Comprobación de seguridad: ¿Tenemos un prefab de poblado asignado?
        if (pobladoPrefab == null)
        {
            Debug.LogError("¡No hay un 'pobladoPrefab' asignado en el UnitBuilder!");
            return;
        }

        // (Aquí irán las comprobaciones de recursos: ¿Tengo 5 de madera y 2 de trigo?)
        // (if (GameManager.Instance.humanPlayer.TieneRecursos(...)) { ... }

        // 2. Obtener la casilla LÓGICA donde estamos
        // Usamos el BoardManager (que es un Singleton) para pedir la celda
        CellData cellDondeEstamos = BoardManager.Instance.GetCell(unitCerebro.misCoordenadasActuales);

        if (cellDondeEstamos == null)
        {
            Debug.LogError("Error: La unidad no parece estar en una casilla válida.");
            return;
        }

        // (Aquí irán más comprobaciones: ¿Ya hay una ciudad en esta casilla?)
        // (if (cellDondeEstamos.hasCity) { ... }

        // 3. ¡Todo correcto! Procedemos a construir.
        
        // 4. Obtenemos la casilla VISUAL (el HexTile)
        HexTile tileVisual = cellDondeEstamos.visualTile; // ¡Por esto era tan importante enlazarlo!

        // 5. Instanciamos el poblado
        // Lo creamos en la misma posición que la casilla y con su misma rotación
        Instantiate(pobladoPrefab, tileVisual.transform.position, tileVisual.transform.rotation);

        // 6. ¡Lanzamos la animación de la casilla!
        // Tu script HexTile.cs ya tiene esta función pública
        tileVisual.StartFlipAnimation();

        // 7. (Lógica de tu juego) Actualizar el estado lógico de la casilla
        // Mañana, cuando lo habléis, aquí es donde iría la llamada:
        // BoardManager.Instance.SetCellAsCity(unitCerebro.misCoordenadasActuales);

        // 8. El colono se consume (¡Adiós!)
        //Debug.Log("¡Poblado construido! El colono se ha consumido.");
        //Destroy(gameObject);
    }
}