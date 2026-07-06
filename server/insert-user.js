const { getConnection } = require('./db');

const user = {
  sub: '123456789012345678901',
  email: 'hong@gmail.com',
  name: '홍길동',
};

async function insertUser() {
  let conn;
  try {
    conn = await getConnection();
    await conn.query(
      `
      INSERT INTO users (sub, email, name)
      VALUES (?, ?, ?)
      ON DUPLICATE KEY UPDATE
        email = VALUES(email),
        name = VALUES(name)
      `,
      [user.sub, user.email, user.name]
    );
    console.log('적재 성공');
  } catch (error) {
    console.error('적재 실패:', error.message);
    process.exitCode = 1;
  } finally {
    if (conn) {
      await conn.end();
    }
  }
}

insertUser();
