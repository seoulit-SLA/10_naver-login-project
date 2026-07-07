const express = require('express');
const path = require('path');
const fs = require('fs'); // [추가] 파일 읽기를 위해 도입
require('dotenv').config();
const session = require('express-session');
const passport = require('passport');
const NaverStrategy = require('passport-naver-v2').Strategy;

const app = express();
const port = 3000;

const NAVER_CLIENT_ID = process.env.NAVER_CLIENT_ID;
const NAVER_CLIENT_SECRET = process.env.NAVER_CLIENT_SECRET;
const NAVER_CALLBACK_URL = process.env.NAVER_CALLBACK_URL || 'http://127.0.0.1:3000/auth/naver/callback';
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

  res.apiError = (statusCode, code, message) => {
    return res.status(statusCode).json({ code, message });
  };

  res.on('finish', () => {
    const duration = Date.now() - startedAt;
    console.log(`[RES] ${req.method} ${req.originalUrl} -> ${res.statusCode} (${duration}ms)`);
  });

  next();
});

app.get('/', (req, res) => {
  res.send('Hello World');
});

app.get('/health', (req, res) => {
  res.json({
    status: 'ok',
    timestamp: new Date().toISOString(),
  });
});

app.get('/login', (req, res) => {
  res.sendFile(path.join(__dirname, 'login.html'));
});

// ======================================================================
// [딸깍 자동 라우팅 시스템]
// routes/ 폴더 내부 자바스크립트 파일을 읽어와 파일명 주소로 자동 연동합니다.
// (예: routes/auth.js ➔ /auth 연동, routes/scores.js ➔ /scores 연동)
// ======================================================================
const routesPath = path.join(__dirname, 'routes');

if (fs.existsSync(routesPath)) {
  fs.readdirSync(routesPath).forEach(file => {
    if (file.endsWith('.js')) {
      const routeName = file.replace('.js', '');
      const router = require(path.join(routesPath, file));
      
      app.use(`/${routeName}`, router);
      console.log(`[Auto-Route] Mounted: /${routeName} ➔ (routes/${file})`);
    }
  });
} else {
  console.error(`[Auto-Route] 에러: routes 폴더를 찾을 수 없습니다.`);
}
// ======================================================================

app.use((err, req, res, next) => {
  console.error('[SERVER_ERROR]', err);

  if (res.headersSent) {
    return next(err);
  }

  return res.status(500).json({
    code: 'SERVER_ERROR',
    message: '서버 내부 오류가 발생했습니다.',
  });
});

app.listen(port, () => {
  console.log(`Server is running on http://localhost:${port}`);
});