using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Gyro : MonoBehaviour
{
    #region Modifiable Fields
    [Range(0f, 10f)]
    public float MoveSpeed;
    [Tooltip("Determines how much the phone must rotate before being accepted as adequate input.")]
    [Range(0, 90f)]
    public float AngularThreshold;
    [Tooltip("Determines how far the phone can be orientated from its default position (phone facing viewer).")]
    [Range(0f, 1f)]
    public float OrientationThreshold;
    [Tooltip("How much time after a succsseful movement opportunity is done before we should check for phone orientation and then a subsequent movement opportunity.")]
    [Range(0f, 5f)]
    public float CheckForOrientationWaitTime;
    #endregion

    private bool moved;

    private void Start()
    {
        // create an instance
        GyroManager.Instance.EnableRotationalSensors();
    }

    // Update is called once per frame
    private void Update()
    {
        // console logging purposes

        //this.transform.rotation = Quaternion.Euler(GyroManager.Instance.GetAngularVelocity());

        //if (GyroManager.Instance.GyroEnabled)
        //    Debug.Log(GyroManager.Instance.GetAngularVelocity());

        //if (GyroManager.Instance.AttitudeSensorEnabled)
        //    Debug.Log(GyroManager.Instance.GetOrientation());

        // actual methods
        UpdateObject();
    }

    private void UpdateObject()
    {
        if (!moved && GyroIsBeingRotated())
        {
            Move(GetDirectionToMove() * MoveSpeed);
            StartCoroutine(ResetMovementOpportunity());

            Debug.Log("Moved!");
        }
    }

    // happens after the object is moved, momentarily pauses the ability to move and requires the user to go back to the fixed position
    private IEnumerator ResetMovementOpportunity()
    {
        moved = true;

        // run this initial wait time
        yield return new WaitForSeconds(CheckForOrientationWaitTime);

        // wait until we're actually back in position
        while (!IsInNeutralOrientation())
            yield return null;

        moved = false;

        Debug.Log("About neutral position... Can move again");
    }

    // determines if the phone is in its standard neutral position
    private bool IsInNeutralOrientation()
    {
        // attitude sensor at neutral position should yield an approxiamte quaternion of (1/sqrt(2), 0, 0, 1/sqrt(2))
        // ************************************************
        // Important notes:
        // Phone face up flat should yield a 0 on the x component
        // Phone facing user on its side to the left should yield a 0 on the z component
        

        // Formula for quaternions (RotationAngle is in radians):
        //x = RotationAxis.x * sin(RotationAngle / 2);
        //y = RotationAxis.y * sin(RotationAngle / 2);
        //z = RotationAxis.z * sin(RotationAngle / 2);
        //w = cos(RotationAngle / 2);

        // think of axis as a vector with which rotate around, since our default position only has the phone rotated around the x axis, we have a vector3 point towards the x axis
        // for more complicated rotations like (90 deg rot on x and 90 deg on y, the math is a lot more heavy and it'd prob take a bit to work out the kinks)
        Vector3 rotAxis = new Vector3(1f, 0f, 0f); 
        float rotAngle = Mathf.PI / 2f;
        float x = rotAxis.x * Mathf.Sin(rotAngle / 2);
        float y = rotAxis.y * Mathf.Sin(rotAngle / 2);
        float z = rotAxis.z * Mathf.Sin(rotAngle / 2);
        float w = Mathf.Cos(rotAngle / 2);

        return Quaternion.Dot(GyroManager.Instance.GetOrientation(), new Quaternion(x, y, z, w)) >= 1 - OrientationThreshold;
    }

    // bool to determine if the gyroscope is being actually moved or it's just noise
    private bool GyroIsBeingRotated()
    {
        return GyroManager.Instance.GetAngularVelocity().magnitude > AngularThreshold;
    }

    // the method that actually moves our character
    private void Move(Vector3 direction)
    {
        this.transform.position += direction;
    }

    // determine which direction the character should move
    private Vector3 GetDirectionToMove()
    {
        Vector3 angularVel = GyroManager.Instance.GetAngularVelocity();

        // how to find the max out of 3 numbers efficiently?? btw this is by sheer magnitude
        if (Mathf.Abs(angularVel.x) - Mathf.Abs(angularVel.y) > 0f && Mathf.Abs(angularVel.x) - Mathf.Abs(angularVel.z) > 0f)
        {
            // x is the biggest by magnitude
            // btw it's reverse, flipping the phone screen up yields a negative x vector
            return new Vector3(0f, 0f, -angularVel.x).normalized;
        }
        else if (Mathf.Abs(angularVel.y) - Mathf.Abs(angularVel.x) > 0f && Mathf.Abs(angularVel.y) - Mathf.Abs(angularVel.z) > 0f)
        {
            // y is the biggest by magnitude
            // do nothing rn, we don't need this direction
        }
        else if (Mathf.Abs(angularVel.z) - Mathf.Abs(angularVel.x) > 0f && Mathf.Abs(angularVel.z) - Mathf.Abs(angularVel.y) > 0f)
        {
            // z is the biggest by magnitude
            // the same logic is here with x, the positioning is reversed
            return new Vector3(-angularVel.z, 0f, 0f).normalized;
        }

        return Vector3.zero;
    }


}

