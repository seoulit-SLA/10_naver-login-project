# Naver OAuth 자동 로그인 아키텍처 가이드

첫 로그인에서만 Naver OAuth를 수행하고, 이후에는 자체 세션 토큰으로 자동 로그인하는 패턴입니다. Unity · Electron · 모바일 등 데스크톱/게임 클라이언트 프로젝트에 재사용할 수 있습니다.

---

## 개요

브라우저 기반 OAuth는 최초 1회만 실행합니다. 서버는 Naver로부터 받은 `accessToken`과 `refreshToken`을 DB에 암호화 저장하고, 클라이언트에는 자체 발급 `sessionToken`만 전달합니다.

### 로그인 흐름 요약
* **첫 로그인:** 브라우저 → Naver OAuth → 서버 토큰 저장 → `sessionToken` 발급
* **이후 로그인:** 클라이언트 `sessionToken` → 서버 검증 → OAuth 없이 진입
* **토큰 만료:** `refreshToken`으로 갱신, 실패 시 OAuth 재실행

> **✓ 적용 대상:** Unity, Unreal, Electron, Tauri 등 로컬 HTTP 콜백(`127.0.0.1`)을 사용하는 데스크톱/게임 클라이언트

---

## 설계 원칙

1. **Naver 토큰은 서버 전용**
   * `refreshToken`을 클라이언트(앱)에 저장하지 않습니다. PlayerPrefs, localStorage 등은 보안에 취약하여 쉽게 노출될 수 있습니다.
2. **자동 로그인은 자체 sessionToken 사용**
   * Naver 토큰은 "Naver API 호출 권한"일 뿐이며, 앱 자체의 내부 세션과는 목적이 다릅니다. JWT 등 서버가 독자적으로 발급한 토큰으로 인증합니다.
3. **OAuth는 예외 상황에서만 실행**
   * 최초 로그인, `refreshToken` 만료/폐기, 사용자 연동 해제, `sessionToken` 무효화 시에만 브라우저 OAuth를 다시 엽니다.
4. **Graceful Degradation (점진적 기능 저하)**
   * 자동 로그인 실패 시 사용자에게 재로그인 버튼을 제공하고, OAuth 플로우로 자연스럽게 전환(폴백)되도록 설계합니다.

---

## 토큰 역할 분리

| 토큰 | 저장 위치 | 용도 | 수명 |
| :--- | :--- | :--- | :--- |
| `naver_access_token` | 서버 DB (암호화) | Naver Open API 호출 | 약 1시간 (Naver 정책) |
| `naver_refresh_token` | 서버 DB (암호화) | `accessToken` 갱신 | 장기 (만료/폐기 가능) |
| `sessionToken` | 클라이언트 로컬 | 앱 자동 로그인 인증 | 서버 설정 (예: 30일) |

> **⚠ 주의:** Naver `refreshToken`도 영구적이지 않습니다. 사용자가 앱 연동을 해제하거나 Naver 정책이 변경될 경우 다시 OAuth 인증이 요구됩니다.

---

## 첫 로그인 흐름

```text
── 첫 로그인 (OAuth) ──

Client  → GET /auth/naver (브라우저 열기)
Browser → Naver 로그인 페이지
Naver   → GET /auth/naver/callback?code=...
Server  → Naver token endpoint (code → accessToken, refreshToken)
Server  → Naver profile API (uid, email, name)
Server  → DB upsert (profile + encrypted tokens)
Server  → JWT sessionToken 발급
Server  → Redirect http://127.0.0.1:{port}/callback?token={sessionToken}
Client  → HttpListener로 token 수신 → 로컬 저장
```

### Unity 콜백 URL 예시

```javascript
// 클라이언트가 수신 대기하는 prefix
http://127.0.0.1:7777/naver-login/

// OAuth 성공 후 서버가 리다이렉트하는 URL
http://127.0.0.1:7777/naver-login/?token=eyJhbGciOiJIUzI1NiIs...
```

---

## 자동 로그인 흐름

