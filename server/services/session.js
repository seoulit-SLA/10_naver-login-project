const jwt = require('jsonwebtoken');

const JWT_SECRET = process.env.JWT_SECRET || process.env.JWT_ACCESS_SECRET || 'dev-jwt-secret';
const JWT_EXPIRES_IN = process.env.JWT_EXPIRES_IN || '30d';

const revokedTokens = new Set();

function signSessionToken(uid) {
  return jwt.sign({ sub: uid }, JWT_SECRET, { expiresIn: JWT_EXPIRES_IN });
}

function verifySessionToken(token, options = {}) {
  if (revokedTokens.has(token)) {
    const error = new Error('세션이 무효화되었습니다.');
    error.code = 'SESSION_INVALID';
    throw error;
  }

  return jwt.verify(token, JWT_SECRET, options);
}

function decodeSessionToken(token) {
  return jwt.decode(token);
}

function revokeSessionToken(token) {
  if (token) {
    revokedTokens.add(token);
  }
}

function getAuthErrorCode(error) {
  if (!error) {
    return 'SESSION_INVALID';
  }

  if (error.code === 'SESSION_INVALID') {
    return 'SESSION_INVALID';
  }

  if (error.name === 'TokenExpiredError') {
    return 'SESSION_EXPIRED';
  }

  return 'SESSION_INVALID';
}

module.exports = {
  signSessionToken,
  verifySessionToken,
  decodeSessionToken,
  revokeSessionToken,
  getAuthErrorCode,
};
