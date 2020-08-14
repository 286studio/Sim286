using Naninovel;
using Naninovel.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;
using static Naninovel.Commands.Command;

public class DialogFragment : MonoBehaviour
{
    public static string currentFile;
    public static string currentId;
    public static DialogFragment ins;
    public static bool ready;
    [SerializeField] ProgressBar remainTime;
    [SerializeField] Image[] pieces;
    RectTransform rt;
    [SerializeField] Vector3[] endPos;
    Quaternion[] endRot;
    bool[] hasLabel;
    private void Awake()
    {
        if (!ins) ins = this;
        else Destroy(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        (remainTime.transform as RectTransform).anchoredPosition = new Vector2(0, Screen.height / 2 - 50);
        rt = transform as RectTransform;
        endPos = new Vector3[pieces.Length];
        endRot = new Quaternion[pieces.Length];
        for (int i = 0; i < endPos.Length; ++i)
        {
            int x = -Screen.width / 2 + Screen.width / 10 + Screen.width / 5 * (i % 5) + Mathf.RoundToInt(Random.Range(-Screen.width / 10, Screen.width / 10) *.75f);
            int y = (i / 5 == 0 ? 1 : -1) * Screen.height / 4 + Mathf.RoundToInt(Random.Range(-Screen.height / 4, Screen.height / 4) * .75f);
            endPos[i] = new Vector3(x, y);
            endRot[i] = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }
        StartCoroutine(Breaking());
    }

    IEnumerator Breaking()
    {
        // Take 0.2s to move to center
        Vector2 start = rt.anchoredPosition;
        for (int i = 0; i < 10; ++i)
        {
            rt.anchoredPosition = Vector2.Lerp(start, Vector2.zero, i / 10f);
            yield return new WaitForSeconds(0.02f);
        }
        rt.anchoredPosition = Vector2.zero;
        // Take 0.5s to break
        for (int i = 0; i < 25; ++i)
        {
            for (int j = 0; j < pieces.Length; ++j)
            {
                float frac = i / 25f;
                pieces[j].rectTransform.anchoredPosition = Vector3.Lerp(Vector3.zero, endPos[j], frac);
                pieces[j].rectTransform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0), endRot[j], frac);
            }
            yield return new WaitForSeconds(0.02f);
        }
    }
    IEnumerator TimeBar(float time)
    {
        float step = time / 100f;
        for (float i = 0; i <= time; i += step)
        {
            remainTime.BarValue = Mathf.Round((1 - i / time) * 100);
            yield return new WaitForSeconds(step);
        }
    }

    public static void StartCountdown(float time)
    {
        ins.StartCoroutine(ins.TimeBar(time));
    }

    public DialogFragment Initialize(string[] seg)
    {
        hasLabel = new bool[pieces.Length];
        List<int> randInt = new List<int>();
        for (int i = 0; i < 10; ++i) randInt.Add(i);
        randInt = randInt.OrderBy(a => Random.Range(0, 10)).ToList();
        for (int i = 0; i < seg.Length; ++i)
        {
            WordFragment wf = Instantiate(Resources.Load<GameObject>("WordFrag"), transform).GetComponent<WordFragment>();
            wf.transform.position = pieces[randInt[i]].transform.position;
            wf.textComp.text = seg[i];
            wf.transform.parent = pieces[randInt[i]].transform;
            hasLabel[i] = true;
            int local_i = i;
            wf.button.onClick.AddListener(delegate { SelectFragment(local_i); });
        }
        return this;
    }

    void SelectFragment(int idx)
    {
        print("Select Frag " + idx);
        if (ready && hasLabel[idx])
        {
            string _label = currentId + "_" + (idx + 1);
            print("Go to " + currentFile + "." + _label);
            GyakutenManager.player.PreloadAndPlayAsync(currentFile, label: _label);
            Destroy(gameObject);
        }
    }
}

// **************************** NEW COMMAND HERE ****************************
[CommandAlias("BreakWords")]
public class BreakWords : Command
{
    public StringParameter id;
    public StringListParameter segments;
    public DecimalParameter timeLimit;
    public StringParameter author;
    public StringParameter words;
    public async override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        DialogFragment.ready = false;
        Engine.GetService<IInputManager>().ProcessInput = false;
        var player = Engine.GetService<IScriptPlayer>();
        if (id != DialogFragment.currentId)
        {
            DialogFragment.currentId = id;
            DialogFragment.currentFile = player.PlayedScript.Name;
        }
        Object.Instantiate(Resources.Load<GameObject>("DialogFragment1"), GameObject.Find("Canvas").transform).GetComponent<DialogFragment>().Initialize(segments);

        Wait wtcmd = new Wait();
        wtcmd.WaitMode = "2.0";
        await wtcmd.ExecuteAsync();

        DialogFragment.ins.gameObject.SetActive(false);

        PrintText ptcmd = new PrintText();
        ptcmd.AuthorId = author;
        ptcmd.Text = words;
        ptcmd.WaitForInput = false;
        await ptcmd.ExecuteAsync();

        wtcmd = new Wait();
        wtcmd.WaitMode = "1.5";
        await wtcmd.ExecuteAsync();

        HidePrinter hptcmd = new HidePrinter();
        await hptcmd.ExecuteAsync();
        DialogFragment.ins.gameObject.SetActive(true);
        DialogFragment.ready = true;
        Engine.GetService<IInputManager>().ProcessInput = true;

        wtcmd = new Wait();
        wtcmd.WaitMode = timeLimit.ToString();
        DialogFragment.StartCountdown(timeLimit);
        await wtcmd.ExecuteAsync();
        if (DialogFragment.ins) Object.Destroy(DialogFragment.ins.gameObject);
    }
}
