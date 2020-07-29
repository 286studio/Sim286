using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileNodeType
{
    Clear,
    Wall,
    WallSeparator,
    Invalid
}
public class TileNode
{
    public Vector3Int coord;
    public TileNodeType tileType = TileNodeType.Clear;
    public HashSet<Furniture> objectOnThisTile = new HashSet<Furniture>();
    public Dir wallDir;
    
    public TileNode(Vector3Int _coord)
    {
        coord = _coord;
    }
    public void Occupies(Furniture f)
    {
        objectOnThisTile.Add(f);
    }
    public void Unoccupies(Furniture f)
    {
        objectOnThisTile.Remove(f);
    }
    public bool CanStandOn(Furniture f, bool edgeCell = false)
    {
        if (tileType == TileNodeType.Invalid) return false;
        if (f.furnitureType == FurnitureType.DeskDecoration)
        {
            foreach (var o in objectOnThisTile) if (o.furnitureType == FurnitureType.Desk) return true;
            return false;
        }
        if (f.furnitureType == FurnitureType.WallDecoration) return tileType == TileNodeType.Wall;
        if (f.furnitureType != FurnitureType.WallDecoration && tileType == TileNodeType.Wall) return false;
        if (objectOnThisTile.Count > 0 && (int)f.furnitureType <= 2) return false;
        if (edgeCell && (int)f.furnitureType <= 2 && tileType == TileNodeType.WallSeparator && objectOnThisTile.Count == 0) return true;
        if (tileType == TileNodeType.WallSeparator) return false;
        return true;
    }
    public Furniture GetFurniture()
    {
        foreach (var o in objectOnThisTile) return o;
        return null;
    }
}
