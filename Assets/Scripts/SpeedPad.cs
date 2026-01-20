using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    [Header("설정")]
    public float boostAmount = 20f;  // 밟으면 속도 +20
    public float duration = 1.5f;    // 1.5초 유지

    [Header("오디오")] // 여기에 소리 관련 설정 추가
    public AudioSource audioSource;

    private void Start()
    {
        // 만약 Inspector에서 AudioSource를 직접 연결하지 않았다면,
        // 이 오브젝트(SpeedPad)에 붙어있는 AudioSource를 자동으로 찾아서 연결함
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CarController2D car = collision.GetComponent<CarController2D>();

        // 내 차만 발동
        if (car != null && car.isLocalPlayer)
        {
            Debug.Log("⚡ 발판 부스트!");
            car.ApplySpeedBoost(boostAmount, duration);

            // --- 소리 재생 코드 시작 ---
            if (audioSource != null)
            {
                // Play()는 소리를 재생함. PlayOneShot()을 쓰면 소리가 겹쳐도 자연스러움
                audioSource.Play();
            }
            // ------------------------
        }
    }
}