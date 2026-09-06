#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Yobitsugi.Core;
using Yobitsugi.Interactables;
using Yobitsugi.Player;
using Yobitsugi.UI;
using Yobitsugi.VisualNovel;

/// <summary>
/// Regenerates the playable base scene from scratch.
/// Scenario assets under <see cref="VNScenesFolder"/> are only created when missing, so hand-authored text survives a rebuild.
/// </summary>
public static class YobitsugiSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Yobitsugi_Base.unity";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string VNScenesFolder = "Assets/Resources/VNScenes";
    private const string VolumeProfilePath = "Assets/Settings/YobitsugiVolumeProfile.asset";
    private const int RequiredClueCount = 3;

    private static Transform systemsRoot;
    private static Transform uiRoot;
    private static Transform levelRoot;
    private static Transform actorsRoot;
    private static Transform eventsRoot;

    [MenuItem("Yobitsugi/Build Base Scene")]
    public static void BuildBaseScene()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "ベースシーンを再生成",
            $"{ScenePath} を作り直します。\n手作業で加えたシーン上の変更は失われます。\n" +
            $"(※ {VNScenesFolder} のシナリオアセットは上書きされません)",
            "再生成する", "キャンセル");
        if (!confirmed) return;

        BuildBaseSceneImmediate();
    }

    /// <summary>Same as the menu item but without the confirmation prompt, for scripted/automated rebuilds.</summary>
    public static void BuildBaseSceneImmediate()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        systemsRoot = CreateGroup("[Systems]");
        uiRoot = CreateGroup("[UI]");
        levelRoot = CreateGroup("[Level]");
        actorsRoot = CreateGroup("[Actors]");
        eventsRoot = CreateGroup("[Events]");

        BuildLighting();
        BuildEnvironment();
        var player = BuildPlayer();
        BuildClues();
        var hudCanvas = BuildGameManagerAndUI(player);
        BuildVisualNovel(player, hudCanvas);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        RegisterInBuildSettings();
        Debug.Log($"Yobitsugi base scene created at {ScenePath}");
    }

    private static Transform CreateGroup(string name) => new GameObject(name).transform;

    private static void RegisterInBuildSettings()
    {
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
        {
            if (s.path == ScenePath) return;
        }

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        updated[0] = new EditorBuildSettingsScene(ScenePath, true);
        existing.CopyTo(updated, 1);
        EditorBuildSettings.scenes = updated;
    }

    private static void BuildLighting()
    {
        var lightGO = new GameObject("Directional Light");
        lightGO.transform.SetParent(levelRoot, false);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.75f, 0.55f);
        light.intensity = 0.8f;
        lightGO.transform.rotation = Quaternion.Euler(35f, 145f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.13f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.35f, 0.3f, 0.32f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.02f;

        BuildPostProcessingVolume();
    }

    private static void BuildPostProcessingVolume()
    {
        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            Debug.LogWarning($"Volume profile not found at {VolumeProfilePath}; skipping post-processing setup.");
            return;
        }

        var volumeGO = new GameObject("Global Volume");
        volumeGO.transform.SetParent(levelRoot, false);

        var volume = volumeGO.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
    }

    private static void BuildEnvironment()
    {
        var root = new GameObject("Environment");
        root.transform.SetParent(levelRoot, false);

        CreateBlock("Floor", root.transform, new Vector3(0f, -0.1f, 9f), new Vector3(16f, 0.2f, 30f));
        CreateBlock("Wall_North", root.transform, new Vector3(0f, 2f, 24f), new Vector3(16f, 4f, 0.3f));
        CreateBlock("Wall_South", root.transform, new Vector3(0f, 2f, -6f), new Vector3(16f, 4f, 0.3f));
        CreateBlock("Wall_East", root.transform, new Vector3(8f, 2f, 9f), new Vector3(0.3f, 4f, 30f));
        CreateBlock("Wall_West", root.transform, new Vector3(-8f, 2f, 9f), new Vector3(0.3f, 4f, 30f));

        CreateBlock("Wall_Divide_Left", root.transform, new Vector3(-4.5f, 2f, 9f), new Vector3(7f, 4f, 0.3f));
        CreateBlock("Wall_Divide_Right", root.transform, new Vector3(4.5f, 2f, 9f), new Vector3(7f, 4f, 0.3f));

        BuildDoor(root.transform);
        BuildExit(root.transform);
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
        return go;
    }

    private static void BuildDoor(Transform parent)
    {
        var hinge = new GameObject("Door_Hinge");
        hinge.transform.SetParent(parent, false);
        hinge.transform.position = new Vector3(-1f, 0f, 9f);

        var doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorPanel.name = "Door_Panel";
        doorPanel.transform.SetParent(hinge.transform, false);
        doorPanel.transform.localPosition = new Vector3(1f, 2f, 0f);
        doorPanel.transform.localScale = new Vector3(2f, 4f, 0.15f);

        var door = hinge.AddComponent<LockedDoor>();
        var so = new SerializedObject(door);
        so.FindProperty("requiredClueCount").intValue = RequiredClueCount;
        so.FindProperty("blockingCollider").objectReferenceValue = doorPanel.GetComponent<Collider>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildExit(Transform parent)
    {
        var exit = new GameObject("ExitZone");
        exit.transform.SetParent(parent, false);
        exit.transform.position = new Vector3(0f, 1.5f, 22f);

        var col = exit.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 3f, 1f);

        var zone = exit.AddComponent<ExitZone>();
        var so = new SerializedObject(zone);
        so.FindProperty("requiredClueCount").intValue = RequiredClueCount;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildPlayer()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.SetParent(actorsRoot, false);
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(0f, 0.1f, -3f);

        var controller = playerGO.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.radius = 0.35f;

        var cameraGO = new GameObject("CameraPivot");
        cameraGO.transform.SetParent(playerGO.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        var cam = cameraGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cameraGO.AddComponent<AudioListener>();
        cameraGO.tag = "MainCamera";

        var playerController = playerGO.AddComponent<PlayerController>();
        var pcSO = new SerializedObject(playerController);
        pcSO.FindProperty("cameraPivot").objectReferenceValue = cameraGO.transform;
        pcSO.ApplyModifiedPropertiesWithoutUndo();

        var interactor = playerGO.AddComponent<PlayerInteractor>();
        var interactorSO = new SerializedObject(interactor);
        interactorSO.FindProperty("interactionCamera").objectReferenceValue = cam;
        interactorSO.ApplyModifiedPropertiesWithoutUndo();

        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var playerInput = playerGO.AddComponent<PlayerInput>();
        playerInput.actions = actions;
        playerInput.defaultActionMap = "Player";
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;

        return playerGO;
    }

    private static void BuildClues()
    {
        var root = new GameObject("Clues");
        root.transform.SetParent(levelRoot, false);

        CreateClue(root.transform, "Clue_01", new Vector3(-5f, 0.5f, -3f));
        CreateClue(root.transform, "Clue_02", new Vector3(5f, 0.5f, -1f));
        CreateClue(root.transform, "Clue_03", new Vector3(-3f, 0.5f, 5f));
    }

    private static void CreateClue(Transform parent, string id, Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = id;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.35f;

        var clue = go.AddComponent<ClueItem>();
        var so = new SerializedObject(clue);
        so.FindProperty("clueId").stringValue = id;
        so.FindProperty("displayName").stringValue = "手がかり";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildGameManagerAndUI(GameObject player)
    {
        var gmGO = new GameObject("GameManager");
        gmGO.transform.SetParent(systemsRoot, false);
        var gm = gmGO.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("requiredClueCount").intValue = RequiredClueCount;
        gmSO.ApplyModifiedPropertiesWithoutUndo();

        var esGO = new GameObject("EventSystem");
        esGO.transform.SetParent(systemsRoot, false);
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        var canvasGO = CreateCanvas("HUD Canvas", 0);

        var clueText = CreateText("ClueCounterText", canvasGO.transform, "手がかり: 0 / 3",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(320f, 40f),
            24, TextAnchor.UpperLeft);

        CreateText("Crosshair", canvasGO.transform, "・",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f),
            28, TextAnchor.MiddleCenter);

        var promptPanel = CreateUIObject("InteractionPromptPanel", canvasGO.transform);
        AddRect(promptPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 44f));
        var promptBg = promptPanel.AddComponent<Image>();
        promptBg.color = new Color(0f, 0f, 0f, 0.55f);
        var promptTextComp = CreateText("PromptText", promptPanel.transform, "調べる [E]",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter);
        promptPanel.SetActive(false);

        var clearPanel = CreateFullScreenPanel("ClearPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.85f));
        CreateText("ClearText", clearPanel.transform, "町から抜け出した",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f),
            36, TextAnchor.MiddleCenter);
        clearPanel.SetActive(false);

        var counterUI = canvasGO.AddComponent<ClueCounterUI>();
        var counterSO = new SerializedObject(counterUI);
        counterSO.FindProperty("counterText").objectReferenceValue = clueText;
        counterSO.ApplyModifiedPropertiesWithoutUndo();

        var interactor = player.GetComponent<PlayerInteractor>();
        var promptUI = canvasGO.AddComponent<InteractionPromptUI>();
        var promptSO = new SerializedObject(promptUI);
        promptSO.FindProperty("interactor").objectReferenceValue = interactor;
        promptSO.FindProperty("panel").objectReferenceValue = promptPanel;
        promptSO.FindProperty("promptText").objectReferenceValue = promptTextComp;
        promptSO.ApplyModifiedPropertiesWithoutUndo();

        var stateUI = canvasGO.AddComponent<GameStateUI>();
        var stateSO = new SerializedObject(stateUI);
        stateSO.FindProperty("clearPanel").objectReferenceValue = clearPanel;
        stateSO.ApplyModifiedPropertiesWithoutUndo();

        return canvasGO;
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        var canvasGO = new GameObject(name, typeof(RectTransform));
        canvasGO.transform.SetParent(uiRoot, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform AddRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return rt;
    }

    private static Text CreateText(string name, Transform parent, string content, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
    {
        var go = CreateUIObject(name, parent);
        AddRect(go, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = content;
        return text;
    }

    private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
    {
        var go = CreateUIObject(name, parent);
        AddRect(go, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPos)
    {
        var go = CreateUIObject(name, parent);
        AddRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(220f, 56f));
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        var btn = go.AddComponent<Button>();
        CreateText(name + "Label", go.transform, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            22, TextAnchor.MiddleCenter);
        return btn;
    }

    private static GameObject CreateVerticalList(string name, Transform parent, Vector2 size)
    {
        var go = CreateUIObject(name, parent);
        AddRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;
        return go;
    }

    private static void BuildVisualNovel(GameObject player, GameObject hudCanvas)
    {
        var canvasGO = CreateCanvas("VN Canvas", 10);

        var background = CreateFullScreenPanel("Background", canvasGO.transform, new Color(0.05f, 0.05f, 0.07f, 1f));
        var backgroundImage = background.GetComponent<Image>();

        var backgroundFade = CreateFullScreenPanel("BackgroundFade", canvasGO.transform, Color.white);
        var backgroundFadeImage = backgroundFade.GetComponent<Image>();
        backgroundFadeImage.raycastTarget = false;
        backgroundFadeImage.enabled = false;

        // Portrait stage sits above the background but below the text panel and choices.
        var stage = CreateUIObject("PortraitStage", canvasGO.transform);
        AddRect(stage, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var portraitTemplate = CreateUIObject("PortraitTemplate", stage.transform);
        AddRect(portraitTemplate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(700f, 1000f));
        var portraitImage = portraitTemplate.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portraitTemplate.SetActive(false);

        var portraitViewGO = new GameObject("VNPortraitView");
        portraitViewGO.transform.SetParent(systemsRoot, false);
        var portraitView = portraitViewGO.AddComponent<VNPortraitView>();
        var portraitSO = new SerializedObject(portraitView);
        portraitSO.FindProperty("stage").objectReferenceValue = stage.GetComponent<RectTransform>();
        portraitSO.FindProperty("portraitTemplate").objectReferenceValue = portraitImage;
        portraitSO.ApplyModifiedPropertiesWithoutUndo();

        var advanceButton = CreateUIObject("AdvanceButton", canvasGO.transform);
        AddRect(advanceButton, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        advanceButton.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var advanceBtn = advanceButton.AddComponent<Button>();

        var textPanel = CreateUIObject("TextPanel", canvasGO.transform);
        AddRect(textPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(-80f, 240f));
        textPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        var textPanelGroup = textPanel.AddComponent<CanvasGroup>();

        var speakerText = CreateText("SpeakerText", textPanel.transform, "",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(-48f, 36f),
            22, TextAnchor.UpperLeft);
        speakerText.fontStyle = FontStyle.Bold;

        var dialogueText = CreateText("DialogueText", textPanel.transform, "",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(-48f, -50f),
            24, TextAnchor.UpperLeft);

        var nextIndicator = CreateText("NextIndicator", textPanel.transform, "▼",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 16f), new Vector2(32f, 32f),
            22, TextAnchor.MiddleCenter);
        nextIndicator.raycastTarget = false;
        nextIndicator.gameObject.AddComponent<BlinkGraphic>();
        nextIndicator.gameObject.SetActive(false);

        var choicesContainer = CreateUIObject("ChoicesContainer", canvasGO.transform);
        AddRect(choicesContainer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(640f, 220f));
        var layout = choicesContainer.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.spacing = 10f;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;

        var choiceTemplate = CreateButton("ChoiceButtonTemplate", choicesContainer.transform, "選択肢", Vector2.zero);
        choiceTemplate.gameObject.SetActive(false);

        var vnUiGO = new GameObject("VNView");
        vnUiGO.transform.SetParent(systemsRoot, false);
        var vnUi = vnUiGO.AddComponent<VNUI>();
        var vnUiSO = new SerializedObject(vnUi);
        vnUiSO.FindProperty("root").objectReferenceValue = canvasGO;
        vnUiSO.FindProperty("backgroundImage").objectReferenceValue = backgroundImage;
        vnUiSO.FindProperty("backgroundFadeImage").objectReferenceValue = backgroundFadeImage;
        vnUiSO.FindProperty("textPanelGroup").objectReferenceValue = textPanelGroup;
        vnUiSO.FindProperty("speakerText").objectReferenceValue = speakerText;
        vnUiSO.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        vnUiSO.FindProperty("advanceButton").objectReferenceValue = advanceBtn;
        vnUiSO.FindProperty("nextIndicator").objectReferenceValue = nextIndicator.gameObject;
        vnUiSO.FindProperty("choicesContainer").objectReferenceValue = choicesContainer.transform;
        vnUiSO.FindProperty("choiceButtonTemplate").objectReferenceValue = choiceTemplate;
        vnUiSO.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        vnUiSO.ApplyModifiedPropertiesWithoutUndo();

        var vnManagerGO = new GameObject("VNManager");
        vnManagerGO.transform.SetParent(systemsRoot, false);
        var vnManager = vnManagerGO.AddComponent<VNManager>();
        var vnManagerSO = new SerializedObject(vnManager);
        vnManagerSO.FindProperty("view").objectReferenceValue = vnUi;
        vnManagerSO.FindProperty("portraitView").objectReferenceValue = portraitView;
        vnManagerSO.ApplyModifiedPropertiesWithoutUndo();

        var introScene = EnsureVNScene("Intro", () => new[]
        {
            NewLine("ミオ", "……ここ、本当にナギが写真を送ってきた駅?"),
            NewLine("ミオ", "誰もいない。なのに電気は点いてるし、信号機も動いてる。"),
            NewLine("ミオ", "とにかく、手がかりを探さないと。"),
        });

        var sakuScene = EnsureVNScene("SakuEncounter", () => new[]
        {
            NewLine("サク", "おいおい、こんな時間にうろついてるのはあんたが初めてだ。"),
            NewLine("サク", "この町、出口が見つかるまでは何度も同じ場所に戻される。"),
            NewLine("ミオ", "……あなたも、閉じ込められてるの?", new[]
            {
                new VNChoice { text = "「手伝ってくれるの?」", nextLineIndex = 3 },
                new VNChoice { text = "「一人で大丈夫」", nextLineIndex = 4 },
            }),
            NewLine("サク", "手がかりを3つ集めれば、鍵のかかった場所が開くはずだ。手伝うよ。"),
            NewLine("サク", "……まあ、無理はするなよ。何かあったら隠れる場所くらいは教えとく。"),
        });

        var gameModeGO = new GameObject("GameModeManager");
        gameModeGO.transform.SetParent(systemsRoot, false);
        var gameMode = gameModeGO.AddComponent<GameModeManager>();
        var gameModeSO = new SerializedObject(gameMode);
        gameModeSO.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        gameModeSO.FindProperty("vnManager").objectReferenceValue = vnManager;
        gameModeSO.FindProperty("introScene").objectReferenceValue = introScene;
        gameModeSO.ApplyModifiedPropertiesWithoutUndo();

        // HUD visibility follows the mode through events instead of a reference held by GameModeManager.
        var hudVisibility = gameModeGO.AddComponent<ModeVisibility>();
        var hudVisibilitySO = new SerializedObject(hudVisibility);
        hudVisibilitySO.FindProperty("target").objectReferenceValue = hudCanvas;
        hudVisibilitySO.FindProperty("visibleInVN").boolValue = false;
        hudVisibilitySO.FindProperty("visibleInExploration").boolValue = true;
        hudVisibilitySO.ApplyModifiedPropertiesWithoutUndo();

        var flagsGO = new GameObject("StoryFlags");
        flagsGO.transform.SetParent(systemsRoot, false);
        flagsGO.AddComponent<StoryFlags>();

        var saveCoordinatorGO = new GameObject("SaveCoordinator");
        saveCoordinatorGO.transform.SetParent(systemsRoot, false);
        var saveCoordinator = saveCoordinatorGO.AddComponent<SaveCoordinator>();

        BuildAudioService();

        BuildVNTrigger("VNTrigger_SakuEncounter", new Vector3(0f, 1.2f, 14f), new Vector3(6f, 3f, 3f), sakuScene);
        BuildSystemMenu(vnManager, gameMode, saveCoordinator);
        BuildScreenFader();
    }

    private static void BuildVNTrigger(string name, Vector3 position, Vector3 size, VNScene scene)
    {
        var triggerGO = new GameObject(name);
        triggerGO.transform.SetParent(eventsRoot, false);
        triggerGO.transform.position = position;

        var col = triggerGO.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        var trigger = triggerGO.AddComponent<VNTriggerZone>();
        var so = new SerializedObject(trigger);
        so.FindProperty("scene").objectReferenceValue = scene;
        so.FindProperty("oneShot").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildAudioService()
    {
        var audioGO = new GameObject("AudioService");
        audioGO.transform.SetParent(systemsRoot, false);

        var music = CreateAudioSource(audioGO.transform, "Music");
        var ambience = CreateAudioSource(audioGO.transform, "Ambience");
        var sfx = CreateAudioSource(audioGO.transform, "SFX");

        var service = audioGO.AddComponent<Yobitsugi.Audio.AudioService>();
        var so = new SerializedObject(service);
        so.FindProperty("musicSource").objectReferenceValue = music;
        so.FindProperty("ambienceSource").objectReferenceValue = ambience;
        so.FindProperty("sfxSource").objectReferenceValue = sfx;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AudioSource CreateAudioSource(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void BuildScreenFader()
    {
        var canvasGO = CreateCanvas("Fade Canvas", 100);
        var image = canvasGO.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        AddRect(canvasGO, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        canvasGO.AddComponent<CanvasGroup>();
        canvasGO.AddComponent<ScreenFader>();
    }

    private static void BuildSystemMenu(VNManager vnManager, GameModeManager gameModeManager, SaveCoordinator saveCoordinator)
    {
        var canvasGO = CreateCanvas("System Menu Canvas", 20);

        var menuButton = CreateButton("MenuButton", canvasGO.transform, "≡", Vector2.zero);
        AddRect(menuButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(56f, 56f));

        var menuPanel = CreateFullScreenPanel("MenuPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.75f));
        var menuButtons = CreateVerticalList("Buttons", menuPanel.transform, new Vector2(360f, 460f));
        var autoButton = CreateButton("AutoButton", menuButtons.transform, "オート: OFF", Vector2.zero);
        var skipButton = CreateButton("SkipButton", menuButtons.transform, "スキップ: OFF", Vector2.zero);
        var logButton = CreateButton("LogButton", menuButtons.transform, "ログ", Vector2.zero);
        var saveButton = CreateButton("SaveButton", menuButtons.transform, "セーブ", Vector2.zero);
        var loadButton = CreateButton("LoadButton", menuButtons.transform, "ロード", Vector2.zero);
        var restartButton = CreateButton("RestartButton", menuButtons.transform, "最初から", Vector2.zero);
        var closeMenuButton = CreateButton("CloseButton", menuButtons.transform, "閉じる", Vector2.zero);
        menuPanel.SetActive(false);

        var backlogPanel = CreateFullScreenPanel("BacklogPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.85f));
        var backlogText = CreateText("BacklogText", backlogPanel.transform, "",
            new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            20, TextAnchor.UpperLeft);
        var closeBacklogButton = CreateButton("CloseBacklogButton", backlogPanel.transform, "閉じる", Vector2.zero);
        AddRect(closeBacklogButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 60f), new Vector2(220f, 56f));
        backlogPanel.SetActive(false);

        var slotPanel = CreateFullScreenPanel("SlotPanel", canvasGO.transform, new Color(0f, 0f, 0f, 0.85f));
        var slotTitle = CreateText("SlotPanelTitle", slotPanel.transform, "セーブ",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(400f, 50f),
            28, TextAnchor.MiddleCenter);

        var slotList = CreateVerticalList("SlotButtons", slotPanel.transform, new Vector2(520f, 320f));
        var slotButtons = new Button[SaveSystem.SlotCount];
        var slotLabels = new Text[SaveSystem.SlotCount];
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            var slotButton = CreateButton($"SlotButton_{i}", slotList.transform, $"スロット{i + 1}: (空)", Vector2.zero);
            AddRect(slotButton.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(460f, 56f));
            slotButtons[i] = slotButton;
            slotLabels[i] = slotButton.GetComponentInChildren<Text>();
        }
        var closeSlotButton = CreateButton("CloseSlotButton", slotList.transform, "閉じる", Vector2.zero);
        slotPanel.SetActive(false);

        var systemMenuGO = new GameObject("SystemMenuView");
        systemMenuGO.transform.SetParent(systemsRoot, false);
        var systemMenu = systemMenuGO.AddComponent<SystemMenuView>();
        var so = new SerializedObject(systemMenu);
        so.FindProperty("menuPanel").objectReferenceValue = menuPanel;
        so.FindProperty("backlogPanel").objectReferenceValue = backlogPanel;
        so.FindProperty("slotPanel").objectReferenceValue = slotPanel;
        so.FindProperty("menuButton").objectReferenceValue = menuButton;
        so.FindProperty("autoButton").objectReferenceValue = autoButton;
        so.FindProperty("skipButton").objectReferenceValue = skipButton;
        so.FindProperty("backlogButton").objectReferenceValue = logButton;
        so.FindProperty("saveButton").objectReferenceValue = saveButton;
        so.FindProperty("loadButton").objectReferenceValue = loadButton;
        so.FindProperty("restartButton").objectReferenceValue = restartButton;
        so.FindProperty("autoButtonLabel").objectReferenceValue = autoButton.GetComponentInChildren<Text>();
        so.FindProperty("skipButtonLabel").objectReferenceValue = skipButton.GetComponentInChildren<Text>();
        so.FindProperty("backlogText").objectReferenceValue = backlogText;
        so.FindProperty("slotPanelTitle").objectReferenceValue = slotTitle;
        so.FindProperty("vnManager").objectReferenceValue = vnManager;
        so.FindProperty("gameModeManager").objectReferenceValue = gameModeManager;
        so.FindProperty("saveCoordinator").objectReferenceValue = saveCoordinator;
        so.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        var backButtons = new[] { closeMenuButton, closeBacklogButton, closeSlotButton };
        var backButtonsProp = so.FindProperty("backButtons");
        backButtonsProp.arraySize = backButtons.Length;
        for (int i = 0; i < backButtons.Length; i++)
            backButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = backButtons[i];

        var slotButtonsProp = so.FindProperty("slotButtons");
        slotButtonsProp.arraySize = slotButtons.Length;
        var slotLabelsProp = so.FindProperty("slotButtonLabels");
        slotLabelsProp.arraySize = slotLabels.Length;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            slotButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotButtons[i];
            slotLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotLabels[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static VNLine NewLine(string speaker, string text, VNChoice[] choices = null)
    {
        return new VNLine { speaker = speaker, text = text, choices = choices };
    }

    /// <summary>Loads the scenario asset, creating it with sample content only when it does not exist yet.</summary>
    private static VNScene EnsureVNScene(string assetName, System.Func<VNLine[]> sampleLines)
    {
        System.IO.Directory.CreateDirectory(VNScenesFolder);
        string path = $"{VNScenesFolder}/{assetName}.asset";

        var scene = AssetDatabase.LoadAssetAtPath<VNScene>(path);
        if (scene != null) return scene;

        scene = ScriptableObject.CreateInstance<VNScene>();
        scene.lines = sampleLines();
        AssetDatabase.CreateAsset(scene, path);
        EditorUtility.SetDirty(scene);
        return scene;
    }
}
#endif
