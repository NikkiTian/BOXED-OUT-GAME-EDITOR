using UnityEngine;

public class Panel : MonoBehaviour
{
    #region Modifiable Variables
    [Range(0f, 1f)]
    public float ScaleSpeed;
    #endregion

    #region Misc
    private RectTransform thisRectTransform;
    #endregion

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        thisRectTransform = GetComponent<RectTransform>();

        if (thisRectTransform != null)
        {
            thisRectTransform.localScale = Vector3.zero;
        }
        else
        {
            Debug.Log("Rect transform is not found in this obj");
            return;
        }
    }

    private void Update()
    {
        if (!IsFullSize())
            ScaleUp();
    }

    private void ScaleUp()
    {
        thisRectTransform.localScale = Vector3.Lerp(thisRectTransform.localScale, Vector3.one, ScaleSpeed);
    }

    private bool IsFullSize()
    {
        return thisRectTransform.localScale.magnitude >= Vector3.one.magnitude;
    }
}
