using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WordFragment : MonoBehaviour
{
    public Button button;
    public Text textComp;
    private void OnValidate()
    {
        button = GetComponent<Button>();
        textComp = GetComponentInChildren<Text>();
    }
}
