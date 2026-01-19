using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("얼마나 빨라질지 (예: 20)")]
    public float boostAmount = 20f;

    [Tooltip("지속 시간 (예: 1.5초)")]
    public float duration = 1.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 충돌한 물체에서 CarController2D를 찾습니다.
        CarController2D car = collision.GetComponent<CarController2D>();

        // 2. 차가 맞고, 그 차가 '내 차(LocalPlayer)'일 때만 발동
        // (멀티플레이어 게임이라 내 화면에서 내 차가 밟았을 때만 처리해야 함)
        if (car != null && car.isLocalPlayer)
        {
            Debug.Log("⚡ 속도 발판 밟음!");

            // 아까 만든 '더하기 방식' 부스트 함수 호출!
            car.ApplySpeedBoost(boostAmount, duration);
        }
    }
}