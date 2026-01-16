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
    const { nickname, password} = req.body;
    console.log(`회원가입 시도: ${nickname} / ${password}`);

    // 중복 아이디 체크
    const insertSql = 'INSERT INTO Users (nickname, password) VALUES (?, ?)';
    
    connection.query(insertSql, [nickname, password], (err, result) => {
        if (err) {
            // 에러 코드가 1062면 중복 아이디
            if (err.errno === 1062) {
                return res.status(409).json({ message: "이미 존재하는 아이디입니다." });
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

    // 아이디와 비밀번호가 둘 다 맞는 유저를 찾음
    const sql = 'SELECT * FROM Users WHERE nickname = ? AND password = ?';
    
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
            res.status(401).json({ message: "아이디 또는 비밀번호가 틀렸습니다." });
        }
    });
});

// 6. 유저 캐릭터 수정
app.put('/user/update', (req, res) => {
    const { nickname, current_character_id} = req.body;
    
    const updateSql = 'UPDATE Users SET current_character_id = ? WHERE nickname = ?';
    
    connection.query(updateSql, [current_character_id, nickname], (err, result) => {
        if (err) return res.status(500).send("업데이트 실패");
        res.json({ message: "정보 저장 완료" });
    });
});

// 서버 실행
app.listen(3000, () => {
    console.log('서버 실행 중: http://localhost:3000');
});