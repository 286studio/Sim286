using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepickFurniture : MonoBehaviour
{
    Furniture furniture;
    RectTransform rt;
    Vector3 worldPos;
    // Start is called before the first frame update
    public RepickFurniture Init(Furniture _furniture)
    {
        furniture = _furniture;
        worldPos = furniture.UIPosition;
        return this;
    }
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Repick);
        rt = GetComponent<RectTransform>();
        rt.anchoredPosition = FurnitureManager.WorldPointToCanvasPoint(worldPos);
    }
    private void Update()
    {
        rt.anchoredPosition = FurnitureManager.WorldPointToCanvasPoint(worldPos);
    }
    void Repick()
    {
        furniture.pfButton.IncreaseCount();
        foreach (var d in furniture.decorations)
        {
            d.pfButton.IncreaseCount();
        }
        furniture.OutlineEnabled = false;
        furniture.RemoveFromWorld();
        Destroy(gameObject);
    }
}