```text
── 앱 시작 / 재실행 ──

Client  → 로컬 sessionToken 존재 여부 확인
Client  → POST /auth/me  Authorization: Bearer {sessionToken}
Server  → JWT 검증 → uid 조회 → 사용자 정보 반환
Client  → 로그인 완료, 메인 화면 진입 (OAuth 없음)
```

> **✓** 이 경로가 일반적인 재로그인 프로세스입니다. 사용자는 추가적으로 브라우저를 열어 로그인 단계를 밟을 필요가 없습니다.

---

## 토큰 갱신 흐름

```text
── sessionToken 만료 또는 /auth/me 401 ──

Client  → POST /auth/refresh  Authorization: Bearer {sessionToken}
Server  → DB에서 naver_refresh_token 조회 (복호화)
Server  → Naver token refresh API 호출
Server  → 새 accessToken/refreshToken DB 저장
Server  → 새 sessionToken 발급 → 클라이언트에 반환
Client  → sessionToken 교체 저장
```

---

## 재인증 흐름 (OAuth 재실행)

다음 상황에서는 자동 갱신이 불가능하므로 OAuth를 처음부터 다시 실행합니다.

| 상황 | 서버 응답 | 클라이언트 동작 |
| :--- | :--- | :--- |
| 로컬 `sessionToken` 없음 | — | 로그인 버튼 노출 → 클릭 시 OAuth 진행 |
| `sessionToken` 만료 + refresh 실패 | `401` + `code: REAUTH_REQUIRED` | 로컬 토큰 삭제 → OAuth 진행 |
| Naver `refreshToken` 폐기 | `401` + `code: NAVER_TOKEN_REVOKED` | 로컬 토큰 삭제 → OAuth 진행 |
| 사용자 로그아웃 | `200` | 로컬 토큰 삭제 → 로그인 화면 복귀 |

---

## DB 스키마

```sql
CREATE TABLE users (
  id                    INT          NOT NULL AUTO_INCREMENT,
  uid                   VARCHAR(64)  NOT NULL,          -- Naver profile id
  email                 VARCHAR(255) NOT NULL,
  name                  VARCHAR(100) NOT NULL,
  naver_access_token    TEXT,                              -- AES 암호화
  naver_refresh_token   TEXT,                              -- AES 암호화
  token_expires_at      DATETIME,                            -- accessToken 만료 시각
  created_at            TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at            TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_users_uid (uid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### JWT sessionToken 페이로드 (예시)

```json
{
  "sub": "naver_uid_12345",     // users.uid
  "iat": 1710000000,
  "exp": 1712592000              // 30일 등 서버 정책에 맞춤 설정
}
```

---

## API 명세

### OAuth
* **`GET /auth/naver`** — Naver OAuth 시작 (브라우저 리다이렉트)
* **`GET /auth/naver/callback`** — OAuth 콜백 처리, DB 저장 완료 후 클라이언트 콜백 URL로 최종 리다이렉트

### Session
* **`POST /auth/me`** — `sessionToken` 검증 진행 및 사용자 정보 반환
* **`POST /auth/refresh`** — `sessionToken` + Naver `refreshToken`을 통한 토큰 갱신
* **`POST /auth/logout`** — 세션 무효화 처리 (선택 사항: JWT 블랙리스트 적용)

---

### POST /auth/me 상세

#### Request
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### Response 200 (성공)
```json
{
  "uid": "12345678",
  "email": "user@example.com",
  "name": "홍길동"
}
```

#### Response 401 (만료)
```json
{
  "message": "세션이 만료되었습니다.",
  "code": "SESSION_EXPIRED"
}
```

---

### OAuth 콜백 → 클라이언트 리다이렉션

#### 로그인 성공 시
```javascript
res.redirect(`http://127.0.0.1:7777/naver-login/?token=${sessionToken}`);
```

#### 로그인 실패 시
```javascript
res.redirect(`http://127.0.0.1:7777/naver-login/?error=auth_failed`);
```

---

## 서버 구현 가이드

### 권장 디렉터리 구조
```text
server/
├── app.js                 # Express 애플리케이션 진입점
├── db.js                  # DB 연결 및 커넥션 풀 설정
├── routes/
│   └── auth.js            # /auth/* 관련 API 라우터
├── services/
│   ├── naverAuth.js       # Naver 프로필 조회 및 토큰 갱신 서비스
│   ├── session.js         # 자체 JWT 발급 및 검증 모듈
│   └── crypto.js          # DB 보관용 토큰 암호화/복호화 (AES)
├── migrations/
│   └── 001_users.sql      # DB 마이그레이션 스키마 파일
└── .env.example           # 환경변수 가이드 파일
```

### OAuth 콜백 핵심 로직
```javascript
passport.use(new NaverStrategy(config, async (accessToken, refreshToken, profile, done) => {
  const user = {
    uid: profile.id,
    email: profile.email,
    name: profile.name || profile.nickname,
  };

  // 1. DB에 회원 등록 혹은 토큰 업데이트 (토큰은 대칭키 암호화 처리)
  await upsertUser(user, accessToken, refreshToken);

  // 2. 클라이언트에 보낼 세션 전용 JWT 발급
  const sessionToken = signSessionToken(user.uid);

  return done(null, { ...user, sessionToken });
}));
```

---

## 클라이언트 구현 가이드

### 앱 시작 시 의사결정 트리
```text
OnAppStart()
│
├─ sessionToken = LoadFromLocal()
│
├─ sessionToken == null
│   └─ ShowLoginButton()  → 클릭 시 브라우저 통화 OAuth 시작
│
└─ sessionToken != null
    └─ response = POST /auth/me
        ├─ 200 → EnterMainScreen()
        ├─ 401 SESSION_EXPIRED → TryRefresh()
        │   ├─ success → SaveToken → EnterMainScreen()
        │   └─ fail → ClearToken → ShowLoginButton()
        └─ 401 REAUTH_REQUIRED → ClearToken → ShowLoginButton()
