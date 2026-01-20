using UnityEngine;
using UnityEngine.Audio; // [NEW] 오디오 믹서 사용

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    
    [Header("오디오 믹서 연결")]
    public AudioMixer audioMixer; // [NEW] 유니티 오디오 믹서 연결

    public AudioSource bgmSource;
    public AudioSource sfxSource;

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 저장된 볼륨 불러오기 (없으면 1.0)
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        // 믹서에 적용 (약간의 딜레이가 필요할 수 있어 Start에서 호출)
        ApplyVolume();
    }

    void ApplyVolume()
    {
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    // 씬에서 음악을 요청할 때 사용하는 함수
    public void PlayBGM(AudioClip newClip)
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == newClip) return;

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

    // 볼륨 조절 함수 (슬라이더 0 ~ 1 값)
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        
        // 믹서 볼륨은 데시벨(dB) 단위이므로 로그 변환 필요 (-80 ~ 0)
        // volume이 0이면 -80dB로 설정
        float db = (volume <= 0.001f) ? -80f : Mathf.Log10(volume) * 20;

        if (audioMixer != null) audioMixer.SetFloat("BGM", db);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;

        float db = (volume <= 0.001f) ? -80f : Mathf.Log10(volume) * 20;
        
        if (audioMixer != null) audioMixer.SetFloat("SFX", db);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
}