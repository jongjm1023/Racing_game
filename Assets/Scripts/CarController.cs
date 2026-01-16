using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 12f;
    public float maxSpeed = 15f;
    public float rotationSpeed = 8f;

    [Header("스킬 설정")]
    public float dashForce = 8f;
    public float driftControl = 0.95f; // 드리프트 안정도

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 1.5f; // 자연스러운 마찰
    }

    void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        if (moveInput.magnitude > 0.1f)
            lastDirection = moveInput;

        HandleSkills();
    }

    void FixedUpdate()
    {
        Move();
        LimitSpeed();
    }

    void Move()
    {
        if (moveInput.magnitude < 0.1f) return;

        // 현재 속도에 가속 추가
        rb.linearVelocity += moveInput * moveSpeed * Time.fixedDeltaTime;

        // 차체 회전
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = Mathf.LerpAngle(rb.rotation, angle, rotationSpeed * Time.fixedDeltaTime);
    }

    void LimitSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    void HandleSkills()
    {
        if (!Input.GetKeyDown(KeyCode.Z)) return;
        if (rb.linearVelocity.magnitude < 1f) return;

        float dot = Vector2.Dot(rb.linearVelocity.normalized, moveInput);

        // 🔥 드리프트 (현재 이동방향과 반대 입력)
        if (dot < -0.2f && moveInput.magnitude > 0.1f)
        {
            Debug.Log("Drift!");

            // 측면 미끄러짐 제거 → 안정적인 커브
            Vector2 forward = rb.linearVelocity.normalized;
            rb.linearVelocity = forward * rb.linearVelocity.magnitude * driftControl;

            // 방향 보정 가속
            rb.AddForce(moveInput * dashForce, ForceMode2D.Impulse);
        }
        else
        {
            // 일반 대시
            rb.AddForce(lastDirection * dashForce, ForceMode2D.Impulse);
        }
    }
}
