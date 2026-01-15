using UnityEngine;
using Mirror; 

public class CarController : NetworkBehaviour
{
    public float speed = 10f;

    void Update()
    {
        // 내 차(LocalPlayer)가 아니면 조종 못하게 막기
        if (!isOwned) return;

        float h = Input.GetAxis("Horizontal"); // 좌우 키
        float v = Input.GetAxis("Vertical");   // 상하 키

        Vector3 move = new Vector3(h, v, 0) * speed * Time.deltaTime;
        transform.position += move;
    }
}