using UnityEngine;
using Mirror; // 👈 Unity.Netcode 대신 이거 씁니다!

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 12f;
    public float maxSpeed = 15f;
    public float rotationSpeed = 8f;

    [Header("스킬 설정")]
    public float dashForce = 8f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastDirection;

    // 1️⃣ [Mirror 전용] 내 캐릭터가 시작될 때 실행되는 함수
    public override void OnStartLocalPlayer()
    {
        // 카메라 연결 (내 캐릭터만!)
        Camera.main.GetComponent<CameraFollow2D>().target = transform;

        // 내 차임을 표시하기 위해 색깔 변경 (테스트용)
        GetComponent<SpriteRenderer>().color = Color.green;
        Debug.Log("🟢 [Mirror] 내 캐릭터 로드 완료!");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 1.5f;
    }

    void Update()
    {
        // 2️⃣ [Mirror 전용] 내 캐릭터가 아니면 조종 금지
        if (!isLocalPlayer) return;

        // 이동 입력
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (moveInput.magnitude > 0.1f)
            lastDirection = moveInput;

        // 스킬 (Z키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            CmdDash(); // 대시는 서버한테 "나 대시할래!"라고 명령(Command)을 보냄
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        Move();
        LimitSpeed();
    }

    void Move()
    {
        if (moveInput.magnitude < 0.1f) return;
        rb.linearVelocity += moveInput * moveSpeed * Time.fixedDeltaTime;

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

    // 3️⃣ [Mirror 전용] 서버에게 명령 보내기 (Command)
    // 클라이언트가 호출하지만, 실제 실행은 서버에서 됨 -> 다른 사람들에게도 동기화
    [Command]
    void CmdDash()
    {
        // 서버에서 물리 힘을 가함
        RpcDashEffect(); // 모든 클라이언트에게 이펙트 보여주라고 지시
    }

    // [Mirror 전용] 모든 클라이언트에게 실행 (ClientRpc)
    [ClientRpc]
    void RpcDashEffect()
    {
        // 여기서 대시 힘을 가하거나 이펙트 재생
        rb.AddForce(transform.up * dashForce, ForceMode2D.Impulse);
    }
}