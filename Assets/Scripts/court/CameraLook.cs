using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public float sensitivity = 2f;

    public float maxHorizontalAngle = 30f;
    public float maxVerticalAngle = 30f;

    private float currentX;
    private float currentY;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        currentY += Input.GetAxis("Mouse X") * sensitivity;
        currentX -= Input.GetAxis("Mouse Y") * sensitivity;

        currentX = Mathf.Clamp(currentX, -maxVerticalAngle, maxVerticalAngle);
        currentY = Mathf.Clamp(currentY, -maxHorizontalAngle, maxHorizontalAngle);

        transform.localRotation =
            startRotation *
            Quaternion.Euler(currentX, currentY, 0f);
    }
}