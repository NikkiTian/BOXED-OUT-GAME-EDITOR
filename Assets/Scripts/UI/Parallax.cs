using UnityEngine;

// Retained so existing scenes and prefabs keep valid script references.
// Mouse-following parallax has been retired; UI stays at its authored position.
public class Parallax : MonoBehaviour
{
    [HideInInspector] public float OffsetLimit;
    [HideInInspector] public float MoveSpeed;
}