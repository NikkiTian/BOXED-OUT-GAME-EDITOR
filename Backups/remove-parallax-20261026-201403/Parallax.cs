using UnityEngine;
using UnityEngine.InputSystem;

public class Parallax : MonoBehaviour
{
    #region Modifiable Variables
    [Tooltip("How far can the object be offset before being clamped")]
    public float OffsetLimit;
    [Tooltip("How fast should the parallax effect be")]
    [Range(0f, 1f)]
    public float MoveSpeed;
    #endregion

    #region Misc
    // store the original transform position for other purposes
    private Vector3 originalTransformPosition;
    private RectTransform thisRectTransform;
    #endregion

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        thisRectTransform = GetComponent<RectTransform>();

        if (thisRectTransform == null)
        {
            Debug.Log("Cannot find rectTransform, please add it");
            return;
        }

        originalTransformPosition = thisRectTransform.position;
    }

    private void Update()
    {
        MoveTowardsMouse();

        if (!WithinOffsetLimit())
            ClampPosition();
    }

    private void MoveTowardsMouse()
    {
        thisRectTransform.position = Vector3.Lerp(thisRectTransform.position, Input.mousePosition, MoveSpeed);
    }

    private void ClampPosition()
    {
        Vector3 dir = (Input.mousePosition - originalTransformPosition).normalized;
        thisRectTransform.position = originalTransformPosition + dir * OffsetLimit;
    }

    private bool WithinOffsetLimit()
    {
        float toleranceMargin = 0.1f;
        return (thisRectTransform.position - originalTransformPosition).magnitude <= OffsetLimit + toleranceMargin;
    }

}
