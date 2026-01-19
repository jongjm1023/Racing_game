using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;

public class ShopAdminManager : MonoBehaviour
{
    // 게임 시작 시 자동으로 실행
    IEnumerator Start()
    {
        Debug.Log("관리자: 아이템 점검 및 추가 시작 (기존 데이터 유지)...");

        // 1. 기존 아이템 조회
        UnityWebRequest request = UnityWebRequest.Get($"http://{MainMenuController.GetServerIP()}:3000/characters");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            
            // 2. 없는 아이템만 골라서 추가하기
            yield return StartCoroutine(CheckAndAdd("샘플 아이템1", 50, 1.5f, "player1.png", charList));
            yield return StartCoroutine(CheckAndAdd("샘플 아이템2", 100, 1.5f, "player2.png", charList));
            yield return StartCoroutine(CheckAndAdd("샘플 아이템3", 100, 1.5f, "player3.png", charList));
            yield return StartCoroutine(CheckAndAdd("샘플 아이템4", 200, 1.5f, "player4.png", charList));
            yield return StartCoroutine(CheckAndAdd("샘플 아이템5", 400, 1.5f, "player5.png", charList));
        }
        else
        {
            Debug.LogError("관리자: 조회 실패 - " + request.error);
        }

        Debug.Log("관리자: 아이템 점검 완료");
    }

    // 이미 있는지 확인하고 없으면 추가하는 함수
    IEnumerator CheckAndAdd(string name, int price, float speed, string img, CharacterList currentList)
    {
        bool exists = false;
        if (currentList.characters != null)
        {
            // LINQ를 사용하여 이름이 같은게 있는지 확인
            exists = currentList.characters.Any(c => c.name == name);
        }

        if (!exists)
        {
            Debug.Log($"관리자: '{name}' 아이템이 없어서 추가합니다.");
            yield return StartCoroutine(AddItemToServer(name, price, speed, img));
        }
    }

    // 실제로 서버에 추가 요청을 보내는 함수
    IEnumerator AddItemToServer(string name, int price, float speed, string imageName)
    {
        string json = $"{{\"name\":\"{name}\", \"price\":{price}, \"stat_speed\":{speed}, \"image_url\":\"{imageName}\"}}";
        UnityWebRequest request = new UnityWebRequest($"http://{MainMenuController.GetServerIP()}:3000/characters", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("아이템 추가 실패: " + request.error);
        }
    }

    // 아이템 삭제 (필요할 때만 호출)
    public IEnumerator RemoveItemFromServer(int characterId)
    {
        UnityWebRequest request = UnityWebRequest.Delete($"http://{MainMenuController.GetServerIP()}:3000/characters/" + characterId);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("관리자: 아이템 삭제 성공 - ID: " + characterId);
        }
        else
        {
            Debug.LogError("관리자: 아이템 삭제 실패: " + request.error);
        }
    }
}