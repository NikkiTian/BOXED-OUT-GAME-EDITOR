using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class SerialTest : MonoBehaviour
{
    #region Instance Setup
    public static SerialTest Instance { get; private set; }
    // this is the actual class object that enables our serial port and sllows us to read incoming data printed there
    public SerialRead Sr { get { return sr; } }
    private SerialRead sr;
    #endregion

    #region Serial Port Setup
    [Tooltip("The port to check")]
    public string Port;
    [Tooltip("The associated baudrate with the port")]
    public short Baudrate;
    [Tooltip("The speed at which data sent to the serial port (if any) should be read")]
    public short ReadSpeed;
    #endregion

    private void Awake()
    {
        Init();
    }
    private void Init()
    {
        DontDestroyOnLoad(this.gameObject);

        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    public void SetupSerialRead()
    {
        // serial read only exists when the application is playing, otherwise it quits automatically
        sr = new SerialRead(Port, Baudrate);
        sr.Open();
        sr.StartMonitoring(ReadSpeed);

        // ends the external thread when we're not in play mode
        Application.quitting += (() =>
        {
            EndSerialRead();
        });
    }

    public void EndSerialRead()
    {
        sr.Close();
    }


    public Vector3 GetParsedSerialData()
    {   
        if (!Sr.PortIsActive || Sr.Data == null)
            return Vector3.zero;

        // to include escape characters, use @ at thr front of the string or \\ for each slash
        string dataWhiteSpaceRemoved = Regex.Replace(Sr.Data, @"\s+", "");

        // item 1 should be roll, item 2 pitch, item 3 yaw
        string[] rotationalAxesData = Regex.Split(dataWhiteSpaceRemoved, @"\|+");

        // right now, not entirely sure which axis is which: 
        // in models, pitch is the x-axis; roll is the z-axis; and yaw is the y-axis rotation
        float roll = 0f, pitch = 0f, yaw = 0f;

        for (int i = 0; i <  rotationalAxesData.Length; i++) 
        {   
            switch (i)
            {
                case 0:
                    float.TryParse(Regex.Match(rotationalAxesData[i], "(?<=:).*").Value, out roll);
                    break;
                case 1:
                    float.TryParse(Regex.Match(rotationalAxesData[i], "(?<=:).*").Value, out pitch);
                    break;
                case 2:
                    float.TryParse(Regex.Match(rotationalAxesData[i], "(?<=:).*").Value, out yaw);
                    break;
            }
        }

        return new Vector3(pitch, yaw, roll);   
    }
}
