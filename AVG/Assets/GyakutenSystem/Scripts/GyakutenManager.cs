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

public enum GyakutenState
{
    None,
    ShatteredTestimony,
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
    public static int inquiryStartLineIdx;
    public static int inquiryCurrentLineIdx;
    public static string scriptFile;
    public static List<int> hiddenTestimonies = new List<int>();
    public static Dictionary<string, HashSet<int>> unlockTestimonies = new Dictionary<string, HashSet<int>>();
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
        if (inquiryCurrentLineIdx > inquiryStartLineIdx)
        {
            do --inquiryCurrentLineIdx; while (hiddenTestimonies.Contains(inquiryCurrentLineIdx - inquiryStartLineIdx + 1)) ;
            player.PreloadAndPlayAsync(scriptFile, inquiryCurrentLineIdx);
        }
    }
    void NextTestimony()
    {
        do ++inquiryCurrentLineIdx; while (hiddenTestimonies.Contains(inquiryCurrentLineIdx - inquiryStartLineIdx + 1));
        player.PreloadAndPlayAsync(scriptFile, inquiryCurrentLineIdx);
    }
    void Mada()
    {
        int testimonyNum = inquiryCurrentLineIdx - inquiryStartLineIdx + 1;
        player.PreloadAndPlayAsync(inquiryName + testimonyNum + "_Mada");
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
        int testimonyNum = inquiryCurrentLineIdx - inquiryStartLineIdx + 1;
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
            player.PreloadAndPlayAsync(scriptFile, inquiryCurrentLineIdx);
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
        if (Assigned(id))
        {
            GyakutenManager.inquiryName = id;
            if (!GyakutenManager.unlockTestimonies.ContainsKey(id)) GyakutenManager.unlockTestimonies.Add(id, new HashSet<int>());
        }
        else GyakutenManager.inquiryName = "";
        GyakutenManager.success = false;
        GyakutenManager.ShowInquiryOptions(true);
        GyakutenManager.inquiryCurrentLineIdx = GyakutenManager.inquiryStartLineIdx = PlaybackSpot.LineIndex + 1;
        GyakutenManager.scriptFile = GyakutenManager.player.PlayedScript.name;
        GyakutenManager.hiddenTestimonies.Clear();
        foreach (var i in hidden) if (i != null && !GyakutenManager.unlockTestimonies[id].Contains(i)) GyakutenManager.hiddenTestimonies.Add(i);
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
        GyakutenManager.inquiryName = "";
        GyakutenManager.inquiryStartLineIdx = -1;
        GyakutenManager.inquiryCurrentLineIdx = -1;
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
        HashSet<int> val;
        if (GyakutenManager.unlockTestimonies.TryGetValue(GyakutenManager.inquiryName, out val))
        {
            if (val != null) val.Add(num);
            else
            {
                val = new HashSet<int>();
                val.Add(num);
            }
        }
        else
        {
            GyakutenManager.unlockTestimonies.Add(GyakutenManager.inquiryName, new HashSet<int>());
            GyakutenManager.unlockTestimonies[GyakutenManager.inquiryName].Add(num);
        }
        if (GyakutenManager.inquiryName == id) GyakutenManager.hiddenTestimonies.Remove(num);
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