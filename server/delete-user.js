const { getConnection } = require('./db');

const uid = process.argv[2];

if (!uid) {
  console.error('uid를 입력하세요');
  process.exit(1);
}

async function deleteUser() {
  let conn;
  try {
    conn = await getConnection();
    const [result] = await conn.query('DELETE FROM users WHERE uid = ?', [uid]);

    if (result.affectedRows > 0) {
      console.log('삭제 성공');
    } else {
      console.log('삭제할 데이터 없음');
    }
  } catch (error) {
    console.error('삭제 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

deleteUser();
