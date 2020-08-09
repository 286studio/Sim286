using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine;

public class DesktopDecoration : Furniture
{
    Furniture desktop;
    Transform parent;
    protected override void UpdatePlacing_CheckSubCells()
    {
        // 桌上物品不占位置
        coord = FurnitureManager.GetMouseCoord();
        TileNode t = FurnitureManager.GetTile(coord);
        if (t != null)
        {
            transform.position = FurnitureManager.CoordToWorldPosition(coord);
            if (t.CanStandOn(this))
            {
                desktop = t.GetFurniture();
                Transform tf = desktop.GetEmptyDeskSlot(transform.position);
                if (tf)
                {
                    print(tf.name);
                    parent = tf;
                    transform.position = tf.position;
                    transform.rotation = tf.rotation;
                    canPlaceHere = true;
                }
                else canPlaceHere = false;
            }
            else
            {
                canPlaceHere = false;
                transform.rotation = Quaternion.Euler(Vector3.zero);
            }
            cells[0, 0].GetComponent<MeshRenderer>().materials = new Material[] { FurnitureManager.ins.valid[canPlaceHere ? 0 : 1] };
        }
    }
    protected override void UpdatePlacing_Rotate()
    {
        // 桌上物品不能旋转
    }
    protected override void UpdatePlacing_Placement()
    {
        if (Input.GetMouseButtonUp(0) && canPlaceHere)
        {
            Destroy(cells[0, 0]);
            cells = null;
            Place();
            transform.parent = parent;
            desktop.decorations.Add(this);
            state = FurnitureState.Idle;
        }
    }

    public override void RemoveFromWorld()
    {
        desktop.decorations.Remove(this);
        Destroy(gameObject);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        foundation.x = 1;
        foundation.y = 1;
        furnitureType = FurnitureType.DeskDecoration;
    }
}
