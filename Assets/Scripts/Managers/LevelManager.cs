using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    #region Instance Setup 
    public static LevelManager Instance { get; private set; }
    #endregion

    #region Modifiable Variables
    [Header("Tilemap References")]
    public Tilemap InteractablesTmap;

    [Header("Tile3D References")]
    public GameObject Moveable;
    public GameObject Wall;
    [Tooltip("This one is not a prefab, it refers to specific Tile3Ds with the goal script (in scene) that represents the goal")]
    // weird thing is, when you deal with any prefab objects, you can't actually refer to it by the script its holding (if it is); that's why Goals is a gameobject array
    public GameObject[] Goals;
    #endregion

    #region Misc
    public int GoalsCompleted { get; set; }
    public bool LevelCompleted { get; private set; }
    // stores all the information about player's movements so it can be undone
    public List<(Vector3, int)> MoveVectorAndCounts { get; set; } = new List<(Vector3, int)> ();
    #endregion

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        CheckForLevelCompletion();
    }

    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        
        Instance = this;
    }

    private void CheckForLevelCompletion()
    {
        if (Goals.Length == 0 || Goals[0] == null)
        {
            Debug.Log("There are no goals in this level. Level cannot be completed.");
            return;
        }

        if (GoalsCompleted != Goals.Length)
        {
            return;
        }

        LevelCompleted = true;
        TriggerWinSequence();
    }

    private void TriggerWinSequence()
    {
        // maybe do some animation or audio
        Debug.Log("You won this level!");

        if (LevelTitleCard.Instance == null || GameManager.Instance == null)
        {
            Debug.Log("Can't wrap up the level, either the title card or the game manager doesn't exist.");
            return;
        }

        if (LevelTitleCard.Instance.DoneClosing)
        {
            // move onto the next level
            GameManager.Instance.UpdateScene();
        }
    }

    private readonly Dictionary<int, (GameObject box, Vector3 position, Quaternion rotation)> pulledBoxes
        = new Dictionary<int, (GameObject, Vector3, Quaternion)>();

    public void LogPullMovement(Vector3 step, GameObject box)
    {
        int index = MoveVectorAndCounts.Count;
        LogMovementVectorAndCount((step, 1));
        pulledBoxes[index] = (box, box.transform.position, box.transform.rotation);
    }

    public void LogMovementVectorAndCount((Vector3, int) movementVectorAndCount)
    {
        pulledBoxes.Remove(MoveVectorAndCounts.Count);
        MoveVectorAndCounts.Add(movementVectorAndCount);
    }

    public void RevertToPreviousState()
    {
        if (MoveVectorAndCounts.Count == 0)
            return;

        (Vector3 moveVector, int count) moveVectorAndCount = MoveVectorAndCounts[MoveVectorAndCounts.Count - 1];

        int historyIndex = MoveVectorAndCounts.Count - 1;
        if (pulledBoxes.TryGetValue(historyIndex, out var pulled))
        {
            if (pulled.box != null)
            {
                pulled.box.transform.position = pulled.position;
                pulled.box.transform.rotation = pulled.rotation;
                SoundManager.Instance?.PlayBoxRustleSFX();
            }
            PlayerController.Instance.transform.position -= moveVectorAndCount.moveVector;
            pulledBoxes.Remove(historyIndex);
            MoveVectorAndCounts.RemoveAt(historyIndex);
            SoundManager.Instance?.PlayPlayerMoveSFX();
            return;
        }

        // move any moveable blocks back if the player has moved them
        for (int i = 1; i < moveVectorAndCount.count; i++)
        {
            Vector3 pos = PlayerController.Instance.transform.position + moveVectorAndCount.moveVector * i;

            IList<GridController.Tile3D> tile3Ds = GridController.GetAllTile3D(
                                                                            InteractablesTmap,
                                                                            GridController.WorldToGridPos(InteractablesTmap, pos));

            foreach (GridController.Tile3D tile3D in tile3Ds)
            {
                if (tile3D.RefersToSimilarGameObject(Moveable))
                {
                    tile3D.Move(GridController.WorldToGridPos(InteractablesTmap, pos - moveVectorAndCount.moveVector));

                    // also rotate our moveable back
                    if (moveVectorAndCount.moveVector.normalized == Vector3.forward)
                    {
                        tile3D.Rotate(new Vector3(90f, 0f, 0f));
                    }
                    else if (moveVectorAndCount.moveVector.normalized == Vector3.back)
                    {
                        tile3D.Rotate(new Vector3(-90f, 0f, 0f));
                    }
                    else if (moveVectorAndCount.moveVector.normalized == Vector3.right)
                    {
                        tile3D.Rotate(new Vector3(0f, 0f, 90f));
                    }
                    else if (moveVectorAndCount.moveVector.normalized == Vector3.left)
                    {
                        tile3D.Rotate(new Vector3(0f, 0f, -90f));
                    }

                    // add the box rustle sfx here
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayBoxRustleSFX();
                    }
                }
            }
        }

        // then move the player back as well
        PlayerController.Instance.transform.position -= moveVectorAndCount.moveVector;
        // and remove the tuple because we've successfully reverted back our position
        MoveVectorAndCounts.RemoveAt(MoveVectorAndCounts.Count - 1);

        // add the player move sfx here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerMoveSFX();
        }
    }
}
