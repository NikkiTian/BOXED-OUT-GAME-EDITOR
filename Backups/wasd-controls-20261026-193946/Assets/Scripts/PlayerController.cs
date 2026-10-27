using System.Runtime.CompilerServices;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlayerController : MonoBehaviour
{
    #region Instance Setup
    public static PlayerController Instance { get; private set; }
    #endregion

    #region Modifiable Variables
    [Header("Player Move Distance")]
    public float MoveDistance = 1.0f;

    [Header("Gyroscope-based Movement Variables")]
    [Range(0f, 90f)]
    public float AngularThreshold;
    #endregion

    #region Misc 
    // the gyroscope (attitude sensor most likely in our case) can rotate on its axis thereby the axis it rotates on changes
    // e.g. x, z (rot on x axis) --> x, y | x, z (rot on z axis) --> z, y  
    public GyroscopeAxes CurrentGyroAxes { get { return currentGyroAxes; } private set {; } }
    private GyroscopeAxes currentGyroAxes = GyroscopeAxes.XZ;

    // following field is to aid in tile-based movment (prevents consistent movement)
    private bool hasMoved;

    // used for gyroscopic purposes
    private Vector3 previousRot;
    private Vector3 originalGyroRotation = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    // player input 
    private PlayerInputActionsBase inputSystemControls;
    private InputAction move;
    #endregion

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        Setup();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.PanelOrNotificationOpen)
        {
            Debug.Log("Game Manager script does not exist or pause panel is up, cannot move");
            return;
        }

        ToggleKeyBasedMovement();

        if (move.enabled) 
            UpdatePlayerKeyBased();
        else
        {
            UpdatePlayerGyroBased();
        }
    }

    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    // disable this to ignore compile errors
    private void OnDisable()
    {
        // seems like this method is called AFTER the scene changes, so we just put the null conditional here
        // to avoid this issue
        move?.Disable();
    }

    private void Setup()
    {
        inputSystemControls = new PlayerInputActionsBase();
        move = inputSystemControls.Player.Move;

        // store the original orientation of the gyroscope though I'm not sure if this is of any use
        new Task(() =>
                                {
                                    int waitTimeMillis = 100;

                                    while (true)
                                    {
                                        if
                                        (
                                        SerialTest.Instance != null &&
                                        SerialTest.Instance.GetParsedSerialData() != Vector3.zero
                                        )
                                        {
                                            originalGyroRotation = SerialTest.Instance.GetParsedSerialData();
                                            Debug.Log(originalGyroRotation);
                                            break;
                                        }

                                        Task.Delay(waitTimeMillis);
                                    }
                                }).Start();

    }

    private void UpdatePlayerKeyBased()
    {
        Vector3 rawInput = GetKeyBasedInput();

        // logic for creating grid-based movement so it functions similar to normalizedInput.GetKeyDown(Button) 
        if (rawInput.magnitude == 0)
        {
            hasMoved = false;
            return;
        }
        if (hasMoved)
        {
            return;
        }

        Vector3 normalizedInput  = NormalizeKeyBasedInput(rawInput);
        UpdatePosition(normalizedInput);
        hasMoved = true;
    }

    private void UpdatePlayerGyroBased()
    {
        // this whole thing just doesn't work unfortunately, the drift is too great and I'm not sure how much of it occurs
        // the only possible way of dealing with this is just to get the angular acceleration from the gyroscope rather than including the 
        // attitude sensor data
        Vector3 rawInput = GetGyroscopeBasedInput();
        float tolerance = 0.05f;

        Vector3 gyroscopeDrift = new Vector3(0.7f, 0f, 0.7f) * Time.deltaTime;
        originalGyroRotation += gyroscopeDrift;

        if (!GyroIsBeingRotated(rawInput) && !GyroIsInOriginalPosition(tolerance))
        {
            return;
        }
        else
        {
            previousRot = Vector3.zero;
        }


        //Vector3 normalizedInput = NormalizeRelativeGyroBasedInput(rawInput); // for box
        Vector3 normalizedInput = NormalizeGyroBasedInput(rawInput);
        Debug.Log(normalizedInput);
        UpdatePosition(normalizedInput);
        //CalibrateNewOrientation(normalizedInput); // for box

        previousRot = rawInput;
    }

    // Universal/Multifunctional Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


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

    // Gyroscope-Based Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Vector3 GetGyroscopeBasedInput()
    {
        // check if the instance doesn't exist or the port is not open
        if (SerialTest.Instance == null || !SerialTest.Instance.Sr.PortIsActive) 
            return Vector2.zero;

        return SerialTest.Instance.GetParsedSerialData();
    }

    private Vector3 NormalizeGyroBasedInput(Vector3 rawInput)
    {
        Vector3 diffRot = rawInput - previousRot;

        if (Mathf.Abs(diffRot.x) > Mathf.Abs(diffRot.z))
            return Vector3.forward * diffRot.x / Mathf.Abs(diffRot.x);
        else
            return Vector3.right * diffRot.z / Mathf.Abs(diffRot.z);
    }

    // the gyroscope is not absolute, that means the axes chage relative to the orientation of the box so we have to take that account when recording our inputs
    private Vector3 NormalizeRelativeGyroBasedInput(Vector3 rawInput)
    {
        Vector3 diffRot = rawInput - previousRot;

        switch (currentGyroAxes)
        {
            case GyroscopeAxes.XZ:
                // default, technically
                if (Mathf.Abs(diffRot.x) > Mathf.Abs(diffRot.z))
                    return Vector3.forward * diffRot.x / Mathf.Abs(diffRot.x);
                else
                    return Vector3.right * diffRot.z / Mathf.Abs(diffRot.z);

            case GyroscopeAxes.YZ:

                if (Mathf.Abs(diffRot.y) > Mathf.Abs(diffRot.z))
                    return Vector3.forward * diffRot.y / Mathf.Abs(diffRot.y);
                else
                    return Vector3.right * diffRot.z / Mathf.Abs(diffRot.z);

            case GyroscopeAxes.XY:

                if (Mathf.Abs(diffRot.x) > Mathf.Abs(diffRot.y))
                    return Vector3.forward * diffRot.x / Mathf.Abs(diffRot.x);
                else
                    return Vector3.right * diffRot.y / Mathf.Abs(diffRot.y);

            case GyroscopeAxes.ZY:

                if (Mathf.Abs(diffRot.z) > Mathf.Abs(diffRot.y))
                    return Vector3.forward * diffRot.z / Mathf.Abs(diffRot.z);
                else
                    return Vector3.right * diffRot.y / Mathf.Abs(diffRot.y);

            case GyroscopeAxes.YX:

                if (Mathf.Abs(diffRot.y) > Mathf.Abs(diffRot.x))
                    return Vector3.forward * diffRot.y / Mathf.Abs(diffRot.y);
                else
                    return Vector3.right * diffRot.x / Mathf.Abs(diffRot.x);

            case GyroscopeAxes.ZX:

                if (Mathf.Abs(diffRot.z) > Mathf.Abs(diffRot.x))
                    return Vector3.forward * diffRot.z / Mathf.Abs(diffRot.z);
                else
                    return Vector3.right * diffRot.x / Mathf.Abs(diffRot.x);
        }

        return Vector3.zero;
    }

    private void CalibrateNewOrientation(Vector3 normalizedInput)
    {
        // we shouldn't calibrate orientation if there is no input
        if (normalizedInput  == Vector3.zero) 
            return;

        switch (currentGyroAxes)
        {
            case GyroscopeAxes.XZ:

                // if the input is a forward/backward movement
                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.XY;
                // else if the input is a left/right movement
                else
                    currentGyroAxes = GyroscopeAxes.YZ;

                return;

            case GyroscopeAxes.YZ:

                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.YX;
                else
                    currentGyroAxes = GyroscopeAxes.XZ;

                return;

            case GyroscopeAxes.XY:

                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.XZ;
                else
                    currentGyroAxes = GyroscopeAxes.ZY;

                return;

            case GyroscopeAxes.ZY:

                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.ZX;
                else
                    currentGyroAxes = GyroscopeAxes.XY;

                return;

            case GyroscopeAxes.YX:

                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.YZ;
                else
                    currentGyroAxes = GyroscopeAxes.ZX;

                return;

            case GyroscopeAxes.ZX:

                if (Mathf.Abs(normalizedInput.z) > 0)
                    currentGyroAxes = GyroscopeAxes.ZY;
                else
                    currentGyroAxes = GyroscopeAxes.YX;

                return;
        }
    }

    private bool GyroIsBeingRotated(Vector3 inputRot)
    {
        Vector3 rotDiff = inputRot - previousRot;

        return rotDiff.magnitude > AngularThreshold;
    }

    private bool GyroIsInOriginalPosition(float tolerance)
    {
        if (SerialTest.Instance != null)
        {
            return Quaternion.Dot(Quaternion.Euler(originalGyroRotation), Quaternion.Euler(SerialTest.Instance.GetParsedSerialData())) >= 1 - tolerance;
        }

        return false;
    }

    // Key-Based Methods /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Vector2 GetKeyBasedInput()
    {
        return move.ReadValue<Vector2>();
    }

    private Vector3 NormalizeKeyBasedInput(Vector2 rawInput)
    {
        if (rawInput.x != 0)
            return Vector3.right * rawInput.x;
        else
            return Vector3.forward * rawInput.y;
    }


    // allows me to use standard wasd or arrow keys to move
    private void ToggleKeyBasedMovement()
    {
        if (!Input.GetKeyDown(KeyCode.Q))
            return;

        if (!move.enabled)
        {
            move.Enable();
            Debug.Log("Key based movement toggled on");
        }
        else
        {
            move.Disable();
            Debug.Log("Key based movement toggled off");
        }
    }
}

// Axes for relative rotations of the gyroscope
public enum GyroscopeAxes
{
    // NOTE: the rightmost letter in each pair is the right axes

    XZ,
    YZ,
    XY,
    ZY,
    YX,
    ZX
}
