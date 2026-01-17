using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// [데이터 규격 클래스들]
[System.Serializable]
public class RegisterData { public string nickname; public string password;  }

[System.Serializable]
public class LoginData { public string nickname; public string password; }

[System.Serializable]
public class UpdateData { public string nickname; public int current_character_id; }

public class ServerManager : MonoBehaviour
{
    string baseUrl = "http://localhost:3000";

    void Start()
    {
        // 테스트 시나리오 실행
        StartCoroutine(TestRoutine());
    }

    IEnumerator TestRoutine()
    {
        // 1. 회원가입 시도
        yield return StartCoroutine(Register("racer_02", "1234"));
        
        // 2. 로그인 시도
        yield return StartCoroutine(Login("racer_02", "1234"));

        // 3. 정보 수정
        //yield return new WaitForSeconds(2.0f);
        //yield return StartCoroutine(UpdateUserInfo("racer_01", 2));
    }

    // [API 1] 회원가입
    IEnumerator Register(string nick, string pw)
    {
        string url = baseUrl + "/register";
        RegisterData data = new RegisterData { nickname = nick, password = pw };
        string json = JsonUtility.ToJson(data);

        // PostRequest 함수 재사용 (아래에 정의함)
        yield return StartCoroutine(PostRequest(url, json));
    }

    // [API 2] 로그인
    IEnumerator Login(string nick, string pw)
    {
        string url = baseUrl + "/login";
        LoginData data = new LoginData { nickname = nick, password = pw };
        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest(url, json));
    }

    // [API 3] 정보 수정
    IEnumerator UpdateUserInfo(string nick, int charId)
    {
        string url = baseUrl + "/user/update";
        UpdateData data = new UpdateData { nickname = nick, current_character_id = charId };
        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PutRequest(url, json));
    }

    // [공통 함수] POST 요청 보내기 (코드 중복 줄이기용)
    IEnumerator PostRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[{url}] 요청 성공: " + request.downloadHandler.text);
        }
        else
        {
            // 409(아이디중복), 401(비번틀림) 등의 에러 처리
            Debug.LogError($"[{url}] 요청 실패: " + request.error + " / 내용: " + request.downloadHandler.text);
        }
    }
    // [공통 함수] PUT 요청 보내기 (코드 중복 줄이기용)
    IEnumerator PutRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[{url}] 요청 성공: " + request.downloadHandler.text);
        }
        else
        {
            // 409(아이디중복), 401(비번틀림) 등의 에러 처리
            Debug.LogError($"[{url}] 요청 실패: " + request.error + " / 내용: " + request.downloadHandler.text);
        }
    }
}