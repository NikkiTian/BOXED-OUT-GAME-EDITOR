using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine.Rendering;


public class SerialRead
{
    #region Accessibles
    // other scripts can access these variables, though they shouldn't/can't be modified
    public SerialPort SerialPort { get; private set; }
    public string Data { get { return data ; } private set {; } }
    private string data;

    public bool PortIsActive
    {
        get
        {
            if (SerialPort != null)
                return SerialPort.IsOpen;
            else
                 return false;
        }
    }
    #endregion

    #region Read Thread Logistics
    // other variabeles concerned with setting up a read thread to transcribe the arduino code
    private CancellationTokenSource cts;
    private Thread readThread;
    #endregion

    #region Port Logistics
    // all of these variables are used to access the serialPort
    private string port;
    private short baudrate;
    private Parity parity;
    private byte dataBits;
    private StopBits stopBits;
    #endregion 

    public SerialRead (string port, short baudrate = 9600, Parity parity = Parity.None, byte dataBits = 8, StopBits stopBits = StopBits.One)
    {
        this.port = port;
        this.baudrate = baudrate;
        this.dataBits = dataBits;
        this.parity = parity;
        this.stopBits = stopBits;
    }

    // try to access our serial port
    public void Open()
    {
        try
        {
            // Debug log is not a functionality of a monobehaviour but of the unity software itself
            // as long as we're using a unity project, we can use debug.log
            Debug.Log("Trying to open...");

            // enable our serial port
            SerialPort = new SerialPort(port, baudrate, parity, dataBits, stopBits);
            // according to several sources, it's based to enable these because readLine might not work without them
            SerialPort.DtrEnable = true;
            SerialPort.RtsEnable = true;
            // also enable our cancelliation token source
            cts = new CancellationTokenSource();

            // Data received doesn't work because Unity's mono version of the serial port doesn't work
            // (prob unless we manage to find a way to import System.Io.Ports from nuGet somehow)
            //SerialPort.DataReceived += SerialPortDataReceived;
            SerialPort.Open();

            Debug.Log("Success!");
        }
        catch (Exception err)
        {
            Debug.Log($"Failed. Serial port '{port}' cannot be initialized: {err.Message}");
        }
    }
    
    // start checking for data
    public void StartMonitoring(int readSpeed)
    {
        Debug.Log("Now attempting to monitor data...");

        if (PortIsActive)
        {
            ReadData readDataHandler = ReadDataMainMethod;
            readDataHandler?.Invoke(readSpeed);
        }
        else
        {
            Debug.Log("No valid serial port to read data");
        }
    }

    // NOTE NOTE NOTE, the following comments on this commented method are super mega fking importante

    // read our data printed to the serial port
    //private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    //{
    //    string data = SerialPort.ReadLine();
    //    // SerialPort.ReadExisitng() or SerialPort.Read() also works too but it is buggy and prone to not working 

    //    Debug.Log(data);
    //}

    public delegate void ReadData(int readSpeed);
    public void ReadDataMainMethod(int readSpeed)
    {
        // start a new, separate thread to read our data (reading it via update causes bugs)
        readThread = new Thread((object obj) =>
        {
            // a psuedo update method that runs when the application is playing
            while (true)
            {
                CancellationToken token = (CancellationToken)obj;
                if (token.IsCancellationRequested)
                {
                    Debug.Log("Ending Thread");
                    break;
                }


                // if data is being sent at all
                if (SerialPort.BytesToRead != 0)
                {
                    // see if we can read the data being sent
                    try
                    {
                        data = SerialPort.ReadLine();
                        //Debug.Log($"Data: {data}");
                    }
                    // if not, SerialPort.ReadLine() will timeout, we'll just break out of the thread atp
                    catch (Exception timeOutException)
                    {
                        Debug.Log($"Error: {timeOutException}");
                        break;
                    }
                }

                // do note the readSpeed isn't exact because SerialPort.ReadLine() is a blocking method
                // thereby, the data read speed is actually about the delay time in arduino + the thread.sleep time
                Thread.Sleep(readSpeed);
            }
        });

        readThread.Start(cts.Token);
    }

    // close our serial port
    public void Close()
    {
        if (PortIsActive)
        {
            Debug.Log("Closing port...");

            cts.Cancel();
            cts.Dispose();

            SerialPort.Close();
            SerialPort.Dispose();
        }
        else
        {
            Debug.Log("No valid port has been identified. Cannot close null");
        }
    }
}