```

### Unity HttpListener 콜백 처리
```csharp
private void HandleCallback(HttpListenerRequest request)
{
    var query = HttpUtility.ParseQueryString(request.Url.Query);
    var token = query["token"];
    var error = query["error"];

    if (!string.IsNullOrEmpty(token))
    {
        // 로컬에 세션 토큰 보관
        PlayerPrefs.SetString("sessionToken", token);
        PlayerPrefs.Save();
        EnterMainScreen();
    }
    else
    {
        Debug.LogError($"Login failed: {error}");
    }
}
```

> **ℹ 참고:** 릴리즈 환경(프로덕션)에서는 `PlayerPrefs` 대신 OS 시스템 키체인(Windows Credential Manager, macOS Keychain)이나 개별 보안 암호화 스토리지의 활용을 강력히 권장합니다.

---

## 보안 가이드

| 보안 항목 | 권장 사항 | 권장하지 않음 |
| :--- | :--- | :--- |
| Naver `refreshToken` | 서버 DB 내에 대칭키 암호화(AES-256) 저장 | 클라이언트 로컬 저장, 평문 형태로 DB 저장 |
| `sessionToken` | JWT(HS256/RS256), 짧은 만료 기간 + 재발급 구조 | 유저 식별 코드(UID) 등 평이한 데이터 인코딩 |
| 통신 보안 | HTTPS 통신 적용 (프로덕션 필수) | 일반 HTTP 통신 (로컬 개발 단계만 허용) |
| 콜백 URL | `127.0.0.1` 고정 루프백 및 신뢰 포트 지정 | 임의 가변 포트 지정 및 검증 없는 도메인 사용 |
| 로그아웃 프로세스 | 클라이언트 로컬 토큰 삭제 + 서버 무효화(블랙리스트) 처리 | 단순히 클라이언트단 토큰 값만 파괴 |

> **✕ 절대 금지:** `.env` 상의 Naver Client Secret, DB 패스워드, JWT Secret Key 등을 Git 저장소에 커밋하거나 클라이언트 빌드 패키지 내부에 하드코딩 형태로 포함하지 않도록 주의하십시오.

---

## 환경 변수 설정 (`.env`)

```env
# Naver OAuth
NAVER_CLIENT_ID=your_client_id
NAVER_CLIENT_SECRET=your_client_secret
NAVER_CALLBACK_URL=http://127.0.0.1:3000/auth/naver/callback

