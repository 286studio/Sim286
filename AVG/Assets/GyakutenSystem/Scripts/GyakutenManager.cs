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
using Coffee.UIExtensions;

public class TestimonyData
{
    public string inquiryName { private set; get; }
    public int lineIdx { private set; get; }
    public bool hidden = false;
    public bool unlocked = false;

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
    public static int spawnCount;
    [SerializeField] Camera cam;
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
    public void NextTestimony()
    {
        do ++cur; while (cur < testimonies.Length && testimonies[cur].IsReallyHidden());
        if (cur >= testimonies.Length)
            player.PreloadAndPlayAsync(scriptFile, testimonies[testimonies.Length - 1].lineIdx + 1);
        else
            player.PreloadAndPlayAsync(scriptFile, testimonies[cur].lineIdx);
    }
    void Mada()
    {
        ShowInquiryOptions(false);
        player.PreloadAndPlayAsync(inquiryName + (cur + 1) + "_Mada");
        Engine.GetService<IInputManager>().ProcessInput = true;
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
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
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

[CommandAlias("StopMada")]
public class StopMada : Command
{
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        GyakutenManager.ShowInquiryOptions(true);
        Engine.GetService<IInputManager>().ProcessInput = false;
        GyakutenManager.ins.NextTestimony();
        return UniTask.CompletedTask;
    }
}

[CommandAlias("StopIgiari")]
public class StopIgiari : Command
{
    public override UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (GyakutenManager.success) GyakutenManager.player.PreloadAndPlayAsync(GyakutenManager.scriptFile, label: GyakutenManager.inquiryName + "END");
        else
        {
            GyakutenManager.ShowInquiryOptions(true);
            Engine.GetService<IInputManager>().ProcessInput = false;
            GyakutenManager.player.PreloadAndPlayAsync(GyakutenManager.scriptFile, GyakutenManager.testimonies[GyakutenManager.cur].lineIdx);
        }
        return UniTask.CompletedTask;
    }
}

[CommandAlias("Zhuijiukaishi")]
public class Zhuijiukaishi : Command
{
    public StringParameter Text;
    public override async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PrintText ptCmd = new PrintText();
        ptCmd.Text = Text;
        ptCmd.WaitForInput = false;
        Wait waitCmd = new Wait();
        waitCmd.WaitMode = Object.Instantiate(Resources.Load<GameObject>("Zhuijiukaishi"), GameObject.Find("Canvas").transform).GetComponent<DestroyInSecond>().destroyInSecond.ToString();
        await ptCmd.ExecuteAsync();
        await waitCmd.ExecuteAsync();
    }
}

[CommandAlias("GyakutenSpawn")]
public class GyakutenSpawn : Spawn
{
    public override async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Path += "#" + ++GyakutenManager.spawnCount;
        await base.ExecuteAsync();
    }
}