using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 720f;

    [Header("시각적 회전 대상")]
    public Transform visualTransform; // 캐릭터 이미지가 담긴 자식 오브젝트를 연결하세요

    private Rigidbody2D rb;
    private Vector2 moveDir;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 만약 인스펙터에서 할당 안했다면 자식 중 첫번째를 자동 할당
        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. 입력 받기 (절대 좌표 기준)
        moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveDir += Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveDir += Vector2.right;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveDir += Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveDir += Vector2.down;

        moveDir = moveDir.normalized;

        // 2. 스프라이트만 회전시키기
        if (moveDir != Vector2.zero)
        {
            HandleVisualRotation(moveDir);
        }
    }

    private void HandleVisualRotation(Vector2 dir)
    {
        if (visualTransform == null) return;

        // 방향 벡터를 각도로 변환
        // Atan2(y, x)는 기본적으로 오른쪽(1,0)이 0도입니다.
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 만약 캐릭터 스프라이트 앞부분이 '위쪽'을 보고 있는 이미지라면 -90도를 해줍니다.
        // 만약 캐릭터 스프라이트 앞부분이 '오른쪽'을 보고 있는 이미지라면 그대로 둡니다.
        float offset = -90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + offset);

        // 자식 오브젝트(이미지)만 부드럽게 회전
        visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (moveDir == Vector2.zero) return;

        // 절대 방향 이동 (회전값에 영향을 받지 않음)
        Vector2 targetPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;
        if (Camera.main != null)
        {
            Vector3 targetPos = transform.position;
            targetPos.z = -10f;
            Camera.main.transform.position = targetPos;
        }
    }
}