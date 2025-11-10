using UnityEngine;

public class CellData
{
    public ResourceType resource;
    public int owner; //-1 without, 0 player, 1 IA
    public bool hasCity;
    public bool hasTroup;
    public int cost;
    public Vector2Int coordinates;
    public HexTile visualTile; // Puedes descomentar esto si lo necesitas

    public CellData(ResourceType resource, Vector2Int coords)
    {
        this.resource = resource;
        this.owner = -1;
        this.hasCity = false;
        this.hasTroup = false;
        this.coordinates = coords;

        if(resource == ResourceType.Desierto || resource == ResourceType.Madera || resource == ResourceType.Roca)
        {
            cost = 2;
        }
        else
            cost = 1;
    }
}