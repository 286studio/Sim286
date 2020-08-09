using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;
using UniRx.Async;
using System.Threading;
using UniRx.Async.Triggers;
using UnityEngine.UI;

public class NaniFurniture : MonoBehaviour
{
    public static NaniFurniture ins;
    public Camera cam;
    public GameObject UI;
    public Button exit;

    private void Awake()
    {
        ins = this;
    }
    async void Start()
    {
        exit.onClick.AddListener(ExitToNaninovel);
        Show(false);
        await RuntimeInitializer.InitializeAsync();
        var player = Engine.GetService<IScriptPlayer>();
        await player.PreloadAndPlayAsync("FurnitureSystem");
    }
    private void OnValidate()
    {
        cam = GetComponentInChildren<Camera>();
    }
    public static void Show(bool b = true)
    {
        ins.cam.enabled = b;
        ins.UI.SetActive(b);
    }
    async void ExitToNaninovel()
    {
        NaniFurniture.Show(false);
        await Engine.GetService<IScriptPlayer>().PreloadAndPlayAsync("FurnitureSystem", label: "Finish");
        Engine.GetService<IInputManager>().ProcessInput = true;
    }
}

[CommandAlias("FurnitureMode")]
public class SwitchToAdventureMode : Naninovel.Commands.Command
{
    public override async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // 1. Disable Naninovel input.
        var inputManager = Engine.GetService<IInputManager>();
        inputManager.ProcessInput = false;

        // 2. Stop script player.
        var scriptPlayer = Engine.GetService<IScriptPlayer>();
        scriptPlayer.Stop();

        // 3. Reset state.
        var stateManager = Engine.GetService<IStateManager>();
        await stateManager.ResetStateAsync();

        // 4. Switch cameras.
        NaniFurniture.Show();
        Engine.GetService<ICameraManager>().Camera.enabled = false;
    }
}

