using UnityEngine;
using UnityEngine.UI; // 기본 Text를 위해 필수
using Mirror;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Elements (기본 Text 객체를 드래그하세요)")]
    public Text timerText;
    public Text lapText;
    public Text resultText;

    void Update()
    {
        if (LapManager.instance == null)
        {
            LapManager.instance = GameObject.FindObjectOfType<LapManager>();
            return;
        }

        UpdateLapInfo();
        UpdateTimer();

        if (LapManager.instance.isGameOver)
        {
            ShowFinalResult();
        }
    }

    void UpdateLapInfo()
    {
        if (NetworkClient.localPlayer != null)
        {
            var lp = NetworkClient.localPlayer.GetComponent<PlayerLapController>();
            if (lp != null)
            {
                float t = lp.GetRaceTime();
                int min = (int)(t / 60);
                int sec = (int)(t % 60);
                int msec = (int)((t * 100) % 100);

                // 1. currentLap이 1부터 시작하는지 확인
                // 2. \n을 써서 바퀴 수 아래에 시간 표시
                lapText.text = string.Format("LAPS: {0} / {1}\n{2:00}:{3:00}.{4:02}",
                    lp.currentLap,
                    LapManager.instance != null ? LapManager.instance.totalLaps : 3,
                    min, sec, msec);
            }
        }
        else
        {
            // 플레이어 소환 전 기본 표시
            lapText.text = "LAPS: 1 / 3\n00:00.00";
        }
    }
    void UpdateTimer()
    {
        if (!string.IsNullOrEmpty(LapManager.instance.winnerNetId) && !LapManager.instance.isGameOver)
        {
            if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            timerText.text = "LIMIT: " + LapManager.instance.remainingTime.ToString("F1") + "s";
        }
        else
        {
            if (timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
        }
    }

    void ShowFinalResult()
    {
        if (resultText == null || resultText.gameObject.activeSelf) return;
        resultText.gameObject.SetActive(true);

        var lp = NetworkClient.localPlayer?.GetComponent<PlayerLapController>();
        string myId = NetworkClient.localPlayer.netId.ToString();
        bool isWinner = (myId == LapManager.instance.winnerNetId);

        if (isWinner)
        {
            resultText.text = "YOU WIN!\n(+100 Coin)\n";
            resultText.color = Color.yellow;
        }
        else if (lp != null && lp.hasFinished)
        {
            resultText.text = "GOAL IN!\n(+50 Coin)\n";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "RETIRED\n(+50 Coin)\n";
            resultText.color = Color.red;
        }

        // [NEW] 보상 지급 및 로비 복귀 프로세스 시작
        StartCoroutine(ProcessEndGame(isWinner, lp != null && lp.hasFinished));
    }

    System.Collections.IEnumerator ProcessEndGame(bool isWinner, bool hasFinished)
    {
        // 1. 보상 계산 (우승 100, 나머지 50)
        int rewardAmount = 50; 
        if (isWinner) rewardAmount = 100;

        Debug.Log($"[GameEnd] 결과: 승리={isWinner}, 완주={hasFinished} -> 보상: {rewardAmount}");

        // 2. 서버에 보상 요청
        string nickname = PlayerPrefs.GetString("Nickname", "");
        if (!string.IsNullOrEmpty(nickname))
        {
            RewardRequest reqData = new RewardRequest { nickname = nickname, amount = rewardAmount };
            string json = JsonUtility.ToJson(reqData);

            string url = $"http://{MainMenuController.GetServerIP()}:3000/reward";
            var request = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[GameEnd] 보상 {rewardAmount}원 지급 성공!");
            }
            else
            {
                Debug.LogError("[GameEnd] 보상 지급 실패: " + request.error);
            }
        }

        // 3. 3초 대기 (결과 화면 감상)
        yield return new WaitForSeconds(3f);

        // 4. 로비로 복귀
        Debug.Log("[GameEnd] 로비로 돌아갑니다.");
        if (NetworkServer.active) Mirror.NetworkManager.singleton.StopHost();
        else Mirror.NetworkManager.singleton.StopClient();

        // [FIX] Mirror가 씬을 안 바꿔주면 강제로 이동 (0번 씬이 보통 메인 메뉴)
        yield return new WaitForSeconds(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }

    [System.Serializable]
    public class RewardRequest
    {
        public string nickname;
        public int amount;
    }
}