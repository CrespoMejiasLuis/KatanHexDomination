using UnityEngine;

public class CellData
{
    public ResourceType resource;
    public int owner; //-1 without, 0 player, 1 IA
    public Unit unitOnCell;
    public TypeUnit typeUnitOnCell;
    public int cost;
    public Vector2Int coordinates;
    public HexTile visualTile; // Puedes descomentar esto si lo necesitas
    public bool isRaided = false;

    public CellData(ResourceType resource, Vector2Int coords)
    {
        this.resource = resource;
        this.owner = -1;
        this.unitOnCell = null;
        this.typeUnitOnCell = TypeUnit.None;
        this.coordinates = coords;

        if(resource == ResourceType.Desierto || resource == ResourceType.Madera || resource == ResourceType.Roca)
        {
            cost = 2;
        }
        else
            cost = 1;
    }
    public void UpdateVisual()
    {
        if (visualTile == null) return;

        switch (owner)
        {
            case -1: // Sin dueño
                visualTile.SetBorderVisible(false);
                break;

            case 0: // Jugador
                visualTile.SetBorderVisible(true);
                visualTile.SetBorderColor(Color.white);
                break;

            case 1: // IA
                visualTile.SetBorderColor(Color.yellow);
                visualTile.SetBorderVisible(true);
                break;
        }
    }

}