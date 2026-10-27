using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
#if UNITY_EDITOR
    public static string EditorPreviewPath;
    public void BeginEditorPreview(string path)
    {
        EditorPreviewPath = path;
        currentSceneIndex = 1; trueCurrentSceneIndex = 1;
        currentGameStage = GameStage.InGame;
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
    }
#endif
    #region Instance Setup 
    public static GameManager Instance { get; private set; }
    #endregion

    #region Modifiable Variaibles
    [Header("UI Related Items")]
    public GameObject PausePanelPrefab;
    public GameObject SetupPanelPrefab;
    public GameObject InstructionsPanelPrefab;
    // a simple warning to the user to setup the serial port properly
    public GameObject SetupFailedNotificationPrefab;
    // a simple warning to the user to fill in the fields for serial port setup
    public GameObject InputNotificationPrefab;

    [Header("Dev Tools")]
    // this just allows me to bypass certain things in the code so I can just speedrun through the whole thing to check for issues
    public bool EnableDevMode;

    [Header("Scenes")]
    [Tooltip("I recommend placing the scenes in chronological order (so menu should be the zeroth index)")]
    public string[] Scenes;
    #endregion

    #region Misc
    // reference to the pause panel (if we instantiate it)
    private GameObject pausePanelPrefabInstance;

    // reference to the other panels in menu (if we instantiate it)
    private GameObject setupPanelPrefabInstance;
    private GameObject instructionsPanelPrefabInstance;
    
    // reference to warnings on the screen 
    private GameObject setupFailedNotificationPrefabInstance, inputNotificationPrefabInstance;

    // accessible variable that returns whether any panel or notification is open or not
    public bool PanelOrNotificationOpen 
    { 
        get 
        {
            return
                pausePanelPrefabInstance != null ||
                setupPanelPrefabInstance != null ||
                instructionsPanelPrefabInstance != null ||
                setupFailedNotificationPrefabInstance != null ||
                inputNotificationPrefabInstance != null;
        } 
        private set {; } 
    }

    // a state machine based enum for the game manager to utilize
    public GameStage CurrentGameStage { get { return currentGameStage; } private set {; } }
    private GameStage currentGameStage = GameStage.Menu;


    // we need some system controls that can access UI and game changing states
    private PlayerInputActionsBase inputSystemControls;
    private InputAction undo, exit, restart;

    // currentSceneIndex returns what the player is currently at, trueCurrentSceneIndex stores the level the player is at before (if they continue their gaming instead of starting new)
    public int CurrentSceneIndex { get { return currentSceneIndex; } private set {; } }
    public int TrueCurrentSceneIndex { get { return trueCurrentSceneIndex; } private set { }  }
    private int currentSceneIndex = 0, trueCurrentSceneIndex = 0;
    #endregion

    public enum GameStage
    {
        Menu,
        InGame,
        End
    }

    // setup methods (PROPRIETARY) //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        Init();
    }

    // slightly modified singleton instance for a persistent obj throughout all scenes
    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // Persistent objects must be roots. Keep the input/game state alive
            // when the menu scene is replaced by a level.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        inputSystemControls = new PlayerInputActionsBase();
        undo = inputSystemControls.Player.Undo;
        exit = inputSystemControls.Player.Exit;
        restart = inputSystemControls.Player.Restart;

        undo.Enable();
        exit.Enable();
        restart.Enable();

        // Keep the cursor free to leave the game window.
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDisable()
    {
        undo?.Disable();
        exit?.Disable();
        restart?.Disable();
    }

    private void Update()
    {
        switch (currentGameStage)
        {
            case GameStage.Menu:

                UpdateMenuStage();
                break;
            case GameStage.InGame:

                UpdateInGameStage();
                break;
            case GameStage.End:

                UpdateEndGameStage();
                break;
        }
    }

    private void UpdateMenuStage()
    {
        CheckForExits();

        // play music here
        if (SoundManager.Instance != null && !SoundManager.Instance.MusicIsPlaying)
        {
            SoundManager.Instance.PlayMenuMusic();
        }
    }

    private void UpdateInGameStage()
    {
        CheckForUndos();
        CheckForRestarts();
        CheckForExits();

        // play music here
        if (SoundManager.Instance != null && !SoundManager.Instance.MusicIsPlaying)
        {
            SoundManager.Instance.PlayGameMusic();
        }
    }

    private void UpdateEndGameStage()
    {
        CheckForExits();

        // play music here
        if (SoundManager.Instance != null && !SoundManager.Instance.MusicIsPlaying)
        {
            SoundManager.Instance.PlayEndingMusic();
        }
    }

    // scene transition and related methods //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UpdateScene()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(EditorPreviewPath)) { UnityEditor.EditorApplication.isPlaying = false; return; }
