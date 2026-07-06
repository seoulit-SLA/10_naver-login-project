const { getConnection } = require('../db');

async function upsertUser({
  uid,
  email,
  name,
  accessToken = null,
  refreshToken = null,
  tokenExpiresAt = null,
}) {
  const conn = await getConnection();
  try {
    const [result] = await conn.query(
      `
      INSERT INTO users (uid, email, name, naver_access_token, naver_refresh_token, token_expires_at)
      VALUES (?, ?, ?, ?, ?, ?)
      ON DUPLICATE KEY UPDATE
        email = VALUES(email),
        name = VALUES(name),
        naver_access_token = COALESCE(VALUES(naver_access_token), naver_access_token),
        naver_refresh_token = COALESCE(VALUES(naver_refresh_token), naver_refresh_token),
        token_expires_at = COALESCE(VALUES(token_expires_at), token_expires_at)
      `,
      [uid, email, name, accessToken, refreshToken, tokenExpiresAt]
    );

    const user = await getUserByUid(uid, conn);
    return {
      insertId: result.insertId,
      affectedRows: result.affectedRows,
      user,
    };
  } finally {
    await conn.end();
  }
}

async function getUserByUid(uid, existingConn = null) {
  const conn = existingConn || (await getConnection());
  const shouldClose = !existingConn;

  try {
    const [rows] = await conn.query(
      `
      SELECT id, uid, email, name, naver_access_token, naver_refresh_token, token_expires_at, created_at, updated_at
      FROM users
      WHERE uid = ?
      `,
      [uid]
    );

    return rows[0] || null;
  } finally {
    if (shouldClose) {
      await conn.end();
    }
  }
}

async function updateUserTokens(uid, accessToken, refreshToken, tokenExpiresAt) {
  const conn = await getConnection();
  try {
    await conn.query(
      `
      UPDATE users
      SET naver_access_token = ?, naver_refresh_token = ?, token_expires_at = ?
      WHERE uid = ?
      `,
      [accessToken, refreshToken, tokenExpiresAt, uid]
    );

    return getUserByUid(uid, conn);
  } finally {
    await conn.end();
  }
}

module.exports = {
  upsertUser,
  getUserByUid,
  updateUserTokens,
};
