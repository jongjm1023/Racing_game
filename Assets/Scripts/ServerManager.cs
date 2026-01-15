using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// JSON 데이터를 담을 봉투 (Node.js랑 이름이 똑같아야 함!)
[System.Serializable]
public class LoginData
{
    public string googleId;
    public string nickname;
}

public class ServerManager : MonoBehaviour
{
    // Node.js 서버 주소 (로컬)
    string baseUrl = "http://localhost:3000"; 

    void Start()
    {
        // 게임 시작하자마자 테스트로 로그인 시도
        StartCoroutine(Login("test_user_1", "SpeedRacer"));
    }

    // 로그인 요청 함수
    IEnumerator Login(string id, string nick)
    {
        // 1. 보낼 데이터 포장하기
        LoginData data = new LoginData { googleId = id, nickname = nick };
        string json = JsonUtility.ToJson(data); // JSON 문자열로 변환

        // 2. 우체부(Request) 부르기
        UnityWebRequest request = new UnityWebRequest(baseUrl + "/login", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json"); // "나 JSON 보낸다!"

        // 3. 전송하고 기다리기
        yield return request.SendWebRequest();

        // 4. 결과 확인
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("서버 응답: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("에러 발생: " + request.error);
        }
    }
}