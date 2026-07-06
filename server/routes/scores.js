// server/routes/scores.js
const express = require('express');
const router = express.Router();
const highScoresService = require('../services/highScores');

// [JWT 연동] 기존 인증 시스템(session, users 서비스)에서 검증 도구를 가져옵니다.
const { verifySessionToken, getAuthErrorCode } = require('../services/session');
const { getUserByUid } = require('../services/users');

// [JWT 호환] sessionToken 검증 미들웨어 (auth.js의 /me 검증 로직과 완전히 일치)
async function authenticateToken(req, res, next) {
    // 헤더에서 Bearer 토큰 추출
    const header = req.headers.authorization || '';
    if (!header.startsWith('Bearer ')) {
        return res.status(401).json({ code: 'SESSION_INVALID', message: '인증 토큰이 필요합니다.' });
    }
    const token = header.slice('Bearer '.length).trim();

    try {
        // 암호화된 토큰 복호화 및 검증
        const payload = verifySessionToken(token);
        
        // 토큰의 sub 클레임(uid)을 사용해 유저 정보가 DB에 실재하는지 조회
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
            code: code, 
            message: error.name === 'TokenExpiredError' ? '세션이 만료되었습니다.' : '유효하지 않은 세션입니다.' 
        });
    }
}

// POST /scores/me ➔ 내 최고 점수 요청 처리
router.post('/me', authenticateToken, async (req, res) => {
    try {
        const uid = req.user.uid;
        const bestScore = await highScoresService.getBestScore(uid);
        
        return res.json({
            uid: uid,
            bestScore: bestScore,
            updatedAt: new Date().toISOString()
        });
    } catch (err) {
        console.error('최고 점수 조회 오류:', err);
        return res.status(500).json({ code: 'SERVER_ERROR', message: 'Database error' });
    }
});

// POST /scores/submit ➔ 최고 점수 제출 처리
router.post('/submit', authenticateToken, async (req, res) => {
    const { score } = req.body;

    if (score === undefined || typeof score !== 'number' || score < 0) {
        return res.status(400).json({ code: 'INVALID_SCORE', message: 'Score must be a positive number' });
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
            uid: uid,
            submittedScore: score,
            bestScore: bestScore,
            previousBestScore: previousBestScore,
            isNewRecord: isNewRecord,
            updatedAt: new Date().toISOString()
        });
    } catch (err) {
        console.error('점수 제출 오류:', err);
        return res.status(500).json({ code: 'SERVER_ERROR', message: 'Database error' });
    }
});

module.exports = router;