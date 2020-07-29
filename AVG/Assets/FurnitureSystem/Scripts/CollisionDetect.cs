using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetect : MonoBehaviour
{
    public delegate void MouseAction();
    public event MouseAction onMouseEnter;
    public event MouseAction onMouseExit;
    public event MouseAction onMouseDown;
    private void OnMouseEnter()
    {
        onMouseEnter?.Invoke();
    }
    private void OnMouseExit()
    {
        onMouseExit?.Invoke();
    }
    
    private void OnMouseDown()
    {
        if (Furniture.AllowAction) onMouseDown?.Invoke();
    }
}
