using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform targetTransform; // The transform the camera will look at

    void Update()
    {
        // Set the camera's position to be the same as the target transform
        //transform.position = targetTransform.position;

        // Make the camera look at the target transform
        transform.LookAt(targetTransform);
    }
}