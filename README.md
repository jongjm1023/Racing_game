# 달려달려 햄찌런


<img width="1186" height="820" alt="Image" src="https://github.com/user-attachments/assets/b10e34fc-9f62-4092-acae-23a4cb5a346a" />

```
햄찌는 오늘도 달린다...!!!
```

---

|팀원|github|역할|
|------|---|---|
|유은선|https://github.com/Erc-nard|기획, 그래픽, 클라이언트|
|이종민|https://github.com/jongjm1023|서버, 클라이언트|



### ✨ 소개
<br>
달리기를 좋아하는 지구별의 햄찌! 햄찌는 오늘도 열심히 달린다...!!


<br>


### 📌 게임 설명

<img width="2719" height="1510" alt="Image" src="https://github.com/user-attachments/assets/7bfd3e13-99f8-414f-b37c-79d97e1d4e01" />


### Scene. 메인

- 구글 연동 로그인 가능, 로그인 이력 저장
- 현재 스킨 표시
- 상점 씬 연결
- 설정: 효과음, 배경음 조절/ 로그아웃
- 온라인 매칭 -> 매칭 성공 시 게임 씬 연결
<br>

### Scene. 상점
- 현재 재화량, 스킨 표시
- 스킨 목록(이름, 외형, 가격) 표시 (DB에서 관리)
- 구매 시 DB의 유저 인벤토리,재화량 업데이트 & 자동 장착
<br>

<img width="1354" height="758" alt="Image" src="https://github.com/user-attachments/assets/461dcc60-dfc0-4056-b732-11d3e9e3ca07" />
<img width="1003" height="694" alt="Image" src="https://github.com/user-attachments/assets/38be805c-1357-4bd9-af17-7be21a478eb9" />

### Scene. 게임
- 클라이언트간 아이템 상호작용
  - 해씨 공격!: 화면에 리듬게임이 뜨고 성공시 대쉬발동, 실패시 스턴상태가 된다.
  - 아름다운 풀밭: 화면 전체를 가리는 풀밭이 일정시간동안 활성화된다.
  - 달려 달려!: 대쉬 발동
  - 방어막!: 방어막 활성화되어 있는 동안 상대의 아이템 공격을 무효화 한다.(1회)
- 타일 별 속도 조절(풀밭에서는 느려진다.)
- 상대 골인 후 10초안에 들어오지 않으면 리타이어, 승리시 +100해씨, 패배 또는 리타이어시 +50해씨 획득
<br>

### 게임 구조(MYSQL)
- **서버:** **Node.js**
    - **역할:** 유니티와 DB를 연결 및 관리
- **게임 네트워크:** **Mirror (유니티 에셋)**
    - **역할:** 레이싱 중 실시간 상호작용
- **DB:** **MySQL**
    - **역할:** 유저 정보, 인벤토리, 상점 정보 저장
    - <img width="834" height="585" alt="스크린샷 2026-01-20 155751" src="https://github.com/user-attachments/assets/b8bbbd41-f956-4997-a5e2-ae71470931a2" />
- **SDK:** **Google Sign-In (구글 로그인)**
    - **역할:** 유니티 시작 화면에서 구글 계정으로 로그인 후, 받은 ID 값을 Node.js 서버로 보냅니다.

### ✅ 개발 스택

- 개발 언어: C#
- 게임 엔진: Unity
- 백엔드: Node.js, MySQL, Mirror
- UI: Figma
- 소스:
https://cupnooble.itch.io/sprout-lands-asset-pack, 카트라이더 BGM

<br>


### 🟡 EXE 파일

[여기](https://github.com/Erc-nard/campusmap/releases/download/v0.1.0-alpha/campusmap-v0.1.0-alpha.apk)를 클릭하여 바로 내려받거나, Releases 탭에서 내역을 확인할 수 있습니다.
