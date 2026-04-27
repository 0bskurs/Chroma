using UnityEngine;

public class FloattingTextTestObject : MonoBehaviour
{
    public Transform followObject;
    public Transform lookAtObject;

    public Vector3 offset = new Vector3(0, 2f, 0);

    void LateUpdate()
    {
        if (followObject == null) return;

        
        transform.position = followObject.position + offset;

        if (lookAtObject != null)
        {
           
            Vector3 direction = transform.position - lookAtObject.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
