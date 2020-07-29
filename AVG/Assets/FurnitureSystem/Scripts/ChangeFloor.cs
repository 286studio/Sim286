using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ChangeFloor : MonoBehaviour
{
    [SerializeField] Dropdown dp;
    // Start is called before the first frame update
    void Start()
    {
        dp.options.Clear();
        for (int i = 1; i <= 16; ++i) dp.options.Add(new Dropdown.OptionData("样式" + i));
        dp.value = 1;
        dp.onValueChanged.AddListener(OnDropdownValueChange);
    }

    private void OnValidate()
    {
        dp = GetComponent<Dropdown>();
    }

    void OnDropdownValueChange(int val)
    {
        FurnitureManager.ChangeFloorPattern(val);
    }
}
