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

async function getRankings(limit = 100) {
    let conn;
    try {
        conn = await getConnection();
        const safeLimit = Math.min(Math.max(Number(limit) || 100, 1), 100);
        const [rows] = await conn.query(
            `
            SELECT
              h.uid,
              COALESCE(u.name, h.uid) AS name,
              h.best_score AS score,
              h.updated_at AS time
            FROM user_high_scores h
            LEFT JOIN users u ON u.uid = h.uid
            ORDER BY h.best_score DESC, h.updated_at ASC
            LIMIT ?
            `,
            [safeLimit]
        );

        return rows.map((row, index) => ({
            rank: index + 1,
            uid: row.uid,
            name: row.name,
            score: row.score,
            time: row.time ? new Date(row.time).toISOString() : null,
        }));
    } finally {
        if (conn) {
            await conn.end();
        }
    }
}

module.exports = {
    getBestScore,
    submitScore,
    getRankings,
};