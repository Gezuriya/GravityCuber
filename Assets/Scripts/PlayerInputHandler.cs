using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInputData InputData { get; private set; } = new PlayerInputData();

    void Update()
    {
        GatherInput();
    }
    private void GatherInput()
    {
        ResetInput();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        InputData.MoveInput = (cameraForward * vertical + cameraRight * horizontal).normalized;

        SetInputFlags();
    }

    private void ResetInput()
    {
        InputData.MoveInput = Vector3.zero;
        InputData.JumpPressed = false;
        InputData.GravDownPressed = false;
        InputData.GravUpPressed = false;
        InputData.GravLeftPressed = false;
        InputData.GravRightPressed = false;
    }
    private void SetInputFlags()
    {
        InputData.JumpPressed = Input.GetKeyDown(KeyCode.Space);
        InputData.GravDownPressed = Input.GetKeyDown(KeyCode.F);
        InputData.GravUpPressed = Input.GetKeyDown(KeyCode.R);
        InputData.GravLeftPressed = Input.GetKeyDown(KeyCode.Q);
        InputData.GravRightPressed = Input.GetKeyDown(KeyCode.E);
    }
}
[System.Serializable]
public class PlayerInputData
{
    public Vector3 MoveInput;
    public bool JumpPressed;
    public bool GravDownPressed;
    public bool GravUpPressed;
    public bool GravLeftPressed;
    public bool GravRightPressed;
}