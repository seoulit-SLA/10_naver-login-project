const express = require('express');
const path = require('path');
require('dotenv').config();
const session = require('express-session');
const passport = require('passport');
const NaverStrategy = require('passport-naver-v2').Strategy;

const app = express();
const port = 3000;

const NAVER_CLIENT_ID = process.env.NAVER_CLIENT_ID;
const NAVER_CLIENT_SECRET = process.env.NAVER_CLIENT_SECRET;
const NAVER_CALLBACK_URL = process.env.NAVER_CALLBACK_URL || 'http://localhost:3000/auth/naver/callback';
const SESSION_SECRET = process.env.SESSION_SECRET || 'dev-session-secret';

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(
  session({
    secret: SESSION_SECRET,
    resave: false,
    saveUninitialized: false,
  })
);
app.use(passport.initialize());
app.use(passport.session());

passport.serializeUser((user, done) => {
  done(null, user);
});

passport.deserializeUser((user, done) => {
  done(null, user);
});

if (NAVER_CLIENT_ID && NAVER_CLIENT_SECRET) {
  passport.use(
    new NaverStrategy(
      {
        clientID: NAVER_CLIENT_ID,
        clientSecret: NAVER_CLIENT_SECRET,
        callbackURL: NAVER_CALLBACK_URL,
      },
      (accessToken, refreshToken, profile, done) => {
        return done(null, {
          id: profile.id,
          email: profile.email || '',
          name: profile.name || profile.nickname || 'naver-user',
          accessToken,
          refreshToken,
          profile,
        });
      }
    )
  );
}

app.use((req, res, next) => {
  const startedAt = Date.now();
  console.log(`[REQ] ${req.method} ${req.originalUrl} from ${req.ip}`);

  res.on('finish', () => {
    const duration = Date.now() - startedAt;
    console.log(`[RES] ${req.method} ${req.originalUrl} -> ${res.statusCode} (${duration}ms)`);
  });

  next();
});

app.get('/', (req, res) => {
  res.send('Hello World');
});

app.get('/login', (req, res) => {
  res.sendFile(path.join(__dirname, 'login.html'));
});

app.get('/auth/naver', (req, res, next) => {
  if (!NAVER_CLIENT_ID || !NAVER_CLIENT_SECRET) {
    return res.status(500).json({ message: 'NAVER 환경변수가 설정되지 않았습니다.' });
  }
  passport.authenticate('naver')(req, res, next);
});

app.get('/auth/naver/callback', (req, res, next) => {
  if (!NAVER_CLIENT_ID || !NAVER_CLIENT_SECRET) {
    return res.status(500).json({ message: 'NAVER 환경변수가 설정되지 않았습니다.' });
  }

  passport.authenticate('naver', { failureRedirect: '/login' }, (error, user) => {
    if (error || !user) {
      console.error('[NAVER] callback auth failed:', error ? error.message : 'no user');
      return res.status(401).json({ message: '네이버 로그인 실패' });
    }

    console.log('[NAVER] login user', user);
    return res.status(200).json({
      message: '네이버 로그인 성공',
      data: user,
    });
  })(req, res, next);
});

app.post('/login', async (req, res) => {
  const { sub, email, name } = req.body;
  console.log('[LOGIN] payload received', { sub, email, name });

  if (!sub || !email || !name) {
    console.warn('[LOGIN] validation failed: missing sub/email/name');
    return res.status(400).json({ message: 'sub, email, name은 필수입니다.' });
  }

  console.log('[LOGIN] data received', { sub, email, name });
  res.status(200).json({ sub, email, name });
});

app.listen(port, () => {
  console.log(`Server is running on http://localhost:${port}`);
});