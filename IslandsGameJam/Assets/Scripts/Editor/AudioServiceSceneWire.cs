using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds AudioService + SFX/Music AudioSources under MainGame's --- Managers --- and assigns on GameManager.
/// </summary>
public static class AudioServiceSceneWire
{
    const string MainGamePath = "Assets/Scenes/MainGame.unity";
    const string ManagersName = "--- Managers ---";

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

        var audioGo = FindOrCreateChild(managers, "AudioService");
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

        var gmSo = new SerializedObject(gameManager);
        gmSo.FindProperty("audioService").objectReferenceValue = audioService;
        gmSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AudioServiceSceneWire] Wired AudioService + SFX/Music AudioSources into MainGame.");
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
