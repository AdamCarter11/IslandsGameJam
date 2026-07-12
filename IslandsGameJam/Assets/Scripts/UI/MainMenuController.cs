using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: Continue (if save exists) or New Game, then load MainGame with boot intent.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    const string MainGameSceneName = "MainGame";

    [SerializeField] Button continueButton;
    [SerializeField] Button newGameButton;
    [SerializeField] Button optionsButton;
    [SerializeField] OptionsPanelUI optionsPanel;

    void Start()
    {
        EnsureEventSystem();

        if (continueButton != null)
        {
            continueButton.interactable = SaveGameService.HasSave;
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinue);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGame);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptions);
        }
    }

    void OnContinue()
    {
        AudioService.Instance?.PlayUiClick();

        if (!SaveGameService.HasSave)
            return;

        SaveGameService.BootMode = BootMode.Load;
        SceneManager.LoadScene(MainGameSceneName);
    }

    void OnNewGame()
    {
        AudioService.Instance?.PlayUiClick();

        SaveGameService.DeleteSave();
        SaveGameService.BootMode = BootMode.New;
        SceneManager.LoadScene(MainGameSceneName);
    }

    void OnOptions()
    {
        AudioService.Instance?.PlayUiClick();
        optionsPanel?.Open();
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null
                && EventSystem.current.GetComponent<BaseInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

#if UNITY_EDITOR
    public void EditorAssign(Button continueBtn, Button newGameBtn, Button optionsBtn, OptionsPanelUI options)
    {
        continueButton = continueBtn;
        newGameButton = newGameBtn;
        optionsButton = optionsBtn;
        optionsPanel = options;
    }
#endif
}
