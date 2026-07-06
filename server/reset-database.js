const fs = require('fs');
const path = require('path');
const { getConnection, config } = require('./db');

async function resetDatabase() {
  let conn;
  try {
    conn = await getConnection();
    console.log(`데이터베이스 '${config.database}' 초기화 시작`);

    await conn.query('SET FOREIGN_KEY_CHECKS = 0');
    const [tables] = await conn.query('SHOW TABLES');
    const tableKey = `Tables_in_${config.database}`;

    for (const table of tables) {
      const tableName = table[tableKey];
      await conn.query(`DROP TABLE IF EXISTS \`${tableName}\``);
      console.log(`- dropped table: ${tableName}`);
    }

    await conn.query('SET FOREIGN_KEY_CHECKS = 1');

    const migrationSql = fs.readFileSync(
      path.join(__dirname, 'migrations', '001_users.sql'),
      'utf8'
    );
    await conn.query(migrationSql);
    console.log('users 테이블 생성 완료');

    const [columns] = await conn.query(
      `
      SELECT COLUMN_NAME
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'users'
      ORDER BY ORDINAL_POSITION
      `
    );
    console.log('users columns:', columns.map((column) => column.COLUMN_NAME).join(', '));
    console.log(`데이터베이스 '${config.database}' 초기화 완료`);
  } catch (error) {
    console.error('데이터베이스 초기화 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

resetDatabase();
