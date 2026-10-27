using Unity.VisualScripting;
using UnityEngine;

public class DoNotDestroy : MonoBehaviour
{
    #region Instance Setup
    public DoNotDestroy Instance { get; private set; }
    #endregion

    private void Awake()
    {
        Init();
    }

    // this singleton behavior is going to be much like the one observed in the GameManager script
    // i.e. only the original scriptholder will exist, any new ones will be overridden versus the other way around
    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject); 
    }
}
