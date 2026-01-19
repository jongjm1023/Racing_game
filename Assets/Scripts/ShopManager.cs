using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CharacterData
{
    public int character_id;
    public string name;
    public int price;
    public float stat_speed;
    public string image_url;
}

[System.Serializable]
public class CharacterList
{
    public CharacterData[] characters;
}

[System.Serializable]
public class InventoryData
{
    public int inventory_id;
    public int user_id;
    public int character_id;
    public string purchased_at;
}

[System.Serializable]
public class InventoryList
{
    public InventoryData[] inventory;
}

[System.Serializable]
public class UserData
{
    public int user_id;
    public string nickname;
    public string password;
    public int seed_money;
    public int current_character_id;
    public float sound_volume;
}

public class ShopManager : MonoBehaviour
{
    public TMP_Text currencyText;
    public Image currentSkinImage;
    public Transform shopItemContainer;
    public GameObject shopItemPrefab;

    private string nickname; // PlayerPrefs에서 로드함
    
    [Header("Assets/assets 스프라이트 목록")]
    public List<Sprite> characterSprites = new List<Sprite>();

    private List<CharacterData> allCharacters = new List<CharacterData>();
    private List<InventoryData> userInventory = new List<InventoryData>();
    private UserData userData;

    void Start()
    {
        // 인스펙터 오류로 버튼이 연결 안 될 경우를 대비해 코드로 직접 찾아서 연결
        GameObject btnObj = GameObject.Find("GoToMainButton"); 
        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClickMain);
                Debug.Log("ShopManager: GoToMainButton 연결 성공!");
            }
        }

        // [NEW] 로그인한 닉네임 가져오기
        if (PlayerPrefs.HasKey("Nickname"))
        {
            nickname = PlayerPrefs.GetString("Nickname");
            Debug.Log($"[ShopManager] 로그인된 유저: {nickname}");
        }
        else
        {
            nickname = "racer_02"; // 로그인 안 했으면 기본 테스트 계정 사용
            Debug.LogWarning("[ShopManager] 로그인 정보가 없어 테스트 계정(racer_02)을 사용합니다.");
        }

        StartCoroutine(LoadShopData());
    }
    
    public void OnClickMain()
    {
        SceneManager.LoadScene("Main");
    }
    public IEnumerator LoadShopData()
    {
        // AdminManager가 DB를 초기화할 시간을 벌기 위해 1초 대기
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(GetCharacters());
        yield return StartCoroutine(GetUserData());
        yield return StartCoroutine(GetUserInventory());
        UpdateUI();
    }

    IEnumerator GetCharacters()
    {
        UnityWebRequest request = UnityWebRequest.Get($"http://{MainMenuController.GetServerIP()}:3000/characters");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            allCharacters.Clear(); // 중복 방지를 위해 기존 리스트 초기화
            allCharacters.AddRange(charList.characters);
        }
        else
        {
            Debug.LogError("캐릭터 조회 실패: " + request.error);
        }
    }

    IEnumerator GetUserData()
    {
        UnityWebRequest request = UnityWebRequest.Get($"http://{MainMenuController.GetServerIP()}:3000/users/" + nickname);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            userData = JsonUtility.FromJson<UserData>(json);
        }
        else
        {
            Debug.LogError("유저 조회 실패: " + request.error);
        }
    }

    IEnumerator GetUserInventory()
    {
        UnityWebRequest request = UnityWebRequest.Get($"http://{MainMenuController.GetServerIP()}:3000/user_inventory/" + nickname);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            InventoryList invList = JsonUtility.FromJson<InventoryList>("{\"inventory\":" + json + "}");
            userInventory.Clear(); // 중복 방지를 위해 기존 리스트 초기화
            userInventory.AddRange(invList.inventory);
        }
        else
        {
            Debug.LogError("인벤토리 조회 실패: " + request.error);
        }
    }

    void UpdateUI()
    {
        if (userData != null)
            currencyText.text = "재화: " + userData.seed_money;

        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        Debug.Log($"[ShopManager] 캐릭터 목록 수: {allCharacters.Count}");
        
        if (shopItemPrefab == null)
        {
            Debug.LogError("[ShopManager] Shop Item Prefab이 연결되지 않았습니다! 인스펙터에서 할당해주세요.");
            return;
        }

        foreach (var charData in allCharacters)
        {
            GameObject item = Instantiate(shopItemPrefab, shopItemContainer);
            
            // ShopItemUI 컴포넌트 가져오기
            ShopItemUI itemUI = item.GetComponent<ShopItemUI>();
            if (itemUI != null)
            {
                Debug.Log($"[ShopManager] {charData.name} 아이템 UI 초기화 중...");
                // 이미지 로드 시도
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(charData.image_url))
                {
                    sprite = Resources.Load<Sprite>(charData.image_url.Replace(".png", ""));
                }
                // 보유 여부 확인
                bool isOwned = false;
                if (userInventory != null)
                {
                    isOwned = userInventory.Exists(inv => inv.character_id == charData.character_id);
                }
                
                // 장착 여부 확인
                bool isEquipped = (userData != null && userData.current_character_id == charData.character_id);

                // UI 초기화 (보유/장착 여부 전달)
                itemUI.Initialize(
                    charData.name, 
                    charData.price, 
                    sprite,
                    isOwned,
                    isEquipped,
                    () => 
                    {
                        if (!isOwned) 
                            BuyCharacter(charData.character_id); // 미보유 -> 구매
                        else if (!isEquipped)
                            EquipCharacter(charData.character_id); // 보유중 & 미장착 -> 장착
                        // 이미 장착중이면 아무것도 안 함
                    }
                );
            }
            else
            {
                Debug.LogError($"[ShopManager] 생성된 프리팹({item.name})에 ShopItemUI 컴포넌트를 찾을 수 없습니다. 프리팹의 **최상위**에 스크립트가 붙어있는지 확인해주세요.");
            }
        }
        
        UpdateCurrentSkinUI(); // 현재 장착된 스킨 이미지 갱신

    }

    // 왼쪽 현재 스킨 이미지 업데이트
    void UpdateCurrentSkinUI()
    {
        if (userData == null) 
        {
            Debug.LogWarning("[ShopManager] UpdateCurrentSkinUI: userData가 아직 null입니다.");
            return;
        }
        if (currentSkinImage == null)
        {
            Debug.LogError("[ShopManager] UpdateCurrentSkinUI: CurrentSkinImage(왼쪽 이미지)가 연결되지 않았습니다! 인스펙터에서 할당하세요.");
            return;
        }

        Debug.Log($"[ShopManager] 현재 장착 ID: {userData.current_character_id}");

        // 현재 장착된 캐릭터 ID에 맞는 스프라이트 찾기
        CharacterData equippedChar = allCharacters.Find(c => c.character_id == userData.current_character_id);
        if (equippedChar != null && !string.IsNullOrEmpty(equippedChar.image_url))
        {
            string baseName = equippedChar.image_url.Replace(".png", "");
            string targetName = baseName + "_1"; 
            Sprite sprite = Resources.Load<Sprite>(targetName);
            
            if (sprite != null)
            {
                currentSkinImage.sprite = sprite;
                currentSkinImage.color = Color.white; 
                Debug.Log($"[ShopManager] 왼쪽 이미지 갱신 완료: {targetName}");
            }
            else
            {
                Debug.LogError($"[ShopManager] 스프라이트를 찾을 수 없습니다: {targetName} (characterSprites 개수: {characterSprites.Count})");
            }
        }
        else
        {
             Debug.LogWarning($"[ShopManager] 장착된 캐릭터({userData.current_character_id})를 찾을 수 없거나 이미지가 없습니다.");
        }
    }

    void EquipCharacter(int charId)
    {
        StartCoroutine(RequestEquip(charId));
    }

    IEnumerator RequestEquip(int charId)
    {
        // 1. 서버에 장착 요청 (PUT /user/update)
        string json = $"{{\"nickname\":\"{nickname}\", \"current_character_id\":{charId}}}";
        UnityWebRequest request = new UnityWebRequest($"http://{MainMenuController.GetServerIP()}:3000/user/update", "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[ShopManager] 캐릭터 {charId}번 장착 성공");
            // 2. 로컬 데이터 갱신 후 UI 업데이트
            if (userData != null) userData.current_character_id = charId;
            UpdateUI();
        }
        else
        {
            Debug.LogError("장착 실패: " + request.error);
        }
    }

    void BuyCharacter(int charId)
    {
        Debug.Log($"[ShopManager] BuyCharacter 호출됨 - ID: {charId}");
        StartCoroutine(Purchase(charId));
    }

    IEnumerator Purchase(int charId)
    {
        string json = "{\"nickname\":\"" + nickname + "\", \"character_id\":" + charId + "}";
        Debug.Log($"[ShopManager] 구매 요청 데이터: {json}"); 

        UnityWebRequest request = new UnityWebRequest($"http://{MainMenuController.GetServerIP()}:3000/purchase", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("구매 성공! 자동으로 장착합니다.");
            
            // 구매 후 목록 갱신 -> 갱신 끝나면 자동 장착
            // 하지만 비동기라 복잡할 수 있으니 간단히 로컬에서 먼저 장착 요청 보내고 로드
            yield return StartCoroutine(RequestEquip(charId)); 
            
            StartCoroutine(LoadShopData()); // 전체 리프레시
        }
        else
        {
            Debug.LogError("구매 실패: " + request.error);
            Debug.LogError("서버 응답: " + request.downloadHandler.text); 
        }
    }
}