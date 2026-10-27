using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Player Move Distance")]
    public float MoveDistance = 1.0f;

    private PlayerInputActionsBase inputSystemControls;
    private InputAction move;
    private bool hasMoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);
        Instance = this;
        inputSystemControls = new PlayerInputActionsBase();
        move = inputSystemControls.Player.Move;
    }

    private void OnEnable()
    {
        hasMoved = false;
        move?.Enable();
    }

    private void OnDisable()
    {
        move?.Disable();
        hasMoved = false;
    }

    private void OnDestroy()
    {
        inputSystemControls?.Dispose();
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Vector2 rawInput = move.ReadValue<Vector2>();
        // Release all movement keys before the next grid step. Holding a key
        // never repeats; diagonal input still moves exactly one cardinal tile.
        if (rawInput == Vector2.zero)
        {
            hasMoved = false;
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.PanelOrNotificationOpen)
        {
            hasMoved = true;
            return;
        }
        if (hasMoved)
            return;

        Vector3 direction = rawInput.x != 0
            ? Vector3.right * Mathf.Sign(rawInput.x)
            : Vector3.forward * Mathf.Sign(rawInput.y);
        UpdatePosition(direction);
        hasMoved = true;
    }

    private void UpdatePosition(Vector3 normalizedInput)
    {
        if (LevelManager.Instance == null)
        {
            Debug.Log("There is no level manager found, player cannot move");
            return;
        }

        // logging of the total amount of movements (including moveables), used to revert the player state later on
        int movementCount = 0;

        // get our relative positions so we can do the movement logic
        Vector3 movementIncrement = normalizedInput * MoveDistance;
        Vector3 targetPos = this.transform.position + movementIncrement;

        // check if there are any tile3Ds in the tile the player is going to
        IList<GridController.Tile3D> tile3Ds = GridController.GetAllTile3D(
                                                                    LevelManager.Instance.InteractablesTmap, 
                                                                    GridController.WorldToGridPos(LevelManager.Instance.InteractablesTmap, targetPos));

        foreach(GridController.Tile3D tile3D in tile3Ds)
        {
            // if a wall is identified
            if (tile3D.RefersToSimilarGameObject(LevelManager.Instance.Wall))
            {
                // it is confirmed you cannot move
                return;
            }
            // else if a moveable is identified
            else if (tile3D.RefersToSimilarGameObject(LevelManager.Instance.Moveable))
            {
                // check all the tile3Ds in the tile one step away
                IList<GridController.Tile3D> furtherTile3Ds = GridController.GetAllTile3D(
                                                                                    LevelManager.Instance.InteractablesTmap, 
                                                                                    GridController.WorldToGridPos(LevelManager.Instance.InteractablesTmap, targetPos + movementIncrement));
                // check if a wall or moeveable is contained in this tile
                foreach(GridController.Tile3D furtherTile3D in furtherTile3Ds)
                {
                    if (furtherTile3D.RefersToSimilarGameObject(LevelManager.Instance.Wall) || furtherTile3D.RefersToSimilarGameObject(LevelManager.Instance.Moveable))
                    {
                        return;
                    }
                }

                // if there isn't a wall or moveable, we're free to push the moveable block and move as well; I know this method is multifunctional, but it's simple enough
                tile3D.Move(GridController.WorldToGridPos(LevelManager.Instance.InteractablesTmap, targetPos + movementIncrement));
                // cool thing to note, Vector3.forward/back/left/etc are static variables, not constant so we have to use an if state branch to rotate our moveable box
                if (normalizedInput == Vector3.forward)
                {
                    tile3D.Rotate(new Vector3(90f, 0f, 0f));
                }
                else if (normalizedInput == Vector3.back)
                {
                    tile3D.Rotate(new Vector3(-90f, 0f, 0f));
                }
                else if (normalizedInput == Vector3.right)
                {
                    tile3D.Rotate(new Vector3(0f, 0f, 90f));
                }
                else if (normalizedInput == Vector3.left)
                {
                    tile3D.Rotate(new Vector3(0f, 0f, -90f));
                }

                // add box rustle audio here
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBoxRustleSFX();
                }

                movementCount++;
            }
        }

        movementCount++;
        LevelManager.Instance.LogMovementVectorAndCount((movementIncrement, movementCount));

        // now actually move the player
        Move(targetPos);
    }

    private void Move(Vector3 targetPos)
    {
        this.transform.position = targetPos;

        // add footstep audio here
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerMoveSFX();
        }
    }

}
