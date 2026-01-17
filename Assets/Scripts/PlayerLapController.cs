using UnityEngine;
using Mirror;

public class PlayerLapController : NetworkBehaviour
{
    [SyncVar] public int currentLap = 1;
    [SyncVar] public bool hasFinished = false;

    // --- 시간 측정용 변수 추가 ---
    [SyncVar] private float startTime;
    private float raceTime;
    private bool isRacing = false;
    // ---------------------------

    private bool passedHalfWay = false;

    // 로컬 플레이어가 시작될 때 시간을 서버로부터 동기화하거나 초기화
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        currentLap=1;
        CmdRequestStartTime(); // 서버에 시작 시간 요청
    }

    [Command]
    void CmdRequestStartTime()
    {
        startTime = (float)NetworkTime.time;
        isRacing = true;
    }

    void Update()
    {
        // 로컬 플레이어이고, 달리는 중이고, 아직 안 끝났을 때만 시간 계산
        if (!isLocalPlayer || !isRacing || hasFinished) return;

        raceTime = (float)NetworkTime.time - startTime;
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

        // 운전 스크립트가 있다면 꺼버리기
        var car = GetComponent<CarController2D>();
        if (car != null) car.enabled = false;
    }
}