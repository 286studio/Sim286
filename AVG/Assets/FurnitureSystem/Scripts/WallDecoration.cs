using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallDecoration : Furniture
{
    protected override void UpdatePlacing_CheckSubCells()
    {
        base.UpdatePlacing_CheckSubCells();
        TileNode t = FurnitureManager.MouseAtTile();
        if (t != null && t.tileType == TileNodeType.Wall) RotateMesh(t.wallDir);
    }
    protected override void UpdatePlacing_Rotate()
    {
        // 墙饰不能主动旋转
    }
    public override Vector3 UIPosition
    {
        get
        {
            return transform.position + new Vector3(foundation.x / 2f - 0.5f, 2 * pivotOffset.y, foundation.y / 2f - 0.5f) * FurnitureManager.gridSize;
        }
    }
}
