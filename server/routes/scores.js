// server/routes/scores.js
const express = require('express');
const router = express.Router();
const highScoresService = require('../services/highScores');

const { verifySessionToken } = require('../services/session');
const { getUserByUid } = require('../services/users');

async function authenticateToken(req, res, next) {
  const header = req.headers.authorization || '';
  if (!header.startsWith('Bearer ')) {
    return res.status(401).json({ code: 'SESSION_INVALID', message: '인증 토큰이 필요합니다.' });
  }

  const token = header.slice('Bearer '.length).trim();

  try {
    const payload = verifySessionToken(token);
    const user = await getUserByUid(payload.sub);

    if (!user) {
      return res.status(404).json({ code: 'USER_NOT_FOUND', message: '사용자를 찾을 수 없습니다.' });
    }

    req.user = { uid: user.uid };
    next();
  } catch (error) {
    console.error('인증 토큰 검증 오류:', error.message);

    const code = error.name === 'TokenExpiredError' ? 'SESSION_EXPIRED' : 'SESSION_INVALID';
    return res.status(401).json({
      code,
      message: error.name === 'TokenExpiredError' ? '세션이 만료되었습니다.' : '유효하지 않은 세션입니다.',
    });
  }
}

router.get('/rankings', async (req, res) => {
  try {
    const limit = Number(req.query.limit || 100);
    const rankings = await highScoresService.getRankings(limit);
    return res.json({ rankings });
  } catch (err) {
    console.error('랭킹 조회 오류:', err);
    return res.status(500).json({ code: 'SERVER_ERROR', message: '랭킹 조회 중 서버 오류가 발생했습니다.' });
  }
});

router.post('/me', authenticateToken, async (req, res) => {
  try {
    const uid = req.user.uid;
    const bestScore = await highScoresService.getBestScore(uid);

    return res.json({
      uid,
      bestScore,
      updatedAt: new Date().toISOString(),
    });
  } catch (err) {
    console.error('최고 점수 조회 오류:', err);
    return res.status(500).json({ code: 'SERVER_ERROR', message: '최고 점수 조회 중 서버 오류가 발생했습니다.' });
  }
});

router.post('/submit', authenticateToken, async (req, res) => {
  const { score } = req.body;

  if (score === undefined || typeof score !== 'number' || score < 0) {
    return res.status(400).json({ code: 'INVALID_SCORE', message: '점수는 0 이상의 숫자여야 합니다.' });
  }

  try {
    const uid = req.user.uid;
    const previousBestScore = await highScoresService.getBestScore(uid);
    let isNewRecord = false;

    if (score > previousBestScore) {
      await highScoresService.submitScore(uid, score);
      isNewRecord = true;
    }

    const bestScore = isNewRecord ? score : previousBestScore;

    return res.json({
      uid,
      submittedScore: score,
      bestScore,
      previousBestScore,
      isNewRecord,
      updatedAt: new Date().toISOString(),
    });
  } catch (err) {
    console.error('점수 제출 오류:', err);
    return res.status(500).json({ code: 'SERVER_ERROR', message: '점수 제출 중 서버 오류가 발생했습니다.' });
  }
});

module.exports = router;
