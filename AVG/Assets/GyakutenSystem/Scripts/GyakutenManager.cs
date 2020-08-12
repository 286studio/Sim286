using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;
using UnityEngine.UI;
using Naninovel.Commands;
using UniRx.Async;
using System.Threading;
using UnityEditor.U2D.Path;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

public enum GyakutenState
{
    None,
    ShatteredTestimony,
}
public class TestimonyData
{
    public string inquiryName { private set; get; }
    public int lineIdx { private set; get; }
    public bool hidden = false;
    public bool unlocked = false;
    public bool shattered = false;

    public TestimonyData(string _inquiryName, int _lineIdx)
    {
        inquiryName = _inquiryName;
        lineIdx = _lineIdx;
    }
    public bool IsReallyHidden()
    {
        if (hidden && !unlocked) return true;
        return false;
    }
}
public class GyakutenManager : MonoBehaviour
{
    public static GyakutenManager ins;
    [SerializeField] Button prev, next;
    [SerializeField] Button inquiry, evidence;
    public Evidence[] evidences;
    [SerializeField] GameObject backpackPrefab;
    public static string inquiryName;
    public static IScriptPlayer player;
    public static int cur;
    public static string scriptFile;
    public static TestimonyData[] testimonies = null;
    GameObject backpack;
    public static bool success;
    public static GyakutenState state = GyakutenState.None;
    private void Awake()
    {
        ins = this;
    }
    async void Start()
    {
        ShowInquiryOptions(false); // hide ui on start

        // Test (Remove pending)
        await RuntimeInitializer.InitializeAsync();
        player = Engine.GetService<IScriptPlayer>();
        await player.PreloadAndPlayAsync("Inquiry");

        // assign function to buttons
        prev.onClick.AddListener(PrevTestimony);
        next.onClick.AddListener(NextTestimony);
        inquiry.onClick.AddListener(Mada);
        evidence.onClick.AddListener(Igiari);
    }
    private void Update()
    {
        switch (state)
        {
            case GyakutenState.ShatteredTestimony: Update_ShatteredTestimony(); break;
            default: break;
        }
    }
    public static void ShowInquiryOptions(bool shown)
    {
        ins.gameObject.SetActive(shown);
    }
    void PrevTestimony()
    {
        if (cur > 0)
        {
            do --cur; while (testimonies[cur].IsReallyHidden());
            player.PreloadAndPlayAsync(scriptFile, testimonies[cur].lineIdx);
        }
    }
    void NextTestimony()
    {
        do ++cur; while (cur < testimonies.Length && testimonies[cur].IsReallyHidden());
        if (cur >= testimonies.Length)
            player.PreloadAndPlayAsync(scriptFile, testimonies[testimonies.Length - 1].lineIdx + 1);
        else
            player.PreloadAndPlayAsync(scriptFile, testimonies[cur].lineIdx);
    }
    void Mada()
    {
        player.PreloadAndPlayAsync(inquiryName + (cur + 1) + "_Mada");
        ShowInquiryOptions(false);
        Engine.GetService<IInputManager>().ProcessInput = true;
        player.OnStop += ReturnFromMada;
    }
    void ReturnFromMada(Script madaScript)
    {
        player.OnStop -= ReturnFromMada;
        ShowInquiryOptions(true);
        Engine.GetService<IInputManager>().ProcessInput = false;
        NextTestimony();
    }
    void Igiari()
    {
        if (!backpack) backpack = Instantiate(backpackPrefab, transform);
        else backpack.SetActive(true);
    }
    public void IgiariDiag(int evidenceNum)
    {
        int testimonyNum = cur + 1;
        string[] filenames =
        {
            inquiryName + testimonyNum + "_Igiari" + evidenceNum,
            inquiryName + testimonyNum + "_Igiari_Default",
            inquiryName + "_Igiari_Default"
        };
        string filename = "";
        foreach (string f in filenames)
        {
            if (File.Exists(Application.dataPath + "/GyakutenSystem/NaniScripts/" + f + ".nani"))
            {
                filename = f;
                break;
            }
        }
        
        player.PreloadAndPlayAsync(filename);
        ShowInquiryOptions(false);
        Engine.GetService<IInputManager>().ProcessInput = true;
        player.OnStop += ReturnFromIgiari;
    }
    void ReturnFromIgiari(Script igiariScript)
    {
        player.OnStop -= ReturnFromIgiari;
        if (success) player.PreloadAndPlayAsync(scriptFile, label: inquiryName + "END");
        else
        {
            ShowInquiryOptions(true);
            Engine.GetService<IInputManager>().ProcessInput = false;
            player.PreloadAndPlayAsync(scriptFile, testimonies[cur].lineIdx);
        }
    }
    public static void Start_ShatteredTestimony()
    {
        Engine.GetService<IInputManager>().ProcessInput = false;
        player.Stop();
        Engine.GetService<IStateManager>().ResetStateAsync();
    }
    void Update_ShatteredTestimony()
    {

    }
}

// **************************** NEW COMMAND HERE ****************************
[CommandAlias("InquiryStart")]
public class InquiryStart : Command
{
    public StringParameter id;
    public IntegerParameter count;
    public IntegerListParameter hidden;
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // inquiryName is unique to each inquiry
        if (Assigned(id))
        {
            if (GyakutenManager.inquiryName != id)
            {
                GyakutenManager.testimonies = new TestimonyData[count];
                for (int i = 0; i < count; ++i)
                {
                    GyakutenManager.testimonies[i] = new TestimonyData(id, PlaybackSpot.LineIndex + 1 + i);
                    if (Assigned(hidden)) foreach (int? d in hidden) if (d == i + 1) { GyakutenManager.testimonies[i].hidden = true; break; }
                }
                GyakutenManager.inquiryName = id;
                GyakutenManager.scriptFile = GyakutenManager.player.PlayedScript.name; // remember the current script file
                GyakutenManager.success = false; // reset success
            }
        }
        else GyakutenManager.inquiryName = "";
        GyakutenManager.cur = 0;
        GyakutenManager.ShowInquiryOptions(true); // show inquiry interface
        Engine.GetService<IInputManager>().ProcessInput = false;
        return UniTask.CompletedTask;
    }
}

[CommandAlias("InquiryEnd")]
public class InquiryEnd : Command
{
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Debug.Log("InquiryEnd");
        GyakutenManager.ShowInquiryOptions(false);
        Engine.GetService<IInputManager>().ProcessInput = true;
        return UniTask.CompletedTask;
    }
}

[CommandAlias("Testimony")]
public class Testimony : Command
{
    public IntegerParameter num;
    public BooleanParameter shatterable;
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!Assigned(shatterable)) shatterable = false;
        Goto gtCmd = new Goto();
        gtCmd.Path = new NamedString(GyakutenManager.player.PlayedScript.name, GyakutenManager.inquiryName + num.ToString());
        return gtCmd.ExecuteAsync(cancellationToken);
    }
}
[CommandAlias("UnlockTestimony")]
public class UnlockTestimony : Command
{
    public StringParameter id;
    public IntegerParameter num;
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GyakutenManager.testimonies[num - 1].unlocked = true;
        return UniTask.CompletedTask;
    }
}

[CommandAlias("InquirySuccess")]
public class InquirySuccess : Command
{
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GyakutenManager.success = true;
        return UniTask.CompletedTask;
    }
}