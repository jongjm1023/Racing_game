using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI; // UI 사용을 위해 추가
using System.Collections.Generic;

public enum ItemType { Boost, Missile, Banana }

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float acceleration = 20f; // 가속력 약간 증가
    public float maxSpeed = 15f;
    public float turnSpeed = 200f;
    public float brakePower = 0.95f;

    [Header("대시 & 스태미나")]
    public float dashForce = 15f;     // 지속 대시 힘
    public float maxStamina = 100f;   // 최대 체력
    public float staminaDrain = 30f;  // 초당 소모량 (약 3.3초면 바닥남)
    public float staminaRegen = 10f;  // 초당 회복량
    public float overheatDuration = 2.0f; // 오버히트 페널티 시간

    [Header("UI 연결 (캔버스에 있는 Slider 혹은 Image)")]
    public Slider staminaSlider;      // 인스펙터에서 연결하세요

    // 내부 변수
    private float currentStamina;
    private bool isOverheated = false; // 오버히트 상태인가?
    private float overheatTimer = 0f;

    private Rigidbody2D rb;
    private float moveInput;
    private float turnInput;
    private Queue<ItemType> itemQueue = new Queue<ItemType>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // 카메라 연결
            Camera cam = Camera.main;
            if (cam != null)
            {
                CameraFollow2D camScript = cam.GetComponent<CameraFollow2D>();
                if (camScript != null) camScript.target = this.transform;
            }

            // 내 UI 찾아서 연결 (태그나 이름으로 찾기 예시)
            // 만약 씬에 "StaminaSlider"라는 이름의 슬라이더가 있다면 자동 연결
            if (staminaSlider == null)
            {
                GameObject sliderObj = GameObject.Find("StaminaSlider");
                if (sliderObj != null) staminaSlider = sliderObj.GetComponent<Slider>();
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Top-Down 게임이므로 중력 0 필수
        rb.gravityScale = 0; 
        rb.linearDamping = 2f; // 마찰력 (너무 낮으면 얼음판 같음)
        rb.angularDamping = 3f;
        
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!IsOwner) return;

        // 1. 오버히트 체크
        if (isOverheated)
        {
            HandleOverheat();
            return; // 오버히트 중이면 조작 불가 (아래 코드 실행 안 함)
        }

        // 2. 키 입력 받기
        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");
        
        // 아이템 사용
        if (Input.GetKeyDown(KeyCode.X)) UseItem();

        // 3. 스태미나 관리 및 대시 입력 처리
        HandleStamina();
        
        // UI 업데이트
        UpdateUI();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // 오버히트 상태면 움직임 불가 (서서히 멈춤)
        if (isOverheated)
        {
            rb.linearVelocity *= 0.9f; // 강제 감속
            return; 
        }

        Move();
        LimitSpeed();
    }

    // ==========================================
    // 로직 함수들
    // ==========================================

    void HandleOverheat()
    {
        overheatTimer -= Time.deltaTime;
        
        // 오버히트 UI 표시 (예: 빨간색으로 깜빡이거나 0으로 고정)
        if (staminaSlider != null) staminaSlider.value = 0;

        if (overheatTimer <= 0)
        {
            isOverheated = false;
            currentStamina = 30f; // 패널티 끝난 후 약간 회복된 상태로 시작
            Debug.Log("오버히트 해제! 다시 이동 가능");
        }
    }

    void HandleStamina()
    {
        // Z키를 '누르고 있는' 동안 (GetKey) && 스태미나가 있을 때
        bool isDashing = Input.GetKey(KeyCode.Z) && currentStamina > 0;

        if (isDashing)
        {
            // 스태미나 소모
            currentStamina -= staminaDrain * Time.deltaTime;

            // 지속적으로 앞방향 힘 추가 (부스터 효과)
            rb.AddForce(transform.up * dashForce, ForceMode2D.Force);

            // 스태미나 바닥남 -> 오버히트 발동!
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isOverheated = true;
                overheatTimer = overheatDuration;
                Debug.Log("🔥 엔진 과열! 2초간 멈춤!");
            }
        }
        else
        {
            // 대시 안 쓸 때는 스태미나 자동 회복
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegen * Time.deltaTime;
            }
        }
    }

    void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Move()
    {
        // 전진 / 후진
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            rb.AddForce(transform.up * moveInput * acceleration);
        }

        // 회전 (속도가 조금이라도 있을 때만)
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            float direction = moveInput < 0 ? 1 : -1; // 후진 시 핸들 반대
            float turn = turnInput * turnSpeed * Time.fixedDeltaTime * direction;
            rb.MoveRotation(rb.rotation + turn);
        }
    }

    void LimitSpeed()
    {
        // 대시 중이 아닐 때만 속도 제한 (대시 중엔 한계 돌파 가능하게 할지 선택)
        // 여기선 대시 중에도 너무 빨라지지 않게 maxSpeed를 조금 늘려줌
        float currentLimit = Input.GetKey(KeyCode.Z) ? maxSpeed * 1.5f : maxSpeed;

        if (rb.linearVelocity.magnitude > currentLimit)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentLimit;
        }
    }
    
    // (아이템 관련 코드는 이전과 동일하게 유지 - 생략함)
    void UseItem() { /* 이전 답변 코드 복붙하시면 됩니다 */ }
}