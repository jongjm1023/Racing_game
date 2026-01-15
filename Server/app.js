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

// 3. 테스트용 주소 (브라우저에서 확인용)
app.get('/', (req, res) => {
    res.send('서버가 아주 잘 돌아가고 있어요!');
});

// 4. 유니티에서 보낸 로그인 정보를 받는 통로
app.post('/login', (req, res) => {
    const { googleId, nickname } = req.body;
    console.log("로그인 요청 옴:", googleId, nickname);

    // 1. 일단 DB에 이 사람이 있는지 찾아본다 (SELECT)
    const searchSql = 'SELECT * FROM users WHERE google_id = ?';
    connection.query(searchSql, [googleId], (err, results) => {
        if (err) {
            console.error(err);
            return res.status(500).send("DB 검색 에러");
        }

        // 2. 만약 사람이 있다면? (이미 가입된 유저)
        if (results.length > 0) {
            console.log("기존 유저 로그인:", results[0].nickname);
            res.send("로그인 성공 (기존 유저)");
        } 
        // 3. 없다면? (새로운 유저 -> 저장!)
        else {
            const insertSql = 'INSERT INTO users (google_id, nickname) VALUES (?, ?)';
            connection.query(insertSql, [googleId, nickname], (err, result) => {
                if (err) {
                    console.error(err); // 여기서 에러 내용을 자세히 봅니다.
                    return res.status(500).send("회원가입 실패");
                }
                console.log("신규 유저 가입 완료!");
                res.send("회원가입 및 로그인 성공");
            });
        }
    });
});

// 5. 서버 시작 (3000번 포트에서 기다림)
app.listen(3000, () => {
    console.log('서버 실행 중: http://localhost:3000');
});