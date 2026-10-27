using UnityEngine;
using System.Collections.Generic;

public class Goal : MonoBehaviour
{
    #region Misc
    // this is a bit of misnomer but we're "disabling" and "activating" the goal by messing with its renderer (and not actually deactivating the game object)
    public bool IsDisabled
    {
        get
        {
            return spriteRend.enabled;
        }
    }
    // checks if a moveable is in the domain of the goal
    public bool MoveableDetected
    {
        get 
        { 
            return moveableDetected; 
        } 
        private set
        {
            ;
        }
    }
    private bool moveableDetected;
    // switch statement field to prevent excessively increasing the goals completed field of LevelManager
    private bool notifiedLevelManager;

    private SpriteRenderer spriteRend;
    #endregion

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        spriteRend = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        CheckForMoveable();
        UpdateVisuals();
        UpdateCommuncations();
    }

    private void UpdateVisuals()
    {
        if (moveableDetected)
        {
            spriteRend.enabled = false;
        }
        else
        {
            spriteRend.enabled = true;
        }
    }

    private void UpdateCommuncations()
    {
        if (LevelManager.Instance == null)
        {
            return;
        }

        // bad way to make a switch statement, but it works
        if (notifiedLevelManager == false && moveableDetected)
        {
            LevelManager.Instance.GoalsCompleted++;
            notifiedLevelManager = true;
        }
        else if (notifiedLevelManager == true && !moveableDetected)
        {
            LevelManager.Instance.GoalsCompleted--;
            notifiedLevelManager = false;
        }

    }

    public void CheckForMoveable()
    {
        if (LevelManager.Instance == null)
        {
            return;
        }

        IList<GridController.Tile3D> tile3Ds = GridController.GetAllTile3D(
                                                                        LevelManager.Instance.InteractablesTmap,
                                                                        GridController.WorldToGridPos(LevelManager.Instance.InteractablesTmap, this.transform.position));

        foreach(GridController.Tile3D tile3D in tile3Ds)
        {
            if (tile3D.RefersToSimilarGameObject(LevelManager.Instance.Moveable))
            {
                moveableDetected = true;
                return;
            }
        }

        moveableDetected = false;
    }
}
