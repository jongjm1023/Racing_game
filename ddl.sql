-- 1. racing_game이라는 이름의 데이터베이스 생성
CREATE DATABASE racing_game;
-- 2. 이제부터 이 데이터베이스를 사용하겠다고 선언
USE racing_game;

-- (이 아래에 아까 작성한 CREATE TABLE 코드들을 두시면 됩니다)

-- 1. 캐릭터 정보 (도감)
CREATE TABLE characters (
character_id INT AUTO_INCREMENT PRIMARY KEY,
name VARCHAR(50) NOT NULL,
price INT DEFAULT 0,
stat_speed FLOAT DEFAULT 1.0,
image_url VARCHAR(255)
);
-- 2. 유저 정보
-- current_character_id는 유저가 현재 착용 중인 캐릭터를 참조합니다.
CREATE TABLE users (
user_id INT AUTO_INCREMENT PRIMARY KEY,
password VARCHAR(255) NOT NULL, -- 해싱된 비밀번호
nickname VARCHAR(50) UNIQUE NOT NULL,
seed_money INT DEFAULT 0, -- 해씨(재화)
current_character_id INT,
sound_volume FLOAT DEFAULT 0.5, -- 0.0 ~ 1.0 설정값
last_login DATETIME,
FOREIGN KEY (current_character_id) REFERENCES characters(character_id)
);
-- 3. 유저 인벤토리 (보유 캐릭터 목록)
-- 유저와 캐릭터의 다대다(N:M) 관계를 해소하는 테이블입니다.
CREATE TABLE user_inventory (
inventory_id INT AUTO_INCREMENT PRIMARY KEY,
user_id INT NOT NULL,
character_id INT NOT NULL,
purchased_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
UNIQUE KEY unique_user_char (user_id, character_id), -- 중복 구매 방지
FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
FOREIGN KEY (character_id) REFERENCES characters(character_id)
);
-- 4. 맵 정보
CREATE TABLE maps (
map_id INT AUTO_INCREMENT PRIMARY KEY,
map_name VARCHAR(100) NOT NULL,
distance FLOAT NOT NULL
);
-- 5. 랭킹 (맵별 최단 시간 기록)
CREATE TABLE rankings (
rank_id INT AUTO_INCREMENT PRIMARY KEY,
user_id INT NOT NULL,
map_id INT NOT NULL,
best_time FLOAT NOT NULL, -- 최단 완주 시간 (초 단위)
updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
FOREIGN KEY (map_id) REFERENCES maps(map_id),
INDEX idx_map_time (map_id, best_time) -- 랭킹 조회 속도 최적화
);