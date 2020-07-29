using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class PlacingFurniture : MonoBehaviour
{
    [SerializeField] int count = 3;
    [SerializeField] Button button;
    [SerializeField] Text showName;
    [SerializeField] Text showCountNumber;
    [SerializeField] GameObject furniture;
    static Furniture placingObject;
    // Start is called before the first frame update
    void Start()
    {
        showName.text = furniture.GetComponent<Furniture>().furnitureName;
        showCountNumber.text = 'x' + count.ToString();
        button.onClick.AddListener(ButtonOnClick);
    }
    public PlacingFurniture Initialize(GameObject tobePlaced, int _count = 3)
    {
        furniture = tobePlaced;
        count = _count;
        return this;
    }
    void ButtonOnClick()
    {
        if (count <= 0 || placingObject) return;
        Furniture.CancelSelect();
        FurnitureManager.PlacingFurniture = true;
        placingObject = Instantiate(furniture, FurnitureManager.ins.parentNode).GetComponent<Furniture>();
        placingObject.pfButton = this;
        placingObject.onPlacingSuccess += OnPlacingSuccess;
        placingObject.onPlacingCancel += OnPlacingCancel;
        --count;
        showCountNumber.text = 'x' + count.ToString();
        UIFurniturePanel.AllowAction(false);
    }
    void OnPlacingCancel()
    {
        FurnitureManager.PlacingFurniture = false;
        ++count;
        showCountNumber.text = 'x' + count.ToString();
        UIFurniturePanel.AllowAction(true, 0.1f);
    }
    void OnPlacingSuccess()
    {
        placingObject = null;
        FurnitureManager.PlacingFurniture = false;
        UIFurniturePanel.AllowAction(true, 0.1f);
    }

    public void IncreaseCount()
    {
        ++count;
        showCountNumber.text = 'x' + count.ToString();
    }
    private void OnValidate()
    {
        button = GetComponent<Button>();
        Text[] uitexts = GetComponentsInChildren<Text>();
        showName = uitexts[0];
        showCountNumber = uitexts[1];
    }
}
