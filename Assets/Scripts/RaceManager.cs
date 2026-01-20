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
    // ★ 기존의 개별 사운드(countSound, goSound)는 지우고 하나로 합침
    public AudioClip fullCountdownClip; // "3, 2, 1, 출발~!"이 통째로 들어있는 파일

    // 인스펙터에 넣을 필요 없음 (코드로 찾음)
    private CarController2D myCarController;

    IEnumerator Start()
    {
        // 1. 내 차(Local Player)가 생성될 때까지 기다림
        while (myCarController == null)
        {
            FindMyCar();
            yield return null;
        }

        // 2. 카운트다운 시작
        yield return StartCoroutine(RoutineCountdown());
    }

    void FindMyCar()
    {
        CarController2D[] cars = FindObjectsByType<CarController2D>(FindObjectsSortMode.None);

        foreach (var car in cars)
        {
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
        if (myCarController != null) myCarController.enabled = false;

        countdownText.text = "준비...";
        yield return new WaitForSeconds(0.5f); // 잠시 대기

        // ====================================================
        // ★ 여기서 "3, 2, 1, 출발!" 전체 사운드를 한 번만 재생
        // ====================================================
        if (audioSource != null && fullCountdownClip != null)
        {
            audioSource.PlayOneShot(fullCountdownClip);
        }

        // --- 3 (소리는 이미 시작됨) ---
        countdownText.text = "3";
        yield return new WaitForSeconds(1.0f); // 1초 기다림

        // --- 2 ---
        countdownText.text = "2";
        yield return new WaitForSeconds(1.0f); // 1초 기다림

        // --- 1 ---
        countdownText.text = "1";
        yield return new WaitForSeconds(1.0f); // 1초 기다림

        // --- 출발! ---
        countdownText.text = "시작!";
        // 여기서는 소리 재생 안 함 (아까 재생한 게 이어지고 있을 테니까)

        // --- 조작 풀기 ---
        if (myCarController != null)
        {
            myCarController.enabled = true;

            // 타이머 시작
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
}