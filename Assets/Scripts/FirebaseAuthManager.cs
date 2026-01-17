using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("Server URL")]
    public string serverUrl = "http://localhost:3000";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeFirebase();
    }

    // 1. Firebase 초기화
    async void InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            // [Fix] 데스크톱(에디터)에서는 config 파일 생성 이슈가 잦으므로 수동 초기화 시도
            try 
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                // google-services.json 내용을 수동으로 입력 (AppId, ApiKey, ProjectId)
                var options = new AppOptions {
                    AppId = "1:42918394120:android:f060439cd42915aae114bd",
                    ApiKey = "AIzaSyDMBuOpK1K0Y6bnGn_rhdbRdOSAxN7W5qo",
                    ProjectId = "hamsterrun-6a5e6",
                    MessageSenderId = "42918394120"
                };
                
                FirebaseApp app = null;

                // 이미 생성된 앱이 있는지 확인 (GetApp이 없으므로 Create 시도 후 catch로 처리)
                // 만약 Create가 "이미 존재함" 에러를 낸다면, DefaultInstance는 안전하게 가져올 수 있을 것임.
                try 
                {
                    app = FirebaseApp.Create(options);
                }
                catch
                {
                    // Create 실패 시 (이미 존재 등)
                    // 이미 존재한다면 DefaultInstance가 파일 로드 없이 인스턴스를 반환하길 기대
                    app = FirebaseApp.DefaultInstance;
                }
                
                auth = FirebaseAuth.GetAuth(app);
#else
                // 모바일은 자동 설정 사용
                auth = FirebaseAuth.DefaultInstance;
#endif
                Debug.Log("[FirebaseAuth] 초기화 성공! (Manual/Default)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FirebaseAuth] 수동 초기화 중 에러: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"[FirebaseAuth] 초기화 실패: {dependencyStatus}");
        }
    }

    // 2. 구글 로그인 버튼 연결용
    public void OnClickGoogleLogin()
    {
        SignInWithGoogle();
    }

    // 3. 구글 로그인 로직 (PC 에디터용 약식 구현 포함)
    // 3. 구글 로그인 로직 (PC 에디터용 OAuth 2.0 Loopback + 그 외 플랫폼)
    async void SignInWithGoogle()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // PC에서는 웹 브라우저를 통한 OAuth 2.0 Loopback 방식 사용
        SignInWithGooglePC();
#else
        // 모바일은 추후 GoogleSignIn 플러그인 연동 필요
        Debug.LogWarning("[FirebaseAuth] 모바일 구글 로그인은 아직 구현되지 않았습니다. (GoogleSignIn 플러그인 필요)");
