const { getConnection } = require('../db');

async function upsertUser({ uid, email, name }) {
  const conn = await getConnection();
  try {
    const [result] = await conn.query(
      `
      INSERT INTO users (uid, email, name)
      VALUES (?, ?, ?)
      ON DUPLICATE KEY UPDATE
        email = VALUES(email),
        name = VALUES(name)
      `,
      [uid, email, name]
    );

    const [rows] = await conn.query(
      'SELECT id, uid, email, name, created_at, updated_at FROM users WHERE uid = ?',
      [uid]
    );

    return {
      insertId: result.insertId,
      affectedRows: result.affectedRows,
      user: rows[0],
    };
  } finally {
    await conn.end();
  }
}

module.exports = { upsertUser };
