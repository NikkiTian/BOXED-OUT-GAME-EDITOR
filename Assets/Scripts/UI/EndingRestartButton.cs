using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class EndingRestartButton : MonoBehaviour
{
    private Button button;

    private void Awake() { button = GetComponent<Button>(); }
    private void OnEnable() { button.onClick.AddListener(ReturnToMenu); }
    private void OnDisable() { button.onClick.RemoveListener(ReturnToMenu); }

    private void ReturnToMenu()
    {
        SoundManager.Instance?.PlayButtonClickSFX();
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
        else
            SceneManager.LoadScene("Menu");
    }
}
