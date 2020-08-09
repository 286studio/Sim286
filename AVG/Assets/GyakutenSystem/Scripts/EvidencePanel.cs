using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Evidence
{
    public int num;
    public string name;
    public string desc;
    public Sprite thumb;
    public Sprite detail2d;
    public GameObject detail3d;
}
public class EvidencePanel : MonoBehaviour
{
    Vector3 start = new Vector3(-270, 70);
    Vector3 step = new Vector3(180, -140);
    [SerializeField] Transform thumbnail, detail;
    [SerializeField] Button show, back;
    [SerializeField] GameObject ThumbPrefab;
    static int selected = 0;
    [SerializeField] List<Evidence> evidences;
    static List<EvidenceThumb> thumbs = new List<EvidenceThumb>();
    // Start is called before the first frame update
    void Start()
    {
        show.onClick.AddListener(ShowButtonClick);
        back.onClick.AddListener(BackButtonClick);
        for (int i = 0; i < 8; ++i)
        {
            var thumb = Instantiate(ThumbPrefab, thumbnail).GetComponent<EvidenceThumb>();
            (thumb.transform as RectTransform).anchoredPosition = start + new Vector3(i % 4 * step.x, i / 4 * step.y);
            if (i < evidences.Count) thumb.Initialize(i, evidences[i]);
            thumbs.Add(thumb);
        }
        SelectThumb();
    }
    void ShowButtonClick()
    {
        GyakutenManager.ins.IgiariDiag(evidences[selected].num);
    }
    void BackButtonClick()
    {
        gameObject.SetActive(false);
    }
    public void AddEvidence(Evidence e)
    {
        evidences.Add(e);
    }
    public static void SelectThumb(int idx = -1)
    {
        if (idx >= 0) selected = idx;
        for (int i = 0; i < thumbs.Count; ++i)
        {
            thumbs[i].GetSelected(i == selected);
        }
    }
}
