using TMPro;
using UnityEngine;

public class LevelTitleCard : MonoBehaviour
{
    #region Instance Setup
    public static LevelTitleCard Instance { get; private set; }
    #endregion

    #region Modifiable Variables
    [Range(0f, 3f)]
    public float AnimationSpeed;
    // made this public so we can use other parts of the transition as well 
    public TitleCardStage Tcs;
    #endregion

    #region Misc
    // marks all animations are done (including the closing one)
    public bool DoneClosing { get; private set; }
    private TMP_Text text;
    private CanvasGroup cg;
    #endregion

    public enum TitleCardStage
    {
        Reveal,
        Hiding,
        Idle,
        Closing
    }


    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        cg = GetComponent<CanvasGroup>();
        text = GetComponentInChildren<TMP_Text>();

        if (cg == null || text == null)
        {
            Debug.Log("Cannot find canvas group, text component, or both, please check and add them");
            return;
        }

        // make sure the text is clear on start
        text.color = Color.clear;
    }

    private void Update()
    {
        switch(Tcs)
        {
            case TitleCardStage.Reveal:

                UpdateRevealStage();
                break;
            case TitleCardStage.Hiding:

                UpdateHidingStage();    
                break;
            case TitleCardStage.Idle:

                UpdateIdleStage();
                break;
            case TitleCardStage.Closing:

                UpdateClosingStage();
                break;
        }
    }

    private void UpdateRevealStage()
    {
        if (text.color.a < 1)
            RevealText();
        else
            Tcs = TitleCardStage.Hiding;
    }
    private void UpdateHidingStage()
    {
        if (cg.alpha > 0)
            HideSelf();
        else
            Tcs = TitleCardStage.Idle;
    }
    private void UpdateIdleStage()
    {
        if (LevelManager.Instance == null)
            return;

        if (LevelManager.Instance.LevelCompleted)
            Tcs = TitleCardStage.Closing;
    }
    private void UpdateClosingStage()
    {
        if (text.color.a > 0)
            text.color = Color.clear;

        if (cg.alpha < 1)
            DarkenScreen();
        else
            DoneClosing = true;

        // play audio here
        if (SoundManager.Instance != null && !SoundManager.Instance.WhooshPlayed)
        {
            SoundManager.Instance.PlayWhooshSFX();
        }
    }


    private void RevealText()
    {
        text.color = new Color(1, 1, 1, text.color.a + AnimationSpeed * Time.deltaTime);
    }

    private void HideSelf()
    {
        cg.alpha -= AnimationSpeed * Time.deltaTime;
    }

    private void DarkenScreen()
    {
        cg.alpha += AnimationSpeed * Time.deltaTime;
    }
}

