using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    public AudioClip sceneMusic; // 여기에 씬에 맞는 음악 파일을 드래그해서 넣으세요

    void Start()
    {
        // 게임 시작하자마자 매니저에게 음악 재생 요청
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(sceneMusic);
        }
    }
}