using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    #region Instance Setup
    public static CameraController Instance { get; private set; }
    #endregion

    #region Modifiable Fields
    [Range(0f, 1f)]
    public float LerpSpeed;
    #endregion

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

    private void Update()
    {
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        if (PlayerController.Instance == null)
        {
            Debug.Log("Camera cannot find player...");
            return;
        }

        // we're only tracking the player's x and z position
        Vector3 targetPos = new Vector3(
                                        PlayerController.Instance.transform.position.x, 
                                        this.transform.position.y, 
                                        PlayerController.Instance.transform.position.z);    

        this.transform.position = Vector3.Lerp(this.transform.position, targetPos , LerpSpeed);   
    }


}
