using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 어디서든 접근 가능하게 만듦
    public AudioSource bgmSource;

    private void Awake()
    {
        // 1. 싱글톤 패턴: 게임 내에 AudioManager가 하나만 있게 유지
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀔 때 파괴되지 않음!
        }
        else
        {
            Destroy(gameObject); // 이미 있으면 새로 생긴 건 삭제
        }
    }

    // 씬에서 음악을 요청할 때 사용하는 함수
    public void PlayBGM(AudioClip newClip)
    {
        // 2. 지금 재생 중인 노래랑 똑같은 노래면 굳이 다시 틀지 않음 (이어지게)
        if (bgmSource.clip == newClip)
            return;

        // 다른 노래라면 교체하고 재생
        bgmSource.clip = newClip;
        bgmSource.Play();
    }
}