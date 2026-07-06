const { getConnection, config } = require('./db');

async function testDbConnection() {
  let conn;
  try {
    conn = await getConnection();
    await conn.query('SELECT 1');
    console.log('접속 성공했다');
  } catch (error) {
    console.error('접속 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

testDbConnection();
