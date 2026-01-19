using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 어디서든 접근 가능하게 만듦
    public AudioSource bgmSource;
    public AudioSource sfxSource; // 효과음용 소스

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

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

    void Start()
    {
        // 저장된 볼륨 불러오기 (없으면 1.0)
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        ApplyVolume();
    }

    void ApplyVolume()
    {
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    // 씬에서 음악을 요청할 때 사용하는 함수
    public void PlayBGM(AudioClip newClip)
    {
        if (bgmSource == null) return;

        // 2. 지금 재생 중인 노래랑 똑같은 노래면 굳이 다시 틀지 않음 (이어지게)
        if (bgmSource.clip == newClip)
            return;

        // 다른 노래라면 교체하고 재생
        bgmSource.clip = newClip;
        bgmSource.Play();
    }

    // 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // 볼륨 조절 함수 (슬라이더에서 호출)
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    // 현재 볼륨 가져오기 (UI 초기화용)
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
}