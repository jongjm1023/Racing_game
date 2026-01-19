require('dotenv').config({ path: '../.env' });

const express = require('express'); // 서버 도구 불러오기
const mysql = require('mysql2');    // DB 도구 불러오기
const app = express();

app.use(express.json()); // 유니티가 보낸 JSON 데이터를 읽을 수 있게 함

// 1. MySQL 연결 설정 
const connection = mysql.createConnection({
    host: process.env.DB_HOST,
    user: process.env.DB_USER,
    password: process.env.DB_PASS,
    database: process.env.DB_NAME
});

// 2. 서버가 켜졌을 때 DB에 연결 시도
connection.connect((err) => {
    if (err) {
        console.error('DB 연결 실패: ' + err.message);
        return;
    }
    console.log('DB에 성공적으로 연결되었습니다!');
});

// 4. 회원가입 처리
app.post('/register', (req, res) => {
    const { nickname, password } = req.body;
    console.log(`회원가입 시도: ${nickname}`);

    // 중복 닉네임 체크
    const insertSql = 'INSERT INTO users (nickname, password) VALUES (?, ?)';

    connection.query(insertSql, [nickname, password], (err, result) => {
        if (err) {
            // 에러 코드가 1062면 중복 닉네임
            if (err.errno === 1062) {
                return res.status(409).json({ message: "이미 존재하는 닉네임입니다." });
            }
            return res.status(500).send("서버 에러");
        }

        if (result) {
            const userId = result.insertId; // 생성된 유저의 ID

            // 4-1. 기본 캐릭터(가장 ID 낮은 것) 찾기
            connection.query('SELECT character_id FROM characters ORDER BY character_id ASC LIMIT 1', (err, charResults) => {
                if (err || charResults.length === 0) {
                    // 캐릭터가 없으면 그냥 가입 성공 처리 (나중에 상점에서 사면 됨)
                    return res.json({ message: "회원가입 성공! (기본 캐릭터 없음)" });
                }

                const defaultCharId = charResults[0].character_id;

                // 4-2. 인벤토리에 기본 캐릭터 지급
                connection.query('INSERT INTO user_inventory (user_id, character_id) VALUES (?, ?)', [userId, defaultCharId], (err) => {
                    if (err) {
                        console.error('기본 캐릭터 지급 실패:', err);
                        return res.json({ message: "회원가입 성공! (캐릭터 지급 오류)" });
                    }

                    // 4-3. 기본 캐릭터 장착
                    connection.query('UPDATE users SET current_character_id = ? WHERE user_id = ?', [defaultCharId, userId], (err) => {
                        if (err) console.error('기본 캐릭터 장착 실패:', err);

                        res.json({ message: "회원가입 성공! 기본 캐릭터가 지급되었습니다." });
                    });
                });
            });
        }
    });
});

// 5. 로그인 처리
app.post('/login', (req, res) => {
    const { nickname, password } = req.body;
    console.log(`로그인 시도: ${nickname}`);

    // 닉네임과 비밀번호가 둘 다 맞는 유저를 찾음
    const sql = 'SELECT * FROM users WHERE nickname = ? AND password = ?';

    connection.query(sql, [nickname, password], (err, results) => {
        if (err) return res.status(500).send("DB 에러");

        if (results.length > 0) {
            const user = results[0];

            // 추가: 인벤토리 확인 (계정은 있는데 캐릭터가 없는 경우 방지)
            connection.query('SELECT * FROM user_inventory WHERE user_id = ?', [user.user_id], (err, invResults) => {
                if (!err && invResults.length === 0) {
                    console.log(`[Login] ${user.nickname} 유저의 캐릭터가 없어 기본 캐릭터를 지급합니다.`);

                    // 1. 기본 캐릭터 찾기
                    connection.query('SELECT character_id FROM characters ORDER BY character_id ASC LIMIT 1', (err, charResults) => {
                        if (err || charResults.length === 0) {
                            return res.json({ message: "로그인 성공", data: user });
                        }

                        const defaultCharId = charResults[0].character_id;

                        // 2. 지급
                        connection.query('INSERT INTO user_inventory (user_id, character_id) VALUES (?, ?)', [user.user_id, defaultCharId], () => {
                            // 3. 장착 업데이트
                            connection.query('UPDATE users SET current_character_id = ? WHERE user_id = ?', [defaultCharId, user.user_id], () => {
                                // 유저 정보 갱신해서 응답
                                user.current_character_id = defaultCharId;
                                res.json({ message: "로그인 성공 (기본 캐릭터 지급됨)", data: user });
                            });
                        });
                    });
                } else {
                    // 정상적인 경우 (이미 캐릭터 있음)
                    res.json({
                        message: "로그인 성공",
                        data: user
                    });
                }
            });
        } else {
            // 로그인 실패
            res.status(401).json({ message: "닉네임 또는 비밀번호가 틀렸습니다." });
        }
    });
});

