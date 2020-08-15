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
    Graphic[] gs;
    float[] initAlpha;
    public static GameObject targetCursor = null;
    int autoMode;
    private void Awake()
    {
        if (!ins) ins = this;
        else Destroy(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        (remainTime.transform as RectTransform).anchoredPosition = new Vector2(0, Screen.height / 2 - 50);

        gs = GetComponentsInChildren<Graphic>();
        initAlpha = new float[gs.Length];
        for (int i = 0; i < gs.Length; ++i) initAlpha[i] = gs[i].color.a;

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

    private void Update()
    {
        if (targetCursor)
        {
            if (autoMode < 0) (targetCursor.transform as RectTransform).anchoredPosition = Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2);
        }
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
    public Button[] buttons;
    public DialogFragment Initialize(string[] seg, int _autoMode = -1)
    {
        autoMode = _autoMode;
        hasLabel = new bool[pieces.Length];
        buttons = new Button[pieces.Length];
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
            buttons[i] = wf.button;
        }
        return this;
    }

    void SelectFragment(int idx)
    {
        if (ready && hasLabel[idx])
        {
            string _label = currentId + "_" + (idx + 1);
            GyakutenManager.player.PreloadAndPlayAsync(currentFile, label: _label);
            Cursor.visible = true;
            if (targetCursor) Destroy(targetCursor);
            Destroy(gameObject);
        }
    }

    IEnumerator FadeOut()
    {
        float step = 1 / 30f;
        for (float i = 0; i < .5f; i += step)
        {
            for (int j = 0; j < gs.Length; ++j)
            {
                Color c = gs[j].color;
                c.a = (1 - i / 0.5f) * initAlpha[j];
                gs[j].color = c;
            }
            yield return new WaitForSeconds(step);
        }
        gameObject.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        float step = 1 / 30f;
        for (float i = 0; i < .5f; i += step)
        {
            for (int j = 0; j < gs.Length; ++j)
            {
                Color c = gs[j].color;
                c.a = i / 0.5f * initAlpha[j];
                gs[j].color = c;
            }
            yield return new WaitForSeconds(step);
        }
        for (int j = 0; j < gs.Length; ++j)
        {
            Color c = gs[j].color;
            c.a = initAlpha[j];
            gs[j].color = c;
        }
    }
    public static void Fade(bool fadeIn)
    {
        if (fadeIn) ins.gameObject.SetActive(true);
        ins.StartCoroutine(fadeIn ? ins.FadeIn() : ins.FadeOut());
    }

    public IEnumerator CursorMoveTo(int idx)
    {
        float step = 1 / 30f;
        Vector3 start = targetCursor.transform.position;
        for (float i = 0; i < 2f; i += step)
        {
            targetCursor.transform.position = Vector3.Lerp(start, buttons[idx].transform.position, i / .6f);
            yield return new WaitForSeconds(step);
        }
        targetCursor.transform.position = buttons[idx].transform.position;
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

        DialogFragment.Fade(false);
        wtcmd = new Wait();
        wtcmd.WaitMode = "0.5";
        await wtcmd.ExecuteAsync();

        PrintText ptcmd = new PrintText();
        ptcmd.AuthorId = author;
        ptcmd.Text = words;
        ptcmd.WaitForInput = false;
        await ptcmd.ExecuteAsync();

        wtcmd = new Wait();
        wtcmd.WaitMode = "2.0";
        await wtcmd.ExecuteAsync();

        HidePrinter hptcmd = new HidePrinter();
        await hptcmd.ExecuteAsync();
        DialogFragment.Fade(true);
        DialogFragment.ready = true;
        DialogFragment.targetCursor = Object.Instantiate(Resources.Load<GameObject>("TargetCursor"), GameObject.Find("Canvas").transform);
        Cursor.visible = false;
        Engine.GetService<IInputManager>().ProcessInput = true;

        wtcmd = new Wait();
        wtcmd.WaitMode = timeLimit.ToString();
        DialogFragment.StartCountdown(timeLimit);
        await wtcmd.ExecuteAsync();
        if (id == DialogFragment.currentId)
        {
            if (DialogFragment.ins) Object.Destroy(DialogFragment.ins.gameObject);
            if (DialogFragment.targetCursor) Object.Destroy(DialogFragment.targetCursor);
            Cursor.visible = true;
        }
    }
}

[CommandAlias("BreakWordsAuto")]
public class BreakWordsAuto : Command
{
    public StringParameter id;
    public StringListParameter segments;
    public DecimalParameter timeLimit;
    public IntegerParameter select;
    public async override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Cursor.visible = false;
        DialogFragment.ready = false;
        Engine.GetService<IInputManager>().ProcessInput = false;
        var player = Engine.GetService<IScriptPlayer>();
        if (id != DialogFragment.currentId)
        {
            DialogFragment.currentId = id;
            DialogFragment.currentFile = player.PlayedScript.Name;
        }
        Object.Instantiate(Resources.Load<GameObject>("DialogFragment1"), GameObject.Find("Canvas").transform).GetComponent<DialogFragment>().Initialize(segments, select);

        Wait wtcmd = new Wait();
        wtcmd.WaitMode = "3.0";
        await wtcmd.ExecuteAsync();

        DialogFragment.targetCursor = Object.Instantiate(Resources.Load<GameObject>("TargetCursor"), GameObject.Find("Canvas").transform);
        DialogFragment.ins.StartCoroutine(DialogFragment.ins.CursorMoveTo(select - 1));

        wtcmd = new Wait();
        wtcmd.WaitMode = "2.5";
        await wtcmd.ExecuteAsync();

        Engine.GetService<IInputManager>().ProcessInput = true;
        if (id == DialogFragment.currentId)
        {
            if (DialogFragment.ins) Object.Destroy(DialogFragment.ins.gameObject);
            if (DialogFragment.targetCursor) Object.Destroy(DialogFragment.targetCursor);
            Cursor.visible = true;
        }
    }
}