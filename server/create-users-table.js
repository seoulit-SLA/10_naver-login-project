const fs = require('fs');
const path = require('path');
const { getConnection } = require('./db');

async function createUsersTable() {
  let conn;
  try {
    conn = await getConnection();
    const migrationSql = fs.readFileSync(
      path.join(__dirname, 'migrations', '001_users.sql'),
      'utf8'
    );
    await conn.query(migrationSql);

    const [columns] = await conn.query(
      `
      SELECT COLUMN_NAME
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'users'
      `
    );
    const columnNames = columns.map((column) => column.COLUMN_NAME);

    if (columnNames.includes('sub') && !columnNames.includes('uid')) {
      await conn.query(`
        ALTER TABLE users
          CHANGE COLUMN sub uid VARCHAR(64) NOT NULL,
          ADD UNIQUE KEY uq_users_uid (uid)
      `);
      console.log('기존 sub 컬럼을 uid로 변경했습니다.');
    }

    console.log('users 테이블 생성/마이그레이션 성공');
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
