using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Legacy Text용
using Mirror; // Mirror 기능 사용

public class RaceManager : NetworkBehaviour
{
    [Header("UI 연결")]
    public Text countdownText; // 꼭 Legacy Text를 연결하세요

    [Header("사운드 설정")]
    public AudioSource audioSource;
    // ★ 기존의 개별 사운드(countSound, goSound)는 지우고 하나로 합침
    public AudioClip fullCountdownClip; // "3, 2, 1, 출발~!"이 통째로 들어있는 파일

    // 인스펙터에 넣을 필요 없음 (코드로 찾음)
    private CarController2D myCarController;

    // [NEW] RPC 대신 SyncVar로 상태 동기화 (재접속/재시작 시 안정성 확보)
    [SyncVar(hook = nameof(OnRaceStateChanged))]
    public bool isRaceStarted = false;

    // [NEW] 서버가 시작되면 실행 (호스트 기준)
    public override void OnStartServer()
    {
        base.OnStartServer();
        // 씬 로딩 등을 고려해 2초 정도 대기 후 시작 신호 보냄
        StartCoroutine(ServerWaitAndStart());
    }

    IEnumerator ServerWaitAndStart()
    {
        Debug.Log("[Server] 플레이어 생성 대기 중...");

        float timeout = 15f; // 최대 15초까지만 기다림
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            int playerCount = NetworkManager.singleton.numPlayers;
            int carCount = FindObjectsByType<CarController2D>(FindObjectsSortMode.None).Length;

            if (playerCount > 0 && carCount >= playerCount)
            {
                Debug.Log($"[Server] 모든 플레이어({playerCount}명) 준비 완료!");
                break; 
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[Server] 카운트다운 신호 전송 (SyncVar)!");
        isRaceStarted = true; // [Change] RPC 호출 대신 변수 변경
    }

    // [NEW] SyncVar 훅: 값이 바뀌면 클라이언트에서 자동 실행
    void OnRaceStateChanged(bool oldState, bool newState)
    {
        if (newState == true)
        {
            StartCoroutine(RoutineCountdown());
        }
    }

    // 기존 로직을 Rpc에서 호출
    IEnumerator RoutineCountdown()
    {
        // 1. 내 차(Local Player)가 생성될 때까지 기다림 (안전을 위해 여기서도 체크)
        while (myCarController == null)
        {
            FindMyCar();
            yield return null;
        }

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
            // [Fix] 사운드 출력구 연결 (혹시 안되어있을 경우 대비)
            // if (AudioManager.instance != null) audioSource.outputAudioMixerGroup = ... 
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
}