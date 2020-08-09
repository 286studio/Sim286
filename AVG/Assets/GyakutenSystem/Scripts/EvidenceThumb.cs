using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceThumb : MonoBehaviour
{
    int idx;
    Evidence e;
    [SerializeField] Image ThumbImage;
    [SerializeField] Button button;
    public EvidenceThumb Initialize(int _idx, Evidence _e)
    {
        e = _e;
        idx = _idx;
        ThumbImage.sprite = e.thumb;
        button.onClick.AddListener(delegate { EvidencePanel.SelectThumb(idx); });
        return this;
    }
    public void GetSelected(bool selected)
    {
        GetComponent<Image>().color = selected ? Color.yellow : Color.white;
    }
}
