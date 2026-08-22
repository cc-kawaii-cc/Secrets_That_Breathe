using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;
    [Header("Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float gravity = -19.62f;
    public float jumpHeight = 2f;
    Vector3 velocity;
    bool isGrounded;

    // ---- state ที่ระบบอื่นอ่านได้ (หมอบ, ยาม, เสียงฝีเท้า) ----
    [HideInInspector] public float speedMultiplier = 1f;
    public bool IsSprinting { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get { return isGrounded; } }
    /// <summary>ความเร็วแนวราบจริงเมื่อเฟรมที่แล้ว (m/s)</summary>
    public float PlanarSpeed { get; private set; }
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    /// <summary>
    /// ล้างความเร็วที่ค้างอยู่ เรียกหลังวาร์ปตัวละคร
    /// ไม่งั้นความเร็วตกจากจุดเดิมจะติดมาด้วย ผู้เล่นจะร่วงทะลุพื้นทันทีที่โผล่
    /// </summary>
    public void ResetMotion()
    {
        velocity = Vector3.zero;
        speedMultiplier = 1f;
        IsMoving = false;
        IsSprinting = false;
        PlanarSpeed = 0f;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        Vector2 inputVector = moveAction.action.ReadValue<Vector2>();
        float x = inputVector.x;
        float z = inputVector.y;
        bool isSprinting = sprintAction.action.IsPressed();
        float currentSpeed = (isSprinting ? runSpeed : walkSpeed) * speedMultiplier;
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        IsMoving = move.sqrMagnitude > 0.01f;
        IsSprinting = isSprinting && IsMoving;
        PlanarSpeed = IsMoving ? currentSpeed : 0f;
        if (jumpAction.action.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    private void OnEnable() 
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
    }
    private void OnDisable() 
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
    }
}