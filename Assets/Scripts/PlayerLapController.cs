using UnityEngine;
using Mirror;

public class PlayerLapController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnCurrentLapChanged))] public int currentLap = 1;
    [SyncVar] public bool hasFinished = false;

    // --- 시간 측정용 변수 추가 ---
    // --- 시간 측정용 변수 추가 ---
    [SyncVar] private double startTime; // NetworkTime은 double을 반환함
    [SyncVar] private bool isRacing = false;
    private float raceTime;
    // ---------------------------

    private bool passedHalfWay = false;

    // 로컬 플레이어가 시작될 때 시간을 서버로부터 동기화하거나 초기화
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }

    // [NEW] RaceManager에서 호출: 카운트다운 끝나면 타이머 시작
    public void StartRacing()
    {
        CmdRequestStartTime();
    }

    void OnCurrentLapChanged(int oldLap, int newLap)
    {
        Debug.Log($"[Client] 바퀴 수 변경: {oldLap} -> {newLap}");
    }

    [Command]
    void CmdRequestStartTime()
    {
        startTime = NetworkTime.time;
        isRacing = true;
    }

    // [DEBUG] 강제 완주 (F3)
    [Command]
    void CmdDebugForceFinish()
    {
        if (LapManager.instance != null)
        {
            currentLap = LapManager.instance.totalLaps;
            ProcessLap(); // 막바퀴 상태에서 호출하면 즉시 완주 처리됨
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // [DEBUG] F3 누르면 즉시 완주
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CmdDebugForceFinish();
        }

        // 달리는 중이고, 아직 안 끝났을 때만 시간 계산
        if (!isRacing || hasFinished) return;
        
        // [FIX] NetworkTime을 사용하여 서버 시간 기준으로 경과 시간 계산
        raceTime = (float)(NetworkTime.time - startTime);
    }

    // UI에서 이 함수를 불러서 시간을 가져감
    public float GetRaceTime()
    {
        return raceTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer) return;

        if (collision.CompareTag("HalfWay"))
        {
            passedHalfWay = true;
        }

        if (collision.CompareTag("FinishLine"))
        {
            if (passedHalfWay)
            {
                ProcessLap();
                passedHalfWay = false;
            }
            else
            {
                Debug.Log("역주행 혹은 중간 지점 미통과!");
            }
        }
    }

    [Server]
    void ProcessLap()
    {
        if (LapManager.instance == null)
        {
            Debug.LogError("LapManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        if (currentLap < LapManager.instance.totalLaps)
        {
            currentLap++;
            Debug.Log($"바퀴수 증가: {currentLap}");
        }
        else if (!hasFinished)
        {
            hasFinished = true;
            isRacing = false; // 시간 멈춤
            LapManager.instance.OnPlayerFinished(netId.ToString());
            RpcStopVehicle();
        }
    }

    [ClientRpc]
    void RpcStopVehicle()
    {
        StopVehicle();
    }

    public void StopVehicle()
    {
        isRacing = false; // 클라이언트에서도 시간 멈춤
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearDamping = 10f;
        }

        // 운전 스크립트가 있다면 완주 처리
        var car = GetComponent<CarController2D>();
        if (car != null) 
        {
            car.FinishRace(); // [FIX] 0.8초 딜레이 후 정지 함수 호출
        }
    }
}