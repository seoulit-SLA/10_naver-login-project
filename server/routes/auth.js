const express = require('express');
const passport = require('passport');
const { encrypt, decrypt } = require('../services/crypto');
const { refreshNaverToken } = require('../services/naverAuth');
const {
  signSessionToken,
  verifySessionToken,
  revokeSessionToken,
  getAuthErrorCode,
} = require('../services/session');
const { upsertUser, getUserByUid, updateUserTokens } = require('../services/users');

const router = express.Router();

const NAVER_CLIENT_ID = process.env.NAVER_CLIENT_ID;
const NAVER_CLIENT_SECRET = process.env.NAVER_CLIENT_SECRET;
const CLIENT_CALLBACK_URL = process.env.CLIENT_CALLBACK_URL || 'http://127.0.0.1:7777/naver-login/';

function getBearerToken(req) {
  const header = req.headers.authorization || '';
  if (!header.startsWith('Bearer ')) {
    return null;
  }
  return header.slice('Bearer '.length).trim();
}

function buildClientCallback(query) {
  const separator = CLIENT_CALLBACK_URL.includes('?') ? '&' : '?';
  return `${CLIENT_CALLBACK_URL}${separator}${query}`;
}

function sendAuthError(res, statusCode, message, code) {
  if (typeof res.apiError === 'function') {
    return res.apiError(statusCode, code, message);
  }

  return res.status(statusCode).json({ message, code });
}

router.get('/naver', (req, res, next) => {
  if (!NAVER_CLIENT_ID || !NAVER_CLIENT_SECRET) {
    return sendAuthError(res, 500, 'NAVER 환경변수가 설정되지 않았습니다.', 'AUTH_CONFIG_MISSING');
  }

  passport.authenticate('naver')(req, res, next);
});

router.get('/naver/callback', (req, res, next) => {
  if (!NAVER_CLIENT_ID || !NAVER_CLIENT_SECRET) {
    return sendAuthError(res, 500, 'NAVER 환경변수가 설정되지 않았습니다.', 'AUTH_CONFIG_MISSING');
  }

  passport.authenticate('naver', { session: false }, async (error, user) => {
    if (error || !user) {
      console.error('[NAVER] callback auth failed:', error ? error.message : 'no user');
      return res.redirect(buildClientCallback('error=auth_failed'));
    }

    const profile = {
      uid: String(user.id),
      email: user.email || '',
      name: user.name || 'naver-user',
    };

    console.log('[NAVER] user info');
    console.log(`uid : ${profile.uid}`);
    console.log(`email : ${profile.email}`);
    console.log(`name : ${profile.name}`);

    try {
      const tokenExpiresAt = new Date(Date.now() + 60 * 60 * 1000);
      const dbResult = await upsertUser({
        ...profile,
        accessToken: encrypt(user.accessToken),
        refreshToken: encrypt(user.refreshToken),
        tokenExpiresAt,
      });

      const sessionToken = signSessionToken(profile.uid);
      console.log('[NAVER] DB insert response', dbResult);

      return res.redirect(buildClientCallback(`token=${encodeURIComponent(sessionToken)}`));
    } catch (dbError) {
      console.error('[NAVER] DB insert failed:', dbError.message);
      return res.redirect(buildClientCallback('error=db_failed'));
    }
  })(req, res, next);
});

router.post('/me', async (req, res) => {
  const token = getBearerToken(req);
  if (!token) {
    return sendAuthError(res, 401, '인증 토큰이 필요합니다.', 'SESSION_INVALID');
  }

  try {
    const payload = verifySessionToken(token);
    const user = await getUserByUid(payload.sub);

    if (!user) {
      return sendAuthError(res, 404, '사용자를 찾을 수 없습니다.', 'USER_NOT_FOUND');
    }

    return res.status(200).json({
      uid: user.uid,
      email: user.email,
      name: user.name,
    });
  } catch (error) {
    return sendAuthError(
      res,
      401,
      error.name === 'TokenExpiredError' ? '세션이 만료되었습니다.' : '유효하지 않은 세션입니다.',
      getAuthErrorCode(error)
    );
  }
});

router.post('/refresh', async (req, res) => {
  const token = getBearerToken(req);
  if (!token) {
    return sendAuthError(res, 401, '인증 토큰이 필요합니다.', 'SESSION_INVALID');
  }

  let payload;
  try {
    payload = verifySessionToken(token, { ignoreExpiration: true });
  } catch (error) {
    return sendAuthError(res, 401, '유효하지 않은 세션입니다.', getAuthErrorCode(error));
  }

  const user = await getUserByUid(payload.sub);
  if (!user) {
    return sendAuthError(res, 404, '사용자를 찾을 수 없습니다.', 'USER_NOT_FOUND');
  }

  if (!user.naver_refresh_token) {
    return sendAuthError(res, 401, '재인증이 필요합니다.', 'REAUTH_REQUIRED');
  }

  try {
    const refreshToken = decrypt(user.naver_refresh_token);
    const refreshed = await refreshNaverToken(refreshToken);
    const tokenExpiresAt = new Date(Date.now() + refreshed.expiresIn * 1000);

    await updateUserTokens(
      user.uid,
      encrypt(refreshed.accessToken),
      encrypt(refreshed.refreshToken),
      tokenExpiresAt
    );

    revokeSessionToken(token);
    const sessionToken = signSessionToken(user.uid);

    return res.status(200).json({
      sessionToken,
      uid: user.uid,
      email: user.email,
      name: user.name,
    });
  } catch (error) {
    const code = error.code === 'NAVER_TOKEN_REVOKED' ? 'NAVER_TOKEN_REVOKED' : 'REAUTH_REQUIRED';
    return sendAuthError(
      res,
      401,
      code === 'NAVER_TOKEN_REVOKED' ? 'Naver 토큰이 폐기되었습니다.' : '재인증이 필요합니다.',
      code
    );
  }
});

router.post('/logout', (req, res) => {
  const token = getBearerToken(req);
  if (token) {
    revokeSessionToken(token);
  }

  return res.status(200).json({ message: '로그아웃되었습니다.' });
});

module.exports = router;
