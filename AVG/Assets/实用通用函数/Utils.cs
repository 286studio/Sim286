using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 这里写一些实用的Unity项目通用函数
public class Utils
{
    public static Vector2 WorldPointToCanvasPoint(Vector3 worldPoint, Camera cam)
    {
        var CanvasRect = GameObject.Find("Canvas").GetComponent<RectTransform>();
        Vector2 ViewportPosition = cam.WorldToViewportPoint(worldPoint);
        return new Vector2(ViewportPosition.x * CanvasRect.sizeDelta.x - CanvasRect.sizeDelta.x * 0.5f,
          ViewportPosition.y * CanvasRect.sizeDelta.y - CanvasRect.sizeDelta.y * 0.5f);
    }
}
