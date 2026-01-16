using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Image itemImage;
    public Button buyButton;

    // 아이템 데이터를 받아 UI를 설정하는 함수
    public void Initialize(string name, int price, Sprite sprite, UnityAction onBuy)
    {
        if (nameText == null) Debug.LogError($"[ShopItemUI] '{name}' 아이템의 NameText가 연결되지 않았습니다! 프리팹을 확인하세요.");
        else nameText.text = name;
        
        if (priceText == null) Debug.LogError($"[ShopItemUI] '{name}' 아이템의 PriceText가 연결되지 않았습니다! 프리팹을 확인하세요.");
        else priceText.text = $"{price:N0} 코인";

        if (itemImage == null) Debug.LogError($"[ShopItemUI] '{name}' 아이템의 ItemImage가 연결되지 않았습니다! 프리팹을 확인하세요.");
        else if (sprite != null) itemImage.sprite = sprite;

        // 버튼 리스너 초기화 (중복 방지)
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(onBuy);
        }
    }
}