#endif
    }

    // --- PC용 OAuth 2.0 Loopback 구현 ---
    private string googleClientId = "1074911587650-gogh57ecbn2rjvun5m35nlqvsadqkmgn.apps.googleusercontent.com";
    
    async void SignInWithGooglePC()
    {
        Debug.Log("[FirebaseAuth] PC 구글 로그인(OAuth Loopback) 시작...");

        // 1. 로컬 리스너 생성 (http://127.0.0.1:52333/)
        string redirectUri = "http://127.0.0.1:52333/";
        
        System.Net.HttpListener http = new System.Net.HttpListener();
        http.Prefixes.Add(redirectUri);
        try 
        {
            http.Start();
        }
        catch(System.Exception startEx)
        {
            Debug.LogError("HttpListener 시작 실패 (관리자 권한 필요할 수 있음): " + startEx.Message);
            return;
        }

        // 2. 브라우저 열기
        string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={googleClientId}&redirect_uri={redirectUri}&response_type=code&scope=email%20profile%20openid";
        Application.OpenURL(authUrl);

        // 3. 코드 수신 대기
        var context = await http.GetContextAsync();
        
        // 4. 응답 보내기 (브라우저에 "성공" 표시)
        var response = context.Response;
        string responseString = "<html><body><h2>Login Successful!</h2><p>You can close this tab and return to the game.</p></body></html>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
        
        http.Stop(); // 리스너 종료

        // 5. Authorization Code 추출
        string code = context.Request.QueryString.Get("code");
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("[FirebaseAuth] 인증 코드를 받지 못했습니다.");
            return;
        }

        // 6. 코드를 ID Token으로 교환
        await ExchangeCodeForToken(code, redirectUri);
    }

    async Task ExchangeCodeForToken(string code, string redirectUri)
    {
        Debug.Log("[FirebaseAuth] 인증 코드로 토큰 교환 시도...");

        // 비밀번호 파일에서 Secret 가져오기
        string clientSecret = GoogleClientSecret.ClientSecret;
        if (clientSecret.Contains("YOUR_CLIENT_SECRET"))
        {
            Debug.LogError("[FirebaseAuth] GoogleClientSecret.cs에 Client Secret을 입력해주세요!");
            return;
        }

        WWWForm form = new WWWForm();
        form.AddField("code", code);
        form.AddField("client_id", googleClientId);
        form.AddField("client_secret", clientSecret);
        form.AddField("redirect_uri", redirectUri);
        form.AddField("grant_type", "authorization_code");

        UnityWebRequest request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);
        await request.SendWebRequest(); // [FIX] async 메서드에서는 yield return 대신 await 사용

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("[FirebaseAuth] 토큰 교환 성공! Response: " + json);

                // JSON 파싱 (JsonUtility 사용)
                GoogleTokenResponse tokenRes = JsonUtility.FromJson<GoogleTokenResponse>(json);
                string idToken = tokenRes.id_token;
                string accessToken = tokenRes.access_token;

                if (!string.IsNullOrEmpty(idToken))
                {
                    // 7. Firebase 자격 증명 생성 및 로그인
                    // AccessToken은 null이어도 되는 경우가 많음
                    Credential credential = GoogleAuthProvider.GetCredential(idToken, !string.IsNullOrEmpty(accessToken) ? accessToken : null);
                    
                    user = await auth.SignInWithCredentialAsync(credential);
                    
                    Debug.LogFormat("Firebase 구글 로그인 진짜 성공: {0} ({1})", user.DisplayName, user.UserId);

                    // 서버 검증 시작
                    StartCoroutine(VerifyTokenWithServer(user));
                }
                else
                {
                    Debug.LogError("[FirebaseAuth] ID Token을 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogError("[FirebaseAuth] 토큰 교환 실패: " + request.error + " / " + request.downloadHandler.text);
            }
    }

    // 4. 서버 통신 (Node.js로 토큰 전달)
    IEnumerator VerifyTokenWithServer(FirebaseUser firebaseUser)
    {
        // 토큰 가져오기
        var tokenTask = firebaseUser.TokenAsync(true);
        yield return new WaitUntil(() => tokenTask.IsCompleted);

        string idToken = tokenTask.Result;
        string uid = firebaseUser.UserId;
        string email = string.IsNullOrEmpty(firebaseUser.Email) ? $"{uid}@guest.com" : firebaseUser.Email;
        string name = string.IsNullOrEmpty(firebaseUser.DisplayName) ? "Guest_" + uid.Substring(0, 5) : firebaseUser.DisplayName;

        Debug.Log($"[Server] 토큰 전송 시도... UID: {uid}");

        // JSON 데이터 생성
        string json = $"{{\"token\":\"{idToken}\", \"uid\":\"{uid}\", \"email\":\"{email}\", \"name\":\"{name}\"}}";

        UnityWebRequest request = new UnityWebRequest($"{serverUrl}/auth/firebase", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("[Server] 인증 성공! : " + responseJson);
            
            // [FIX] 서버가 만들어준 실제 닉네임 파싱
            // 서버 응답 예시: {"user_id":1, "nickname":"Guest_abc12", ...}
            // 기존에는 로컬 변수 'name'을 그냥 넘겨서, 서버가 변형한 닉네임과 불일치했음 -> 404 원인
            string realNickname = name; // 기본값
            try 
            {
                // 간단한 JSON 파싱을 위해 임시 클래스나 Dictionary 대신 문자열 파싱 혹은 UserData 재사용
                // UserData 클래스는 ShopManager 소속이지만, 여기서 간단히 JsonUtility로 파싱 시도
                // (ServerManager나 ShopManager에 정의된 UserData와 구조가 같으므로 복사본 정의 필요 혹은 단순 파싱)
                
                // 여기서는 간단히 JsonUtility를 쓰기 위해 익명 클래스 대신 임시 구조체를 정의하거나 문자열 검색
                // 가장 확실하게: JsonUtility를 쓰려면 클래스가 필요함.
                // 편의상 문자열 파싱이나 UserData 구조체를 재사용하지 않고, 로컬 struct 정의
                ServerResponseData resData = JsonUtility.FromJson<ServerResponseData>(responseJson);
                if (!string.IsNullOrEmpty(resData.nickname))
                {
                    realNickname = resData.nickname;
                }
            }
            catch (System.Exception parseEx)
            {
                Debug.LogError("닉네임 파싱 실패, 기존 이름 사용: " + parseEx.Message);
            }

            // 여기서 MainMenuController에 '로그인 성공' 알림 (예: 로비 화면으로 전환)
            MainMenuController mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu != null)
            {
                mainMenu.OnLoginSuccess(realNickname);
            }
            else
            {
                Debug.LogWarning("[FirebaseAuth] MainMenuController를 찾을 수 없습니다.");
            } 
        }
        else
        {
            Debug.LogError("[Server] 인증 실패: " + request.error + " / " + request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class ServerResponseData
{
    public string nickname;
    public int user_id;
}

[System.Serializable]
public class GoogleTokenResponse
{
    public string access_token;
    public string expires_in;
    public string scope;
    public string token_type;
    public string id_token;
    //
}
