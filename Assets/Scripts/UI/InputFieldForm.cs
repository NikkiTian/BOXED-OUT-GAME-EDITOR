using UnityEngine;
using UnityEngine.UI;

public class InputFieldForm : MonoBehaviour
{
    #region Modifiable Variables
    [Header("Function of the input field")]
    // this will determine what the input field will do
    public TypeOfInputField Type;
    #endregion

    #region Misc
    // returns if the input field actually triggers anything or not
    public bool IsDisabled { get { return isDisabled; } private set {; } }
    private bool isDisabled;

    private TMPro.TMP_InputField thisInputField;
    #endregion

    public enum TypeOfInputField
    {
        SerialPort,
        BaudRate,
        ReadSpeed
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        // using just type InputField yields null using GetComponent() :/
        thisInputField = GetComponent<TMPro.TMP_InputField>();
        if (thisInputField == null)
        {
            Debug.Log("Input field component cannot be found, cannot set onValueChanged listener event");
            return;
        }

        switch (Type)
        {
            case TypeOfInputField.SerialPort:
                thisInputField?.onValueChanged.AddListener(
                                                        (string portName) =>
                                                        {
                                                            UpdateSerialTestPortName(portName);
                                                            Debug.Log("Stored input for serial port name");
                                                        });
                break;
            case TypeOfInputField.BaudRate:
                thisInputField?.onValueChanged.AddListener(
                                                        (string baudRate) => 
                                                        {
                                                            short baudRateAsShort;
                                                            if (short.TryParse(baudRate, out baudRateAsShort))
                                                            {
                                                                UpdateSerialTestBaudRate(baudRateAsShort);
                                                                Debug.Log("Stored valid input for baudrate");
                                                            }
                                                            else
                                                            {
                                                                Debug.Log("Invalid input for baudrate. Was unable to store info");
                                                            }
                                                        });
                break;
            case TypeOfInputField.ReadSpeed:
                thisInputField?.onValueChanged.AddListener(
                                                       (string readSpeed) =>
                                                       {
                                                           short readSpeedAsShort;
                                                           if (short.TryParse(readSpeed, out readSpeedAsShort))
                                                           {
                                                               UpdateSerialTestReadSpeed(readSpeedAsShort);
                                                               Debug.Log("Stored valid input for read speed");
                                                           }
                                                           else
                                                           {
                                                               Debug.Log("Invalid input for read speed. Was unable to store info");
                                                           }
                                                       });
                break;
        } 
    }

    // just clear all listeners to avoid complications when disabled and or destroyed
    private void OnDestroy()
    {
        thisInputField?.onValueChanged.RemoveAllListeners();
    }

    private void OnDisable()
    {
        if (thisInputField != null)
        {
            thisInputField.onValueChanged.RemoveAllListeners();
            thisInputField.enabled = false;
        }

        isDisabled = true;
    }

    private void UpdateSerialTestPortName(string portName)
    {
        Debug.Log("Logging port name");

        if (SerialTest.Instance == null)
        {
            Debug.LogWarning("Serial test instance does not exist, cannot store input");
            return;
        }

        SerialTest.Instance.Port = portName;
    }

    private void UpdateSerialTestBaudRate(short baudRate)
    {
        Debug.Log("Logging baudrate");

        if (SerialTest.Instance == null)
        {
            Debug.LogWarning("Serial test instance does not exist, cannot store input");
            return;
        }

        SerialTest.Instance.Baudrate = baudRate;
    }

    private void UpdateSerialTestReadSpeed(short readSpeed)
    {
        Debug.Log("Logging read speed");

        if (SerialTest.Instance == null)
        {
            Debug.LogWarning("Serial test instance does not exist, cannot store input");
            return;
        }

        SerialTest.Instance.ReadSpeed = readSpeed;
    }


}
