using NUnit.Framework.Internal;
using System.Drawing.Text;
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
        Setup
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
                                            // delegate { //method or code here ;} also works, you just can't access parameters
                                            async () =>
                                            {
                                                if (GameManager.Instance == null)
                                                {
                                                    Debug.LogWarning("Game manager doesn't exist, cannot go to menu.");
                                                    return;
                                                }

                                                // set audio here before game manager goes back to menu
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
                                                if (GameManager.Instance == null || SerialTest.Instance == null)
                                                {
                                                    Debug.LogWarning("Game manager or serial test doesn't exist, cannot start game.");
                                                    return;
                                                }

                                                // in the case input is not filled, user will be prompted to fill out adequate information
                                                // technically don't need to do it for continue because serial test has already stored successful values
                                                if
                                                (!GameManager.Instance.EnableDevMode &&
                                                SerialTest.Instance.Port == string.Empty ||
                                                SerialTest.Instance.Baudrate == 0 ||
                                                SerialTest.Instance.ReadSpeed == 0 
                                                )
                                                {
                                                    GameManager.Instance.LoadInputNotification();
                                                    return;
                                                }

                                                // set audio here before game manager goes to new game
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
                                               if (GameManager.Instance == null || SerialTest.Instance == null)
                                               {
                                                   Debug.LogWarning("Game manager or serial test doesn't exist, cannot start game.");
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

                                             // set audio here before game manager resumes game
                                             if (SoundManager.Instance != null)
                                             {
                                                 SoundManager.Instance.PlayButtonClickSFX();
                                             }

                                             if (!GameManager.Instance.PanelOrNotificationOpen)
                                                 GameManager.Instance.LoadInstructionsPanel();
                                         });
                break;
            case TypeOfButton.Setup:

                thisButton?.onClick.AddListener(
                                        () =>
                                        {
                                            if (GameManager.Instance == null)
                                            {
                                                Debug.LogWarning("Game manager doesn't exist, cannot load setup panel.");
                                                return;
                                            }

                                            // set audio here before game manager resumes game
                                            if (SoundManager.Instance != null)
                                            {
                                                SoundManager.Instance.PlayButtonClickSFX();
                                            }

                                            if (!GameManager.Instance.PanelOrNotificationOpen)
                                                GameManager.Instance.LoadSetupPanel();
                                        });
                break;
        }
    }

    // this method only applies to the continue button, this is a method of pseudo disabling the button in case the player has not played the game yet
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
