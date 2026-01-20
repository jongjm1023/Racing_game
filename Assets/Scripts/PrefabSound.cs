using UnityEngine;

public class PrefabSound : MonoBehaviour
{
    // 효과음 파일(AudioClip)을 Inspector에서 넣을 수 있게 변수 선언
    public AudioClip soundClip;

    // 오디오 소스 컴포넌트
    private AudioSource audioSource;

    void Start()
    {
        // 이 스크립트가 붙은 개체의 AudioSource를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
    }

    // 마우스로 이 개체의 Collider를 클릭했을 때 실행되는 함수
    void OnMouseDown()
    {
        // 소리가 할당되어 있고, 오디오 소스가 있다면 재생
        if (soundClip != null && audioSource != null)
        {
            // PlayOneShot은 소리가 겹쳐도 끊기지 않고 재생됩니다.
            audioSource.PlayOneShot(soundClip);
        }
    }
}