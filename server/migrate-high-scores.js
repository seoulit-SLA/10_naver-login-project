// server/migrate-high-scores.js
const { getConnection } = require('./db'); // db.js의 getConnection 함수를 구조분해할당으로 가져옵니다.

async function migrate() {
    let conn;
    try {
        // db.js에 설정된 표준 방식으로 연결 객체를 획득합니다.
        conn = await getConnection();
        console.log('[Migration] 데이터베이스 연결 성공');

        const createTableQuery = `
            CREATE TABLE IF NOT EXISTS user_high_scores (
              id INT NOT NULL AUTO_INCREMENT,
              uid VARCHAR(64) NOT NULL,
              best_score INT NOT NULL DEFAULT 0,
              created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
              updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
              PRIMARY KEY (id),
              UNIQUE KEY uq_user_high_scores_uid (uid),
              KEY idx_user_high_scores_best_score (best_score DESC)
            );
        `;

        await conn.query(createTableQuery);
        console.log('[Migration] user_high_scores 테이블이 성공적으로 생성되었습니다!');
    } catch (err) {
        console.error('[Migration] 테이블 생성 실패:', err.message || err);
    } finally {
        if (conn) {
            await conn.end(); // 쿼리 실행 후 세션을 안전하게 종료합니다.
            console.log('[Migration] 데이터베이스 연결 닫힘');
        }
    }
}

migrate();