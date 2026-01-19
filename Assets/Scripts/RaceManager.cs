using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Legacy Text용
using Mirror; // Mirror 기능 사용

public class RaceManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Text countdownText; // 꼭 Legacy Text를 연결하세요

    [Header("사운드 설정")]
    public AudioSource audioSource;
    public AudioClip countSound; // 삑
    public AudioClip goSound;    // 삐익

    // 인스펙터에 넣을 필요 없음 (코드로 찾음)
    private CarController2D myCarController;

    IEnumerator Start()
    {
        // 1. 내 차(Local Player)가 생성될 때까지 기다림 (중요!)
        // 멀티플레이는 로딩 후 차가 생성되기까지 0.1~0.5초 정도 걸릴 수 있음
        while (myCarController == null)
        {
            FindMyCar();
            yield return null; // 못 찾았으면 다음 프레임에 다시 찾기
        }

        // 2. 카운트다운 시작
        yield return StartCoroutine(RoutineCountdown());
    }

    // 내 차(isLocalPlayer가 true인 차)를 찾는 함수
    void FindMyCar()
    {
        // 씬에 있는 모든 CarController2D를 가져옴
        CarController2D[] cars = FindObjectsByType<CarController2D>(FindObjectsSortMode.None);

        foreach (var car in cars)
        {
            // Mirror가 제공하는 변수: 이 차가 내 컴퓨터의 차인가?
            if (car.isLocalPlayer)
            {
                myCarController = car;
                break;
            }
        }
    }

    IEnumerator RoutineCountdown()
    {
        // --- 조작 얼리기 ---
        // CarController2D 스크립트를 끄면 Update가 안 돌아가서 움직임이 멈춤
        if (myCarController != null) myCarController.enabled = false;

        countdownText.text = "준비...";
        yield return new WaitForSeconds(0.5f); // 잠시 대기

        // --- 3 ---
        countdownText.text = "3";
        PlaySound(countSound);
        yield return new WaitForSeconds(1.0f);

        // --- 2 ---
        countdownText.text = "2";
        PlaySound(countSound);
        yield return new WaitForSeconds(1.0f);

        // --- 1 ---
        countdownText.text = "1";
        PlaySound(countSound);
        yield return new WaitForSeconds(1.0f);

        // --- 출발! ---
        countdownText.text = "시작!";
        PlaySound(goSound);

        // --- 조작 풀기 ---
        if (myCarController != null) 
        {
            myCarController.enabled = true;

            // [NEW] 타이머 시작 (카운트다운 동안 멈춰있었음)
            var lapController = myCarController.GetComponent<PlayerLapController>();
            if (lapController != null) 
            {
                lapController.StartRacing();
            }
        }

        // 텍스트 끄기
        yield return new WaitForSeconds(1.0f);
        countdownText.gameObject.SetActive(false);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}