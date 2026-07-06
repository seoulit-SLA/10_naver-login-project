const { getConnection } = require('./db');

async function createUsersTable() {
  let conn;
  try {
    conn = await getConnection();
    await conn.query(`
      CREATE TABLE IF NOT EXISTS users (
        sub VARCHAR(30) PRIMARY KEY,
        email VARCHAR(255) NOT NULL,
        name VARCHAR(100) NOT NULL,
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
      ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
    `);

    const [columns] = await conn.query(
      `
      SELECT COLUMN_NAME
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'users'
        AND COLUMN_NAME = 'created_at'
      `
    );

    if (columns.length === 0) {
      await conn.query(`
        ALTER TABLE users
        ADD COLUMN created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
      `);
    }

    console.log('테이블 생성 성공');
  } catch (error) {
    console.error('테이블 생성 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

createUsersTable();
