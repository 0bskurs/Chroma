using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 200f;

    float targetRotationY;

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;

        targetRotationY += mouseX;

        transform.rotation = Quaternion.Euler(0f, targetRotationY, 0f);
    }
}
