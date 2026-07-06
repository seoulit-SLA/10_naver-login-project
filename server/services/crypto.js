const crypto = require('crypto');

const ALGORITHM = 'aes-256-gcm';
const IV_LENGTH = 12;

function getEncryptionKey() {
  const keyHex = process.env.TOKEN_ENCRYPTION_KEY;
  if (keyHex && keyHex.length === 64) {
    return Buffer.from(keyHex, 'hex');
  }

  const fallback = process.env.JWT_SECRET || process.env.JWT_ACCESS_SECRET || 'dev-token-encryption-key';
  return crypto.createHash('sha256').update(fallback).digest();
}

function encrypt(plainText) {
  if (!plainText) {
    return null;
  }

  const key = getEncryptionKey();
  const iv = crypto.randomBytes(IV_LENGTH);
  const cipher = crypto.createCipheriv(ALGORITHM, key, iv);
  const encrypted = Buffer.concat([cipher.update(String(plainText), 'utf8'), cipher.final()]);
  const authTag = cipher.getAuthTag();

  return Buffer.concat([iv, authTag, encrypted]).toString('base64');
}

function decrypt(encryptedText) {
  if (!encryptedText) {
    return null;
  }

  const key = getEncryptionKey();
  const payload = Buffer.from(encryptedText, 'base64');
  const iv = payload.subarray(0, IV_LENGTH);
  const authTag = payload.subarray(IV_LENGTH, IV_LENGTH + 16);
  const encrypted = payload.subarray(IV_LENGTH + 16);
  const decipher = crypto.createDecipheriv(ALGORITHM, key, iv);
  decipher.setAuthTag(authTag);

  const decrypted = Buffer.concat([decipher.update(encrypted), decipher.final()]);
  return decrypted.toString('utf8');
}

module.exports = { encrypt, decrypt };
