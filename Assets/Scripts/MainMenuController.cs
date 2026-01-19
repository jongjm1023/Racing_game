using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class MainMenuController : MonoBehaviour
{
    public UnityEngine.UI.Image currentSkinImage; // 인스펙터에서 연결 필요
    
    // IP 주소 가셔오는 헬퍼 함수
    // [CHANGE] 인스펙터가 아닌 코드로 직접 제어 (private static)
    // 친구 컴퓨터(방장)의 IP를 여기에 직접 적으세요! 
    private static string serverIP = "10.249.18.68"; 

    // IP 주소 가져오는 헬퍼 함수
    public static string GetServerIP()
    {
        return !string.IsNullOrEmpty(serverIP) ? serverIP : "localhost";
    }

    private string nickname = "racer_02"; // 테스트용 닉네임 (나중에 로그인 연동 필요)
    private UserData userData;
    private System.Collections.Generic.List<CharacterData> allCharacters = new System.Collections.Generic.List<CharacterData>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 저장된 닉네임이 있으면 불러오기
        if (PlayerPrefs.HasKey("Nickname"))
        {
            nickname = PlayerPrefs.GetString("Nickname");
            Debug.Log($"[MainMenu] 저장된 닉네임 로드: {nickname}");
            
            // 이미 로그인된 상태라면 패널 숨김, 로그아웃 버튼 보임
            if (loginPanel != null) loginPanel.SetActive(false);
            if (logoutButton != null) logoutButton.SetActive(true);

            if (currentSkinImage != null)
            {
                StartCoroutine(LoadData());
            }
        }
        else
        {
            // 정보가 없으면 로드하지 않고 로그인 대기
            Debug.Log("[MainMenu] 저장된 닉네임이 없습니다. 로그인이 필요합니다.");
            if (loginPanel != null) loginPanel.SetActive(true);
            if (logoutButton != null) logoutButton.SetActive(false);
        }
    }

    // 데이터 로드 코루틴
    System.Collections.IEnumerator LoadData()
    {
        // 1. 캐릭터 목록 조회
        UnityEngine.Networking.UnityWebRequest charRequest = UnityEngine.Networking.UnityWebRequest.Get($"http://{GetServerIP()}:3000/characters");
        yield return charRequest.SendWebRequest();

        if (charRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string json = charRequest.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            allCharacters = new System.Collections.Generic.List<CharacterData>(charList.characters);
        }
        else
        {
            Debug.LogError("캐릭터 목록 로드 실패: " + charRequest.error);
        }

        // 2. 유저 정보 조회
        string userUrl = $"http://{GetServerIP()}:3000/users/" + nickname;
        UnityEngine.Networking.UnityWebRequest userRequest = UnityEngine.Networking.UnityWebRequest.Get(userUrl);
        yield return userRequest.SendWebRequest();

        if (userRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            userData = JsonUtility.FromJson<UserData>(userRequest.downloadHandler.text);
            UpdateCurrentSkinUI();
        }
        else
        {
             // 404 등 에러 발생 시 처리
             if (userRequest.responseCode == 404)
             {
                 Debug.LogWarning("유저 정보를 찾을 수 없습니다. (재로그인 필요)");
             }
             else
             {
                 Debug.LogError("유저 정보 로드 실패: " + userRequest.error);
             }
        }
    }

    // --- 로비 UI 변수 ---
    [Header("Lobby UI")]
    public GameObject lobbyPanel;      // "대기 중..." 패널
    public GameObject cancelMatchButton; // [NEW] 매칭 취소 버튼
    // public TMP_InputField ipInputField; // [REMOVED] 코드 변수로 대체됨

    [Header("Login UI")]
    public GameObject loginPanel; // [NEW] 로그인 화면
    public GameObject logoutButton; // [NEW] 로그아웃 버튼 (로그인 전에는 숨김)

    private bool isGameStarting = false;

    // Update is called once per frame
    void Update()
    {
        // 호스트이고, 게임이 아직 시작 안 했으면 인원 수 체크
        if (Mirror.NetworkServer.active && Mirror.NetworkManager.singleton != null && !isGameStarting)
        {
            int playerCount = Mirror.NetworkManager.singleton.numPlayers;

            // 2명 이상이면 게임 시작!
            if (playerCount >= 2)
            {
                isGameStarting = true;
                Debug.Log("2명이 모였습니다! 게임을 시작합니다.");
                Mirror.NetworkManager.singleton.ServerChangeScene("SampleScene");
            }

            // [테스트용] F1 키를 누르면 혼자서도 강제 시작
            if (Input.GetKeyDown(KeyCode.F1))
            {
               Debug.Log("F1 키 입력: 강제로 게임을 시작합니다.");
               StartCoroutine(CancelMatchRequest());
               Mirror.NetworkManager.singleton.ServerChangeScene("SampleScene"); 
            }
        }

        // [테스트용] F2 키를 누르면 매칭 상태 초기화
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StartCoroutine(ResetMatchState());
        }
    }

    // 매칭 상태 리셋 요청
    System.Collections.IEnumerator ResetMatchState()
    {
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get($"http://{GetServerIP()}:3000/reset_match");
        yield return request.SendWebRequest();
        Debug.Log("서버 매칭 대기열을 초기화했습니다.");
    }

    // 구글 로그인 버튼 클릭 시 연결
    public void OnClickGoogleLogin()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnClickGoogleLogin();
        }
        else
        {
            Debug.LogError("FirebaseAuthManager가 없습니다! 씬에 추가했는지 확인하세요.");
        }
    }

    // 로그인 성공 시 호출됨 (FirebaseAuthManager에서 부름)
    public void OnLoginSuccess(string userNickname)
    {
        Debug.Log($"[MainMenu] 로그인 성공! 환영합니다, {userNickname}님.");
        
        // 로그인 패널 끄기
        if (loginPanel != null) 
        {
            loginPanel.SetActive(false);
            Debug.Log("[MainMenu] LoginPanel을 비활성화했습니다.");
            
            // 로그아웃 버튼 보이기
            if (logoutButton != null) logoutButton.SetActive(true);
        }
        else
        {
            Debug.LogError("[MainMenu] LoginPanel이 null입니다! 인스펙터를 확인하세요.");
        }
        
        // ShopManager 등 다른 매니저 공유를 위해 저장
        PlayerPrefs.SetString("Nickname", userNickname);
        PlayerPrefs.Save();

        // 로그인 성공 후 데이터 로드 시작
        nickname = userNickname; // 로그인한 유저 닉네임으로 설정
        StartCoroutine(LoadData());
    }

    // 로비 UI 표시 로직 (버튼 로직 제거됨)
    void ShowLobbyUI(bool isHost)
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (cancelMatchButton != null) cancelMatchButton.SetActive(true); // 취소 버튼 보이기
    }
    private bool isMatching = false; // 매칭 중복 방지

    // --- 스타트 버튼이 부를 함수 ---
    public void OnClickStart() 
    {
        // 로그인 안 했으면 막기 (선택 사항)
        // if (loginPanel.activeSelf) return;

        if (isMatching) return; // 이미 매칭 중이면 무시

        // 매칭 요청 시작
        StartCoroutine(RequestMatch());
    }

    // --- 매칭 취소 버튼 ---
    public void OnClickCancelMatch()
    {
        if (Mirror.NetworkManager.singleton != null)
        {
            // 호스트였다면 서버에 대기열 취소 요청
            if (Mirror.NetworkServer.active && Mirror.NetworkServer.connections.Count < 2) 
            {
               StartCoroutine(CancelMatchRequest());
            }

            // 네트워크 종료
            if (Mirror.NetworkServer.active) Mirror.NetworkManager.singleton.StopHost();
            else Mirror.NetworkManager.singleton.StopClient();
        }

        ShowLobbyUI_Off(); // UI 끄기
        isMatching = false;
        isGameStarting = false;
    }

    void ShowLobbyUI_Off()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }
    
    System.Collections.IEnumerator CancelMatchRequest()
    {
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get($"http://{GetServerIP()}:3000/cancel_match");
        yield return request.SendWebRequest();
        Debug.Log("서버에 매칭 취소를 요청했습니다.");
    }

    // 매칭 요청 코루틴
    System.Collections.IEnumerator RequestMatch()
    {
        isMatching = true; // 매칭 시작 잠금
        Debug.Log("매칭 시작! 서버에 역할을 요청합니다...");
        
        // 1. 서버에 매칭 요청 (GET /match)
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get($"http://{GetServerIP()}:3000/match");
        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log($"서버 응답: {json}");
            
            MatchResponse response = JsonUtility.FromJson<MatchResponse>(json);

            if (Mirror.NetworkManager.singleton == null)
            {
                Debug.LogError("NetworkManager가 없습니다!");
                yield break;
            }

            if (response.role == "host")
            {
                Debug.Log("당신은 호스트입니다! 방을 만듭니다.");
                Mirror.NetworkManager.singleton.StartHost();
                // 호스트는 로비 UI 보여줌 (누군가 들어오면 시작 버튼 누르기 위해)
                ShowLobbyUI(true); 
            }
            else if (response.role == "client")
            {
                string connectIp = GetServerIP();
                Debug.Log($"당신은 클라이언트입니다! {connectIp}로 접속합니다.");
                Mirror.NetworkManager.singleton.networkAddress = connectIp;
                Mirror.NetworkManager.singleton.StartClient();
                ShowLobbyUI(false); // 클라이언트는 대기
            }
        }
        else
        {
            Debug.LogError("매칭 요청 실패: " + request.error);
        }
    }

    [System.Serializable]
    public class MatchResponse
    {
        public string role;
        public string address;
    }

    // --- 상점 버튼이 부를 함수 ---
    public void OnClickShop()
    {
        SceneManager.LoadScene("SampleScene2");
    }

    // --- 로그아웃 버튼이 부를 함수 ---
    public void OnClickLogout()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("레지스트리(PlayerPrefs) 초기화 완료! 씬을 재로드합니다.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 왼쪽 현재 스킨 이미지 업데이트
    void UpdateCurrentSkinUI()
    {
        if (userData == null || currentSkinImage == null) return;

        Debug.Log($"[MainMenu] 현재 장착 ID: {userData.current_character_id}");

        // 현재 장착된 캐릭터 ID에 맞는 스프라이트 찾기
        CharacterData equippedChar = allCharacters.Find(c => c.character_id == userData.current_character_id);
        if (equippedChar != null && !string.IsNullOrEmpty(equippedChar.image_url))
        {
            string baseName = equippedChar.image_url.Replace(".png", "");
            
            // [NEW] 게임 씬에서 사용할 수 있게 저장 (차량 스킨 동기화용)
            PlayerPrefs.SetString("SelectedSkin", baseName);
            PlayerPrefs.Save(); // 즉시 저장

            string targetName = baseName + "_1"; // 메인메뉴용 스킨 (옆모습 등)
            Sprite sprite = Resources.Load<Sprite>(targetName);
            
            if (sprite != null)
            {
                currentSkinImage.sprite = sprite;
                currentSkinImage.color = Color.white; 
            }
            else
            {
                Debug.LogError($"[MainMenu] 스프라이트를 찾을 수 없습니다: {targetName}");
            }
        }
    }

}