// 6. 유저 캐릭터 수정
app.put('/user/update', (req, res) => {
    const { nickname, current_character_id } = req.body;

    const updateSql = 'UPDATE users SET current_character_id = ? WHERE nickname = ?';

    connection.query(updateSql, [current_character_id, nickname], (err, result) => {
        if (err) return res.status(500).send("업데이트 실패");
        res.json({ message: "정보 저장 완료" });
    });
});

// 서버 실행
app.listen(3000, () => {
    console.log('서버 실행 중');
});

// 캐릭터 상점 API

// GET /characters - 모든 캐릭터 조회
app.get('/characters', (req, res) => {
    connection.query('SELECT * FROM characters', (err, results) => {
        if (err) {
            console.error(err);
            return res.status(500).send('DB 조회 에러');
        }
        res.json(results);
    });
});

// POST /characters - 캐릭터 추가 (관리자용)
app.post('/characters', (req, res) => {
    const { name, price, stat_speed, image_url } = req.body;
    connection.query('INSERT INTO characters (name, price, stat_speed, image_url) VALUES (?, ?, ?, ?)',
        [name, price, stat_speed, image_url], (err, result) => {
            if (err) {
                console.error(err);
                return res.status(500).send('캐릭터 추가 실패');
            }
            res.status(201).json({ character_id: result.insertId, message: '캐릭터 추가 성공' });
        });
});

// DELETE /characters/:id - 캐릭터 삭제 (관리자용)
app.delete('/characters/:id', (req, res) => {
    const { id } = req.params;
    connection.query('DELETE FROM characters WHERE character_id = ?', [id], (err, result) => {
        if (err) {
            console.error(err);
            return res.status(500).send('캐릭터 삭제 실패');
        }
        if (result.affectedRows === 0) {
            return res.status(404).send('캐릭터를 찾을 수 없습니다');
        }
        res.send('캐릭터 삭제 성공');
    });
});

// GET /users/:nickname - 유저 조회
app.get('/users/:nickname', (req, res) => {
    const { nickname } = req.params;
    connection.query('SELECT * FROM users WHERE nickname = ?', [nickname], (err, results) => {
        if (err) {
            console.error(err);
            return res.status(500).send('DB 조회 에러');
        }
        if (results.length === 0) {
            return res.status(404).send('유저를 찾을 수 없습니다');
        }
        res.json(results[0]);
    });
});

// GET /user_inventory/:nickname - 유저 인벤토리 조회
app.get('/user_inventory/:nickname', (req, res) => {
    const { nickname } = req.params;
    connection.query('SELECT * FROM user_inventory WHERE user_id = (SELECT user_id FROM users WHERE nickname = ?)', [nickname], (err, results) => {
        if (err) {
            console.error(err);
            return res.status(500).send('DB 조회 에러');
        }
        res.json(results);
    });
});

// POST /purchase - 캐릭터 구매
app.post('/purchase', (req, res) => {
    const { nickname, character_id } = req.body;

    // 1. 캐릭터 가격 조회
    connection.query('SELECT price FROM characters WHERE character_id = ?', [character_id], (err, charResults) => {
        if (err || charResults.length === 0) return res.status(500).send('캐릭터 조회 실패');

        const price = charResults[0].price;

        // 2. 유저 재화 조회
        connection.query('SELECT seed_money, user_id FROM users WHERE nickname = ?', [nickname], (err, userResults) => {
            if (err || userResults.length === 0) return res.status(500).send('유저 조회 실패');

            const currentMoney = userResults[0].seed_money;
            const userId = userResults[0].user_id;

            if (currentMoney < price) return res.status(400).send('재화 부족');

            // 3. 재화 차감
            connection.query('UPDATE users SET seed_money = seed_money - ? WHERE nickname = ?', [price, nickname], (err) => {
                if (err) return res.status(500).send('재화 차감 실패');

                // 4. 인벤토리 추가
                connection.query('INSERT INTO user_inventory (user_id, character_id) VALUES (?, ?)', [userId, character_id], (err) => {
                    if (err) return res.status(500).send('인벤토리 추가 실패');

                    res.json({ message: '구매 성공' });
                });
            });
        });
    });
});

// 8. 간단 매칭 시스템 (서버 메모리에 저장)
let waitingHost = false;