#endif
        currentSceneIndex++;
        trueCurrentSceneIndex++;

        SceneManager.LoadScene(Scenes[currentSceneIndex]);
        UpdateGameStage();

        Debug.Log("Scene is loading...");
    }

    public void StartNewGame()
    {
        trueCurrentSceneIndex = 0;
        currentSceneIndex = 0;
        UpdateScene();
    }

    public void ContinueGame()
    {
        currentSceneIndex = trueCurrentSceneIndex;
        SceneManager.LoadScene(Scenes[currentSceneIndex]);
        UpdateGameStage();
    }

    public void GoToMenu()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(EditorPreviewPath)) { UnityEditor.EditorApplication.isPlaying = false; return; }
#endif
        trueCurrentSceneIndex = currentSceneIndex;
        currentSceneIndex = 0;
        SceneManager.LoadScene(Scenes[currentSceneIndex]);
        UpdateGameStage();

        Debug.Log("Loading menu scene");
    }

    private void UpdateGameStage()
    {
        if (currentSceneIndex == 0)
            currentGameStage = GameStage.Menu;
        else if (currentSceneIndex == Scenes.Length - 1)
            currentGameStage = GameStage.End;
        else
            currentGameStage = GameStage.InGame;
    }

    // input check methods //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void CheckForUndos()
    {
        if (LevelManager.Instance == null)
        {
            Debug.Log("Level manager instance doesn't exist, cannot undo moves.");
            return;
        }

        if (undo.WasPressedThisFrame())
        {
            Debug.Log("Move has been undone");

            LevelManager.Instance.RevertToPreviousState();
        }
    }

    private void CheckForRestarts()
    {
        if (restart.WasPressedThisFrame())
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(EditorPreviewPath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(EditorPreviewPath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.Log($"Scene {Scenes[currentSceneIndex]} is going to be reloaded");

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void CheckForExits()
    {
        if (exit.WasPressedThisFrame())
        {
            // feature is exclusively for pause panel 
            if (!PanelOrNotificationOpen)
            {
                Debug.Log("Game paused");

                LoadPausePanel();
                return;
            }

            // btw Destroy() is null safe so I dont have to worry about any errors thrown
            if (pausePanelPrefabInstance != null)
                ClosePausePanel();
            // for setup panel
            if (setupPanelPrefabInstance != null)
                CloseSetupPanel();
            // for instructions panel
            if (instructionsPanelPrefabInstance != null)
                CloseInstructionsPanel();
            // for setup failed notification
            if (setupFailedNotificationPrefabInstance != null)
                CloseSetupFailedNotification();
            if (inputNotificationPrefabInstance != null)
                // for input notification
                CloseInputNotification();

        }
    }

    // miscellaneous methods //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Task InsertPause(int waitTime)
    {
        return Task.Delay(waitTime);
    }

    public void LoadPausePanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        pausePanelPrefabInstance = Instantiate(PausePanelPrefab, canvas.transform);


        // play panel open sound here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPanelOpenAndCloseSFX();

            if (currentGameStage == GameStage.InGame)
                SoundManager.Instance.PauseGameMusic();
            else if (currentGameStage == GameStage.End)
                SoundManager.Instance.PauseEndingMusic();
        }
    }

    // Retain the public entry point for existing serialized UI references.
    // External-device setup has been retired; keyboard controls need no setup.
    public void LoadSetupPanel()
    {
        LoadInstructionsPanel();
    }

    public void LoadInstructionsPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        instructionsPanelPrefabInstance = Instantiate(InstructionsPanelPrefab, canvas.transform);

        // play panel open sound here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPanelOpenAndCloseSFX();
        }
    }

    public void LoadSetupFailedNotification()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        setupFailedNotificationPrefabInstance = Instantiate(SetupFailedNotificationPrefab, canvas.transform);
    }

    public void LoadInputNotification()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        inputNotificationPrefabInstance = Instantiate(InputNotificationPrefab, canvas.transform);
    }

    public void ClosePausePanel()
    {
        Destroy(pausePanelPrefabInstance);

        // play panel close sound here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPanelOpenAndCloseSFX();

            if (currentGameStage == GameStage.InGame)
                SoundManager.Instance.UnpauseGameMusic();
            else if (currentGameStage == GameStage.End)
                SoundManager.Instance.UnpauseEndingMusic();
        }
    }

    public void CloseSetupPanel()
    {
        Destroy(setupPanelPrefabInstance);

        // play panel close sound here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPanelOpenAndCloseSFX();
        }
    }

    public void CloseInstructionsPanel()
    {
        Destroy(instructionsPanelPrefabInstance);

        // play panel close sound here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPanelOpenAndCloseSFX();
        }
    }

    public void CloseSetupFailedNotification()
    {
        Destroy(setupFailedNotificationPrefabInstance);
    }

    public void CloseInputNotification()
    {
        Destroy(inputNotificationPrefabInstance);
    }
}
