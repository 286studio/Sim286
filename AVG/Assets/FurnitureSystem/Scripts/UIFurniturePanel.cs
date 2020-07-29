using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class UIFurniturePanel : MonoBehaviour
{
    static UIFurniturePanel ins;
    [SerializeField] Image BIGMASK;
    [SerializeField] Image line;
    [Header("Furnitures")]
    [SerializeField] Vector2 furnitureStart;
    [SerializeField] Vector2 furnitureStep;
    [SerializeField] Transform furnitureButtonsParent;
    GameObject[] furniturePrefabs;
    [Header("Desktop Decoration")]
    [SerializeField] Transform desktopDecorationButtonsParent;
    GameObject[] ddcrtPrefabs;
    [Header("Wall Decoration")]
    [SerializeField] Transform wallDecorationButtonsParent;
    GameObject[] wdcrtPrefabs;
    int col = 3;

    private void Awake()
    {
        if (ins != null) Destroy(ins);
        ins = this;
    }

    public static void AllowAction(bool b, float time = 0)
    {
        if (time == 0)
        {
            ins.BIGMASK.gameObject.SetActive(!b);
            Furniture.AllowAction = b;
        }
        else
        {
            IEnumerator c = ins.AllowActionInTime(b, time);
            ins.StartCoroutine(c);
        }
    }

    IEnumerator AllowActionInTime(bool b, float time)
    {
        yield return new WaitForSeconds(time);
        ins.BIGMASK.gameObject.SetActive(!b);
        Furniture.AllowAction = b;
    }

    // Start is called before the first frame update
    void Start()
    {
        AllowAction(true);
        furniturePrefabs = Resources.LoadAll<GameObject>("Furniture");
        ddcrtPrefabs = Resources.LoadAll<GameObject>("DesktopDecoration");
        wdcrtPrefabs = Resources.LoadAll<GameObject>("WallDecoration");
        for (int i = 0; i < furniturePrefabs.Length; ++i)
        {
            PlacingFurniture btn = Instantiate(Resources.Load<GameObject>("FurnitureButton"), furnitureButtonsParent).GetComponent<PlacingFurniture>().Initialize(furniturePrefabs[i]);
            ((RectTransform)btn.transform).anchoredPosition = furnitureStart + new Vector2(furnitureStep.x * (i % 3), furnitureStep.y * (i / 3));
            if (i == furniturePrefabs.Length - 1)
            {
                var newline = Instantiate(line, line.transform.parent);
                var pos = newline.transform.position;
                pos.y = btn.transform.position.y;
                newline.transform.position = pos;
                var apos = newline.rectTransform.anchoredPosition;
                apos.y += furnitureStep.y;
                newline.rectTransform.anchoredPosition = apos;
                ((RectTransform)desktopDecorationButtonsParent).anchoredPosition = new Vector2(0, apos.y - 12);
            }
        }
        for (int i = 0; i < ddcrtPrefabs.Length; ++i)
        {
            PlacingFurniture btn = Instantiate(Resources.Load<GameObject>("FurnitureButton"), desktopDecorationButtonsParent).GetComponent<PlacingFurniture>().Initialize(ddcrtPrefabs[i]);
            ((RectTransform)btn.transform).anchoredPosition = furnitureStart + new Vector2(furnitureStep.x * (i % 3), furnitureStep.y * (i / 3));
            if (i == ddcrtPrefabs.Length - 1)
            {
                var newline = Instantiate(line, line.transform.parent);
                var pos = newline.transform.position;
                pos.y = btn.transform.position.y;
                newline.transform.position = pos;
                var apos = newline.rectTransform.anchoredPosition;
                apos.y += furnitureStep.y;
                newline.rectTransform.anchoredPosition = apos;
                ((RectTransform)wallDecorationButtonsParent).anchoredPosition = new Vector2(0, apos.y - 12);
            }
        }
        for (int i = 0; i < wdcrtPrefabs.Length; ++i)
        {
            PlacingFurniture btn = Instantiate(Resources.Load<GameObject>("FurnitureButton"), wallDecorationButtonsParent).GetComponent<PlacingFurniture>().Initialize(wdcrtPrefabs[i]);
            ((RectTransform)btn.transform).anchoredPosition = furnitureStart + new Vector2(furnitureStep.x * (i % 3), furnitureStep.y * (i / 3));
        }
    }
}