app.get('/match', (req, res) => {
    if (waitingHost) {
        // 이미 누군가 호스트로 대기 중 -> 클라이언트로 참여
        waitingHost = false; // 매칭 성사 -> 대기 해제
        res.json({ role: 'client', address: 'localhost' });
        console.log("매칭 성사! 클라이언트로 입장시킵니다.");
    } else {
        // 대기 중인 사람이 없음 -> 호스트가 됨
        waitingHost = true;
        res.json({ role: 'host' });
        console.log("매칭 대기열 생성. 호스트로 대기합니다.");
    }
});

// [개발용] 매칭 상태 리셋 (F2키로 호출)
app.get('/reset_match', (req, res) => {
    waitingHost = false;
    res.send('매칭 대기열 초기화 완료');
    console.log("매칭 상태가 초기화되었습니다.");
});

// 10. Firebase 로그인(통합)
app.get('/cancel_match', (req, res) => {
    // ... 기존 cancel_match 로직 유지 ...
    if (waitingHost) {
        waitingHost = false;
        res.send('매칭이 취소되었습니다.');
        console.log("호스트가 매칭을 취소했습니다. 대기열 해제.");
    } else {
        res.send('대기 중인 매칭이 없습니다.');
    }
});

// [NEW] Firebase 인증 처리
app.post('/auth/firebase', (req, res) => {
    const { token, uid, email, name } = req.body;

    // 원래는 여기서 'firebase-admin' 라이브러리로 'token'을 검증해야 안전합니다.
    // 지금은 개발 단계이므로 클라이언트가 보낸 uid를 신뢰하고 진행합니다.
    console.log(`[Firebase Login] UID: ${uid}, Email: ${email}, Name: ${name}`);

    // DB에서 이 UID를 가진 유저가 있는지 확인
    // (기존 users 테이블은 id/password 기반이라, firebase_uid 칼럼이 없으면 닉네임이나 이메일로 대체해야 함)
    // 여기서는 간단히 '닉네임 = Firebase Name'으로 매핑해서 처리하거나
    // 'firebase_' 접두어를 붙여 고유하게 만듭니다.

    const dbNickname = name.replace(/\s/g, "") + "_" + uid.substring(0, 5); // 공백제거 + UID 일부

    const querySelect = "SELECT * FROM users WHERE nickname = ?";
    connection.query(querySelect, [dbNickname], (err, results) => {
        if (err) {
            console.error(err);
            res.status(500).send("DB Error");
            return;
        }

        if (results.length > 0) {
            // 이미 존재함 -> 로그인 성공
            res.json(results[0]);
            console.log("Firebase 재방문 유저 로그인: " + dbNickname);
        } else {
            // 없음 -> 회원가입 (초기 데이터 생성)

            // 1. 기본 캐릭터(가장 ID 낮은 것) 찾기
            connection.query('SELECT character_id FROM characters ORDER BY character_id ASC LIMIT 1', (errDef, charResults) => {
                if (errDef) {
                    console.error(errDef);
                    return res.status(500).send("DB Error finding default char");
                }

                // 만약 캐릭터가 하나도 없다면? (에러 처리 혹은 더미 값)
                // FK 제약조건 때문에 캐릭터가 없으면 유저 생성이 불가능할 수 있음.
                let defaultCharId = (charResults.length > 0) ? charResults[0].character_id : null;

                if (!defaultCharId) {
                    return res.status(500).send("DB에 캐릭터가 하나도 없습니다. 관리자에게 문의하세요.");
                }

                const queryInsert = "INSERT INTO users (nickname, password, seed_money, current_character_id) VALUES (?, ?, ?, ?)";
                // 비밀번호는 Firebase 유저라 없지만 DB 제약조건 때문에 임의값 넣음
                // [FIX] 100 대신 defaultCharId 변수 사용
                connection.query(queryInsert, [dbNickname, "firebase_user", 3000, defaultCharId], (err2, result2) => {
                    if (err2) {
                        console.error(err2);
                        res.status(400).send("Register Error");
                        return;
                    }

                    const newUserId = result2.insertId;
                    const invValues = [[newUserId, defaultCharId]];

                    connection.query("INSERT INTO user_inventory (user_id, character_id) VALUES ?", [invValues], (err3) => {
                        if (err3) console.error("기본 지급 실패", err3);
                    });

                    // 방금 만든 유저 정보 리턴
                    connection.query("SELECT * FROM users WHERE user_id = ?", [newUserId], (err4, finalResults) => {
                        if (err4) {
                            console.error("가입 후 조회 실패: " + err4);
                            return res.status(500).send("DB Error");
                        }
                        res.json(finalResults[0]);
                        console.log("Firebase 신규 유저 가입 완료: " + dbNickname);
                    });
                });
            });
        }
    });
});