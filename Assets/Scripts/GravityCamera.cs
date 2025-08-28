using UnityEngine;

public class GravityCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 defaultOffset = new Vector3(0, 3, -10);
    [SerializeField] float followSpeed = 5f;
    [SerializeField] float rotateSpeed = 5f;

    GravityCube cube;

    Quaternion targetRotation;  
    Vector3 frozenOffset;   
    bool wasGrounded;
    bool isOffsetFrozen = false;

    void Start()
    {
        if (target != null)
            cube = target.GetComponent<GravityCube>();

        targetRotation = transform.rotation;
        frozenOffset = defaultOffset;
    }

    void LateUpdate()
    {
        if (!target || cube == null) return;

        bool gravityKeyPressed = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) ||
                                 Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.F);

        if (!cube.IsGrounded && wasGrounded)
        {
            isOffsetFrozen = false;
        }
        else if (!cube.IsGrounded && gravityKeyPressed && !isOffsetFrozen)
        {
            frozenOffset = transform.position - target.position;
            isOffsetFrozen = true; 
        }
        else if (cube.IsGrounded && !wasGrounded)
        {
            targetRotation = Quaternion.LookRotation(target.forward, cube.SurfaceUp);
            isOffsetFrozen = false;
        }

        wasGrounded = cube.IsGrounded;

        Vector3 desiredPos;
        if (cube.IsGrounded)
        {
            desiredPos = target.position + target.rotation * defaultOffset;
        }
        else
        {
            if (isOffsetFrozen)
            {
                desiredPos = target.position + frozenOffset;
            }
            else
            {
                desiredPos = target.position + target.rotation * defaultOffset;
            }
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }
}
