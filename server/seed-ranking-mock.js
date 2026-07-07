const { getConnection } = require('./db');
require('dotenv').config();

const MOCK_USER_COUNT = 100;

async function seedRankingMockData() {
  let conn;
  try {
    conn = await getConnection();
    console.log('[Seed] gamedb 랭킹 목업 데이터 생성 시작');

    await conn.query(`
      CREATE TABLE IF NOT EXISTS user_high_scores (
        id INT NOT NULL AUTO_INCREMENT,
        uid VARCHAR(64) NOT NULL,
        best_score INT NOT NULL DEFAULT 0,
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        PRIMARY KEY (id),
        UNIQUE KEY uq_user_high_scores_uid (uid),
        KEY idx_user_high_scores_best_score (best_score DESC)
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
    `);

    await conn.query('DELETE FROM user_high_scores WHERE uid LIKE ?', ['mock_user_%']);
    await conn.query('DELETE FROM users WHERE uid LIKE ?', ['mock_user_%']);

    for (let i = 1; i <= MOCK_USER_COUNT; i += 1) {
      const uid = `mock_user_${String(i).padStart(3, '0')}`;
      const name = `목업유저${200 + i}`;
      const email = `mock${String(i).padStart(3, '0')}@example.com`;
      const score = 10000 - i * 10;
      const updatedAt = new Date(Date.now() - i * 60 * 1000);

      await conn.query(
        `
        INSERT INTO users (uid, email, name)
        VALUES (?, ?, ?)
        ON DUPLICATE KEY UPDATE
          email = VALUES(email),
          name = VALUES(name)
        `,
        [uid, email, name]
      );

      await conn.query(
        `
        INSERT INTO user_high_scores (uid, best_score, updated_at)
        VALUES (?, ?, ?)
        ON DUPLICATE KEY UPDATE
          best_score = VALUES(best_score),
          updated_at = VALUES(updated_at)
        `,
        [uid, score, updatedAt]
      );
    }

    const [countRows] = await conn.query(
      'SELECT COUNT(*) AS count FROM user_high_scores WHERE uid LIKE ?',
      ['mock_user_%']
    );

    console.log(`[Seed] 목업 유저 ${countRows[0].count}명 / 최고 점수 ${countRows[0].count}건 생성 완료`);
  } catch (error) {
    console.error('[Seed] 랭킹 목업 데이터 생성 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

seedRankingMockData();
