using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Wires AudioService + SFX/Music AudioSources into MainGame (under --- Managers ---)
/// and MainMenu (scene root). Assigns uiClickClip on MainMenu.
/// </summary>
public static class AudioServiceSceneWire
{
    const string MainGamePath = "Assets/Scenes/MainGame.unity";
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string ManagersName = "--- Managers ---";
    const string UiClickClipPath = "Assets/Sound/SFX/UI/SFX_UIGeneric11.wav";

    [MenuItem("Tools/Audio/Wire AudioService Into MainGame")]
    public static void WireMainGame()
    {
        var scene = EditorSceneManager.OpenScene(MainGamePath);

        var managers = FindManagersRoot();
        if (managers == null)
        {
            Debug.LogError("[AudioServiceSceneWire] No '--- Managers ---' root in MainGame.");
            return;
        }

        var gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[AudioServiceSceneWire] No GameManager in MainGame.");
            return;
        }

        var audioService = EnsureAudioServiceHierarchy(managers);

        var gmSo = new SerializedObject(gameManager);
        gmSo.FindProperty("audioService").objectReferenceValue = audioService;
        gmSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AudioServiceSceneWire] Wired AudioService + SFX/Music AudioSources into MainGame.");
    }

    [MenuItem("Tools/Audio/Wire AudioService Into MainMenu")]
    public static void WireMainMenu()
    {
        var scene = EditorSceneManager.OpenScene(MainMenuPath);

        var audioService = EnsureAudioServiceHierarchy(parent: null);

        var uiClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(UiClickClipPath);
        if (uiClickClip == null)
        {
            Debug.LogError($"[AudioServiceSceneWire] Missing ui click clip at {UiClickClipPath}");
            return;
        }

        var audioSo = new SerializedObject(audioService);
        audioSo.FindProperty("uiClickClip").objectReferenceValue = uiClickClip;
        // musicClip left null until a track is assigned in the inspector
        audioSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(audioService);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AudioServiceSceneWire] Wired AudioService + SFX/Music AudioSources into MainMenu (uiClickClip set, musicClip unset).");
    }

    public static void WireMainMenuBatch()
    {
        try
        {
            WireMainMenu();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AudioServiceSceneWire] WireMainMenu failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Creates AudioService with SFX/Music AudioSources. When <paramref name="parent"/> is null, creates at scene root.
    /// </summary>
    static AudioService EnsureAudioServiceHierarchy(Transform parent)
    {
        GameObject audioGo;
        if (parent != null)
        {
            audioGo = FindOrCreateChild(parent, "AudioService");
        }
        else
        {
            audioGo = FindOrCreateRoot("AudioService");
        }

        var audioService = audioGo.GetComponent<AudioService>();
        if (audioService == null)
            audioService = Undo.AddComponent<AudioService>(audioGo);

        var sfxGo = FindOrCreateChild(audioGo.transform, "SFX");
        var sfxSource = EnsureAudioSource(sfxGo, playOnAwake: false, loop: false);

        var musicGo = FindOrCreateChild(audioGo.transform, "Music");
        var musicSource = EnsureAudioSource(musicGo, playOnAwake: false, loop: true);

        var audioSo = new SerializedObject(audioService);
        audioSo.FindProperty("sfxSource").objectReferenceValue = sfxSource;
        audioSo.FindProperty("musicSource").objectReferenceValue = musicSource;
        audioSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(audioService);

        return audioService;
    }

    static Transform FindManagersRoot()
    {
        foreach (var root in sceneRoots())
        {
            if (root.name == ManagersName)
                return root;
        }

        return null;
    }

    static System.Collections.Generic.IEnumerable<Transform> sceneRoots()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
            yield return go.transform;
    }

    static GameObject FindOrCreateRoot(string name)
    {
        foreach (var root in sceneRoots())
        {
            if (root.name == name)
                return root.gameObject;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }

    static AudioSource EnsureAudioSource(GameObject go, bool playOnAwake, bool loop)
    {
        var source = go.GetComponent<AudioSource>();
        if (source == null)
            source = Undo.AddComponent<AudioSource>(go);

        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(source);
        return source;
    }
}
