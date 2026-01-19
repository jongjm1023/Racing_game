using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// [데이터 규격 클래스들]
[System.Serializable]
public class UpdateData { public string nickname; public int current_character_id; }

public class ServerManager : MonoBehaviour
{
    // [FIX] 동적 IP 사용
    string getBaseUrl() { return $"http://{MainMenuController.GetServerIP()}:3000"; }
    // [API 3] 정보 수정
    IEnumerator UpdateUserInfo(string nick, int charId)
    {
        string url = getBaseUrl() + "/user/update";
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