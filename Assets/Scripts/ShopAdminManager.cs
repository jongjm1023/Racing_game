using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;

public class ShopAdminManager : MonoBehaviour
{
    void Start()
    {
        // 게임 시작 시 자동으로 샘플 아이템 추가 (테스트용)
        AddItem("샘플 아이템", 100, 1.5f, "sample.png");
        AddItem("샘플 아이템1", 100, 1.5f, "sample.png");
        AddItem("샘플 아이템2", 100, 1.5f, "sample.png");
        AddItem("샘플 아이템3", 100, 1.5f, "sample.png");
    }

    // 아이템 추가
    public void AddItem(string name, int price, float statSpeed, string imageUrl)
    {
        StartCoroutine(AddItemToServer(name, price, statSpeed, imageUrl));
    }

    // 아이템 삭제
    public void RemoveItem(int characterId)
    {
        StartCoroutine(RemoveItemFromServer(characterId));
    }

    // 서버에 아이템 추가 (POST /characters)
    IEnumerator AddItemToServer(string name, int price, float statSpeed, string imageUrl)
    {
        // 먼저 존재 확인
        UnityWebRequest checkRequest = UnityWebRequest.Get("http://localhost:3000/characters");
        yield return checkRequest.SendWebRequest();
        if (checkRequest.result == UnityWebRequest.Result.Success)
        {
            string json = checkRequest.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            bool exists = charList.characters.Any(c => c.name == name);
            if (exists)
            {
                Debug.Log("관리자: 아이템 이미 존재 - " + name);
                yield break;
            }
        }
        else
        {
            Debug.LogError("관리자: 아이템 확인 실패: " + checkRequest.error);
            yield break;
        }

        // 없으면 추가
        string addJson = $"{{\"name\":\"{name}\", \"price\":{price}, \"stat_speed\":{statSpeed}, \"image_url\":\"{imageUrl}\"}}";
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/characters", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(addJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("관리자: 아이템 추가 성공 - " + name);
        }
        else
        {
            Debug.LogError("관리자: 아이템 추가 실패: " + request.error);
        }
    }

    // 서버에 아이템 삭제 (DELETE /characters/:id)
    IEnumerator RemoveItemFromServer(int characterId)
    {
        UnityWebRequest request = UnityWebRequest.Delete("http://localhost:3000/characters/" + characterId);
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