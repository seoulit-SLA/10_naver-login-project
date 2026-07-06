// server/services/highScores.js
const { getConnection } = require('../db'); // 상위 폴더의 db.js에서 getConnection을 가져옵니다.

// 내 최고 점수 조회
async function getBestScore(uid) {
    let conn;
    try {
        conn = await getConnection();
        const query = 'SELECT best_score FROM user_high_scores WHERE uid = ?';
        const [rows] = await conn.query(query, [uid]);
        if (rows.length > 0) {
            return rows[0].best_score;
        }
        return 0;
    } finally {
        if (conn) {
            await conn.end(); // 연결 반환
        }
    }
}

// 최고 점수 업데이트
async function submitScore(uid, score) {
    let conn;
    try {
        conn = await getConnection();
        const query = `
            INSERT INTO user_high_scores (uid, best_score) 
            VALUES (?, ?) 
            ON DUPLICATE KEY UPDATE 
            best_score = VALUES(best_score),
            updated_at = CURRENT_TIMESTAMP
        `;
        await conn.query(query, [uid, score]);
    } finally {
        if (conn) {
            await conn.end(); // 연결 반환
        }
    }
}

module.exports = {
    getBestScore,
    submitScore
};