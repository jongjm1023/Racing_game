using UnityEngine;
using Mirror;

public class PlayerLapController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnCurrentLapChanged))] public int currentLap = 1;
    [SyncVar] public bool hasFinished = false;

    // --- 시간 측정용 변수 ---
    [SyncVar] private double startTime;
    [SyncVar] private bool isRacing = false;
    private float raceTime;
    // -----------------------

    // [NEW] 오디오 설정 변수 추가
    [Header("Audio Settings")]
    public AudioClip lapSoundClip; // 인스펙터에서 오디오 클립 연결
    private AudioSource audioSource;

    private bool passedHalfWay = false;

    void Start()
    {
        // 오디오 소스 가져오기 (없으면 추가)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }

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

    [Command]
    void CmdDebugForceFinish()
    {
        if (LapManager.instance != null)
        {
            currentLap = LapManager.instance.totalLaps;
            ProcessLap();
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.F3))
        {
            CmdDebugForceFinish();
        }

        if (!isRacing || hasFinished) return;

        raceTime = (float)(NetworkTime.time - startTime);
    }

    public float GetRaceTime()
    {
        return raceTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌 감지는 서버에서만 수행
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

        // [NEW] 바퀴를 돌 때마다 소리 재생 명령을 보냄 (완주 여부 상관없이 소리 남)
        RpcPlayLapSound();

        if (currentLap < LapManager.instance.totalLaps)
        {
            currentLap++;
            Debug.Log($"바퀴수 증가: {currentLap}");
        }
        else if (!hasFinished)
        {
            hasFinished = true;
            isRacing = false;
            LapManager.instance.OnPlayerFinished(netId.ToString());
            RpcStopVehicle();
        }
    }

    // [NEW] 서버가 클라이언트들에게 소리를 재생하라고 시키는 함수
    [ClientRpc]
    void RpcPlayLapSound()
    {
        if (audioSource != null && lapSoundClip != null)
        {
            audioSource.PlayOneShot(lapSoundClip);
        }
    }

    [ClientRpc]
    void RpcStopVehicle()
    {
        StopVehicle();
    }

    public void StopVehicle()
    {
        isRacing = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearDamping = 10f;
        }

        var car = GetComponent<CarController2D>();
        if (car != null)
        {
            car.FinishRace();
        }
    }
}