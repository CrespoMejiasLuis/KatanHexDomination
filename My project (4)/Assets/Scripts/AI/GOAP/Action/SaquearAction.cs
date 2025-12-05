using UnityEngine;
using System.Collections.Generic;

public class SaquearAction : GoapAction
{
    private Player ownerPlayer;

    protected override void Awake()
    {
        base.Awake();
        actionType = ActionType.Saquear;
        cost = 15.0f; // Coste medio
        rangeInTiles = 1; // Debe estar adyacente para saquear
        requiresInRange = true;

        // Efectos esperados
        if (!Effects.ContainsKey("RecursosRobados"))
            Effects.Add("RecursosRobados", 1);
            
        // Precondición: Estar en rango (se manejará por MoverAction)
        if (!Preconditions.ContainsKey("EstaEnRango"))
            Preconditions.Add("EstaEnRango", 1);
    }

    private void Start()
    {
        if (GameManager.Instance != null && unitAgent != null)
        {
            ownerPlayer = GameManager.Instance.GetPlayer(unitAgent.ownerID);
        }
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // 1. Validar objetivo
        if (target == null) return false;

        // 2. Validar que sea una casilla válida con recursos
        // Asumimos que el target es el objeto visual de la casilla (HexTile)
        HexTile tile = target.GetComponent<HexTile>();
        if (tile == null) return false;

        CellData cell = BoardManager.Instance.GetCell(tile.AxialCoordinates);
        if (cell == null) return false;

        // 3. No se puede saquear si ya fue saqueada recientemente
        if (cell.lootedCooldown > 0) return false;

        // 4. No se puede saquear territorio propio (opcional, pero lógico)
        if (cell.owner == unitAgent.ownerID && cell.owner != -1) return false;
        
        // 5. Debe tener dueño para "robarle" (o al menos ser una casilla productiva)
        // Si queremos permitir saquear casillas neutrales, quitamos check de owner != -1
        if (cell.resource == ResourceType.Desierto) return false;

        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (ownerPlayer == null) return true;
        
        Debug.Log($"💰 GOAP: {agent.name} saqueando objetivo {target.name}...");
        running = true;

        HexTile tile = target.GetComponent<HexTile>();
        if (tile != null)
        {
            CellData cell = BoardManager.Instance.GetCell(tile.AxialCoordinates);
            if (cell != null)
            {
                // 1. Robar recurso
                ResourceType type = cell.resource;
                if (type != ResourceType.Desierto)
                {
                    ownerPlayer.AddResource(type, 1);
                    Debug.Log($"💰 ¡Saqueo exitoso! {ownerPlayer.playerName} obtuvo 1 de {type}.");
                }

                // 2. Aplicar Cooldown (daña la producción del dueño)
                cell.lootedCooldown = 1; // Un turno de penalización
                
                // 3. Feedback Visual (Opcional: cambiar color temporalmente, partículas...)
                // tile.SetBorderColor(Color.black); // Ejemplo
            }
        }

        running = false;
        return true;
    }
}
