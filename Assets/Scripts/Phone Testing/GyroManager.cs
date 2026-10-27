using System.Linq.Expressions;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using AttitudeSensor = UnityEngine.InputSystem.AttitudeSensor;

public class GyroManager : MonoBehaviour
{
    /// <summary>
    /// Before you get to actually doing anything this is the workflow to get your iphone gyroscope working on windows:
    /// 
    /// INSTRUCTIONS (The Setup)
    /// ----------------------------------------
    /// 1. Download iTunes, though since it is defunct. Download apple devices (especially for Windows 11 pcs). Both apps can be found in the microsoft store.
    /// 2. Be sure to set those up so that they rocognize your phone.
    /// 3. Also download the ios build for unity. In the unity editor, go to File --> Build Profiles --> iOS; if you don't have the ios package, you'll be prompted to install it.
    /// 4. Probably restart your computer afterwards so everything initializes (this is mainly for the apple devices app).
    /// 5. After that go to (in your Unity project): Project Settings -> Editor -> Devices (under Remote Play) and change the device to your ios device OR any ios device
    /// 6. To make sure it works. Make sure Apple Devices (or iTunes though I haven't tested iTunes) is turned on and detects your phone. If it still doesn't appear, unplug and replug.
    /// 7. Install Unity Remote 5 from the ios store.
    /// 8. So the tricky part is getting Remote 5 to execute. I would maybe suggest restarting your phone just in case.
    /// 9. The method of successfully executing the app is to open the app when you're loading the game after pressing play.
    /// 10. If your phone fell asleep, probably refresh the app before setting it up again.
    /// 11. If you did it successfully, the app on your phone should now be streaming your game after it successfully compiled to playmode.
    /// 
    /// 
    /// BENEFITS (The Benefits)
    /// ----------------------------------------
    ///     You can access the new UnityEngine.InputSystem module which allows you to access mobile features 
    ///     like accelerometers, touchscreen, attitude sensors (which are technically gyroscopes, but includes orientation), gyroscopes, etc.
    ///     
    ///     It seems to work over using the legacy Input module, which seems to be finnicky when interacting with features like Input.gyro (which is not null but doesn't return my phone
    ///     gyro when I connect it so idk why tf it's doing that)
    ///     
    /// ADDITIONAL NOTES (Miscellaneous Bugs and Crap)
    /// ----------------------------------------
    /// By default, all inputdevices are disabled by default so you have to enable them. HOWEVER, the devices take a small amount of time to be found so you have to check
    /// if the device you're looking for is not null. After enabling the device, the device disables shortly after for some reason, you can check this by trying to read the 
    /// value of a sensor. If it debugs to a default value, then it probably means the device is off again. ENABLE IT AGAIN in update or something.
    /// 
    /// </summary>

    #region Init The Script
    public static GyroManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject().AddComponent<GyroManager>();

            return instance;
        }
        set
        {
            ;
        }
    }
    private static GyroManager instance;
    #endregion Init The Script

    #region Other Misc Vars
    //for gyroscope
    public bool GyroEnabled { get { return gyroEnabled; } }
    private bool gyroEnabled;
    // for attitude sensor
    public bool AttitudeSensorEnabled { get { return attitudeSensorEnabled; } }
    private bool attitudeSensorEnabled;
    #endregion Other Misc Vars

    // activate gyroscope
    public async void EnableRotationalSensors()
    {
        // waiting for a viable sensor (if any)
        await AwaitSensorsDetection();

        // then enable our devices 
        InputSystem.EnableDevice(AttitudeSensor.current);
        InputSystem.EnableDevice(Gyroscope.current);

        attitudeSensorEnabled = true;
        gyroEnabled = true;
    }

    // first check if the gyroscope exists
    public async Task AwaitSensorsDetection()
    {
        Debug.Log("Awaiting attitude sensor connection...");
        Debug.Log("Awaiting gyroscope sensor connection...");

        while (AttitudeSensor.current == null || Gyroscope.current == null)
        {
            await Task.Yield();
        }

        Debug.Log("Attitude sensor detected!");
        Debug.Log("Gyroscope sensor detected!");
    }

    private void Update()
    {
        // this is extremely strange but it seems that after initially checking if the sensor is enabled and enabling it; it turns off again
        // though the device is not null, but the device is deactivated
        if (attitudeSensorEnabled && !AttitudeSensor.current.enabled)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }

        if (gyroEnabled && !Gyroscope.current.enabled)
        {
            InputSystem.EnableDevice(Gyroscope.current);
        }
    }

    public Quaternion GetOrientation()
    {
        if (attitudeSensorEnabled)
        {
            // Note attitude sensor is a quaternion version of the gyroscope and includes the orientation (compared to gyroscopes which returns only raw angular velocity)
            return AttitudeSensor.current.attitude.ReadValue();
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public Vector3 GetAngularVelocity()
    {
        if (gyroEnabled)
        {
            return Gyroscope.current.angularVelocity.ReadValue();
        }
        else
        {
            return Vector3.zero;
        }
    }
}

