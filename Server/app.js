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


        res.json({ message: "회원가입 성공! 로그인 해주세요." });
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
            // 로그인 성공 (유저 정보 리턴)
            res.json({
                message: "로그인 성공",
                data: results[0]
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
    console.log('서버 실행 중: http://localhost:3000');
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