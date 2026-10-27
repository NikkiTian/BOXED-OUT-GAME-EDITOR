using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ButtonForm : MonoBehaviour
{
    #region Modifiable Variables
    [Header("Function of the button")]
    // this will determine what the button will do
    public TypeOfButton Type;
    #endregion

    #region Misc
    // returns if the button is wired to a function onclick
    public bool IsDisabled { get { return isDisabled; } private set {; } }
    private bool isDisabled;

    private Button thisButton;
    #endregion 

    public enum TypeOfButton
    {
        Menu,
        Start,
        Continue,
        Instructions,
        Setup,
        Quit
    }

    private void Start()
    {
        Setup();
    }

    private void Update()
    {
        if (Type == TypeOfButton.Continue)
            UpdateContinueButton();
    }

    private void Setup()
    {
        thisButton = GetComponent<Button>();
        if (thisButton == null)
        {
            Debug.Log("Button component not found, cannot set onclick listener event");
            return;
        }

        switch (Type)
        {
            case TypeOfButton.Menu:

                thisButton?.onClick.AddListener(
                                            async () =>
                                            {
                                                if (GameManager.Instance == null)
                                                {
                                                    Debug.LogWarning("Game manager doesn't exist, cannot go to menu.");
                                                    return;
                                                }

                                                if (SoundManager.Instance != null)
                                                {
                                                    SoundManager.Instance.PlayButtonClickSFX();
                                                }

                                                int waitTimeMillis = 500;
                                                await GameManager.Instance.InsertPause(waitTimeMillis);

                                                GameManager.Instance.GoToMenu();
                                            });
                break;
            case TypeOfButton.Start:

                thisButton?.onClick.AddListener(
                                            async () =>
                                            {
                                                if (GameManager.Instance == null)
                                                {
                                                    Debug.LogWarning("Game manager doesn't exist, cannot start game.");
                                                    return;
                                                }

                                                if (SoundManager.Instance != null)
                                                {
                                                    SoundManager.Instance.PlayButtonClickSFX();
                                                }

                                                int waitTimeMillis = 500;
                                                await GameManager.Instance.InsertPause(waitTimeMillis);

                                                GameManager.Instance.StartNewGame();
                                            });
                break;
            case TypeOfButton.Continue:

                thisButton?.onClick.AddListener(
                                           async() =>
                                           { 
                                               if (GameManager.Instance == null)
                                               {
                                                   Debug.LogWarning("Game manager doesn't exist, cannot start game.");
                                                   return;
                                               }

                                               // set audio here before game manager resumes game
                                               if (SoundManager.Instance != null)
                                               {
                                                   SoundManager.Instance.PlayButtonClickSFX();
                                               }

                                               int waitTimeMillis = 500;
                                               await GameManager.Instance.InsertPause(waitTimeMillis);

                                               GameManager.Instance.ContinueGame();
                                           });
                break;
            case TypeOfButton.Instructions:

                thisButton?.onClick.AddListener(
                                         () =>
                                         {
                                             if (GameManager.Instance == null)
                                             {
                                                 Debug.LogWarning("Game manager doesn't exist, cannot load instructions panel.");
                                                 return;
                                             }

                                             if (SoundManager.Instance != null)
                                             {
                                                 SoundManager.Instance.PlayButtonClickSFX();
                                             }

                                             if (!GameManager.Instance.PanelOrNotificationOpen)
                                                 GameManager.Instance.LoadInstructionsPanel();
                                         });
                break;
            case TypeOfButton.Quit:
                thisButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
                break;
            case TypeOfButton.Setup:
                gameObject.SetActive(false);
                break;
        }
    }

    private void UpdateContinueButton()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("Game manager doesn't exist, cannot update this continue button");
            return;
        }

        if (!(GameManager.Instance.TrueCurrentSceneIndex == 0 && !isDisabled))
            return;

        Image img = GetComponent<Image>();
        if (img != null) 
            img.color = Color.gray;

        if (thisButton != null)
        {
            thisButton?.onClick.RemoveAllListeners();
            thisButton.enabled = false;
        }

        isDisabled = true;
    }

    private void OnDestroy()
    {
        thisButton?.onClick.RemoveAllListeners();
    }

    private void OnDisable()
    {
        if (thisButton != null)
        {
            thisButton.onClick.RemoveAllListeners();
            thisButton.enabled = false;
        }

        isDisabled = true;
    }
}