# Session
JWT_SECRET=your-random-secret-min-32-chars
JWT_EXPIRES_IN=30d

# Token encryption (AES-256)
TOKEN_ENCRYPTION_KEY=your-32-byte-hex-key

# Client callback (OAuth success redirect)
CLIENT_CALLBACK_URL=http://127.0.0.1:7777/naver-login/

# Database
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASSWORD=
DB_NAME=gamedb
```

---

## 구현 체크리스트

### Phase 1 — DB & Server Foundation
- [ ] `users` 테이블 스키마 생성 및 필드 구조 구성 (UID, 암호화 토큰 필드 반영)
- [ ] 암호화 모듈 구현 (`services/crypto.js` 내 복호화/암호화 로직 구축)
- [ ] JWT 발행 및 키 검증 로직 구현 (`services/session.js` 연동)
- [ ] OAuth 콜백 처리 완료: DB Upsert 기능 연동 및 세션용 토큰 발행, 리다이렉트 연결

### Phase 2 — Auth API
- [ ] `POST /auth/me` — 토큰 검증 미들웨어 및 응답 구현
- [ ] `POST /auth/refresh` — Naver Token Refresh 및 새로운 Session 발행 연결
- [ ] `POST /auth/logout` — 만료 세션 키 처리 및 서버 무효화

### Phase 3 — Client
- [ ] `HttpListener` 내 응답 수신 구조 및 데이터 스토리지 이식
- [ ] 앱 초기 구동 시 `POST /auth/me` 연동 자동 로그인 검사 분기 로직 마련
- [ ] 세션 키 유효 유무 감지에 따른 조건식 로그인 창 분기 처리
- [ ] 401 오류 검출 시 토큰 갱신 시도 및 실패 시 리로그인 연동 설계

### Phase 4 — Test
- [ ] 최초 가입 동작: 브라우저 인증 성공 ➡️ DB 저장 완료 ➡️ 성공 복귀 과정 일치 확인
- [ ] 재구동 테스트: 별도의 로그인 창 팝업 없이 로컬 토큰 기반 메인 로비 진입 검증
- [ ] 토큰 만료 처리: 가상 만료 생성 후 갱신 통신 테스트
- [ ] 외부 플랫폼 연동 제거 테스트: 인가 상실 시 안전하게 클라이언트 로그인 복귀 처리 검증
- [ ] 완전 로그아웃: 로컬 저장 내역 삭제 확인 및 화면 동기화 체크

---

## 에러 코드 가이드

| 에러 코드 (`code`) | HTTP Status | 상태 의미 | 대응 가이드라인 |
| :--- | :--- | :--- | :--- |
| `SESSION_EXPIRED` | 401 | 자체 세션 토큰 유효시간 초과 | `/auth/refresh` API 갱신 요청 |
| `SESSION_INVALID` | 401 | 서명 및 위변조 값 불일치 감지 | 기저 토큰 강제 소거 ➡️ 신규 로그인 연동 |
| `NAVER_TOKEN_REVOKED` | 401 | 연동 해제 등의 이유로 토큰 무효화 | 기저 토큰 강제 소거 ➡️ 신규 로그인 연동 |
| `REAUTH_REQUIRED` | 401 | 연동 및 갱신 프로세스 수행 불가 | 기저 토큰 강제 소거 ➡️ 신규 로그인 연동 |
| `USER_NOT_FOUND` | 404 | DB 테이블 내 일치 유저 없음 | 기저 토큰 강제 소거 ➡️ 신규 로그인 연동 |