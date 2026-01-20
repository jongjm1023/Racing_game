using UnityEngine;

public class Particle : MonoBehaviour
{
    private ParticleSystem dustEffect; // public 말고 private으로 숨김
    private Rigidbody2D rb;

    void Awake() // Start보다 먼저 실행
    {
        rb = GetComponent<Rigidbody2D>();

        // 내 자식들 중에 ParticleSystem이 있으면 알아서 찾아와라!
        dustEffect = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        // 혹시 모르니 null 체크 (파티클 없으면 에러 안 나게)
        if (dustEffect == null) return;

        var emission = dustEffect.emission;

        // 속도가 1.0 이상이면 연기 뿜기
        if (rb.linearVelocity.magnitude > 1.0f)
        {
            emission.enabled = true;
        }
        else
        {
            emission.enabled = false;
        }
    }
}