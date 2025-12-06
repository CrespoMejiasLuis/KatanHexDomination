using UnityEngine;
using System.Collections.Generic;

public class HuirAction : GoapAction
{
    private GoapAgent goapAgent;

    protected override void Awake()
    {
        base.Awake();
        goapAgent = GetComponent<GoapAgent>();

        actionType = ActionType.Huir;
        cost = 20.0f; // Coste más alto que Saquear (15) para preferir saquear+curarse si es posible
        rangeInTiles = 0; // El destino ES la seguridad
        requiresInRange = true;

        // Efectos
        if (!Effects.ContainsKey("Seguro"))
            Effects.Add("Seguro", 1);

        // Precondiciones
        if (!Preconditions.ContainsKey("EstaEnRango"))
            Preconditions.Add("EstaEnRango", 1);
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        if (goapAgent == null || unitAgent == null) return false;

        // Si tenemos más del 40% de vida, no necesitamos huir específicamente
        if (unitAgent.vidaActual > unitAgent.statsBase.vidaMaxima * 0.4f)
        {
            return false;
        }

        // Validar que tenemos un destino de huida asignado
        Vector2Int escapeDest = goapAgent.targetDestination;
        
        // Si estamos ya ahí, es válido ejecutar la acción de "Huir" (que es básicamente "Llegar a salvo")
        // Pero MoverAction se encarga de llevarnos.
        // Aquí solo validamos que el destino exista y sea válido en el mapa.
        CellData cell = BoardManager.Instance.GetCell(escapeDest);
        if (cell == null) return false;

        // Asignamos el target físico para que IsInRange funcione
        if (cell.visualTile != null)
        {
            target = cell.visualTile.gameObject;
        }

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        running = true;
        Debug.Log($"🏳️ GOAP: {agent.name} ha huido y está a salvo en {unitAgent.misCoordenadasActuales}.");
        
        // Aquí podrías añadir lógica de recuperación, como curarse.
        // O cambiar el estado mental de la unidad.

        running = false;
        return true;
    }
}
