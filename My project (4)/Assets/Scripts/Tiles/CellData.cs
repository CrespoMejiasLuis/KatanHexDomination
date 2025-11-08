using UnityEngine;

public class CellData : MonoBehaviour
{
    public ResourceType resource;
    public int owner; //-1 without, 0 player, 1 IA
    public bool hasCity;
    public int troupCount;
    public Vector2Int coordinates;
    public HexTile visualTile;

    public CellData(ResourceType resource, Vector2Int coords)
    {
        this.resource = resource;
        this.owner = -1;
        this.hasCity = false;
        this.troupCount = 0;
        this.coordinates = coords;
    }

    
}
