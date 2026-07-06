# Naver Test Tools & Full Reset 가이드

Unity Editor에서 Naver OAuth **첫 로그인(동의하기) 테스트**를 반복하기 위한 도구입니다.  
`Tools → Naver Login → Test Tools` 메뉴로 엽니다.

---

## 1. Full Reset 버튼 — 무슨 일을 하는가

Full Reset은 **로컬·서버·브라우저·네이버 연동**을 한 번에 초기화해, 다시 OAuth(동의하기)를 테스트할 수 있는 상태로 만듭니다.

### 실행 순서 (5단계)

| 순서 | 동작 | 구현 위치 |
|------|------|-----------|
| 1 | **Chrome 종료 + 기본 프로필 쿠키/세션 스토리지 삭제** | `tools/clear-chrome-browser-data.ps1` → `ChromeDevCleaner.cs` |
| 2 | **Unity sessionToken 파일 삭제** | `SessionTokenStore.ClearToken()` |
| 3 | **DB users 전체 삭제 + Naver Token Revocation API 호출** | `POST /test/reset` → `server/services/testReset.js` |
| 4 | **Fresh Test 모드 ON** | `NaverLoginTestMode.SetFreshOAuthPending(true)` — 다음 Play 로그인 시 `?fresh=1` 사용 |
| 5 | **Chrome 시크릿: Naver 로그아웃 + [외부 서비스 연결] 페이지 오픈** | `BrowserLauncher.OpenUrl(..., incognito: true)` |

### 3단계 상세 (서버)

`POST /test/reset` body: `{ "deleteAll": true }`

1. DB에 저장된 모든 사용자의 `naver_refresh_token`(우선) 또는 `naver_access_token`을 복호화
2. 네이버 **`POST https://nid.naver.com/oauth2.0/revoke`** 호출 → **앱 연동 해제**
3. `DELETE FROM users` 실행
4. 응답 예: `{ "deletedCount": 1, "revokedTokenCount": 1 }`

> **중요:** 예전 `grant_type=delete` 방식은 토큰만 무효화하고 앱 연동은 남을 수 있습니다.  
> 동의 화면 재노출에는 공식 **`/oauth2.0/revoke`** API가 필요합니다.

### 5단계 상세 (브라우저)

| URL | 목적 |
|-----|------|
| `https://nid.naver.com/nidlogin.logout?returl=...` | 브라우저 Naver 로그인 세션 해제 |
| `https://nid.naver.com/user2/help/myInfo?m=viewSecurity` | [보안설정] → [외부 서비스 연결] — 앱 연결 수동 해제/확인 |

- `revokedTokenCount > 0`: revoke API로 이미 연동 해제됨 → disconnect 페이지는 **확인용**
- `revokedTokenCount === 0`: DB에 토큰 없음 → disconnect 페이지에서 **직접 연결 해제 필요**

### Full Reset 이후 사용자가 할 일

1. 시크릿 창에서 Naver 로그아웃·연동 해제 확인
2. Unity **Play** → **로그인** 버튼
3. Chrome **시크릿** + `/auth/naver?fresh=1` OAuth → **동의하기** 화면 확인

---

## 2. 사용자 동의 화면을 다시 보이게 한 방법

### 왜 동의 화면이 안 나왔는가

네이버 OAuth 동의 상태는 **브라우저 쿠키가 아니라 네이버 계정 + client_id(앱) 연동**으로 관리됩니다.

| 시도 | 동의 화면 재노출 |
|------|------------------|
| Chrome 쿠키 삭제만 | ❌ (OAuth는 시크릿 창 사용, 연동은 네이버 서버에 유지) |
| DB users 삭제만 | ❌ |
| `auth_type=reprompt` (`?fresh=1`) | ⚠️ **제한적** — 이미 전체 동의한 사용자에게는 거의 효과 없음 (네이버 공식: 거부한 프로필 항목 재동의용) |
| **`POST /oauth2.0/revoke`** (연동 해제) | ✅ 다음 OAuth 시 **새 동의 절차** |
| 브라우저 Naver 로그아웃 + [외부 서비스 연결] 해제 | ✅ revoke 보조 / DB 토큰 없을 때 필수 |

### 우리가 적용한 해결책 (3겹)

```
[Full Reset]
    ├─ 서버: revoke API → 네이버 "연결된 서비스 관리"에서 앱 제거
    ├─ 브라우저: 시크릿 Naver logout → nid 세션 해제
    ├─ 브라우저: 시크릿 [외부 서비스 연결] → 수동 해제 백업
    └─ 클라이언트: Fresh Test ON → Play 로그인 시 ?fresh=1 + auth_type=reprompt
```

#### 서버 — revoke API

`server/services/naverAuth.js`

```javascript
POST https://nid.naver.com/oauth2.0/revoke
Body: client_id, client_secret, token, token_type_hint (access_token | refresh_token)
```

성공 시(HTTP 200): 토큰 폐기 + **내정보 > 연결된 서비스 관리** 항목 삭제 + 재연동 시 새 동의 절차.

#### 서버 — Fresh OAuth

`GET /auth/naver?fresh=1` → `passport.authenticate('naver', { authType: 'reprompt' })`

보조 수단. **연동 해제(revoke)가 핵심**, reprompt만으로는 이미 동의한 사용자에게 동의 화면이 안 뜰 수 있음.

#### Unity — Fresh Test + 시크릿 OAuth

`Main.cs` — `NaverLoginTestMode.IsFreshOAuthPending`이면:

- URL: `http://127.0.0.1:3000/auth/naver?fresh=1`
- `BrowserLauncher.OpenUrl(..., incognito: true)` — 기존 Chrome 프로필 세션과 분리

### 권장 테스트 순서

1. **한 번 로그인** (DB에 naver_refresh_token 저장)
2. **Full Reset** (revoke + 브라우저 해제)
3. **Play → 로그인** → 동의하기 확인

DB가 비어 있는 상태에서 Full Reset하면 revoke할 토큰이 없으므로, **시크릿 [외부 서비스 연결]에서 수동 해제**가 필요합니다.

---

## 3. Naver Test Tools — 구조와 기능

### 메뉴

`Tools → Naver Login → Test Tools`

### UI 구성

| 영역 | 내용 |
|------|------|
| Server URL | 테스트 API 호출 주소 (기본 `http://127.0.0.1:3000`, EditorPrefs 저장) |
| Status | Session Token / DB Users / Fresh Test / Test API 상태 |
| **Full Reset** | 유일한 액션 버튼 |
| Log | 실행 로그 (최근 40줄) |

### 사전 조건

`server/.env`:

```env
ENABLE_TEST_ROUTES=true
NAVER_CLIENT_ID=...
NAVER_CLIENT_SECRET=...
```

서버 실행: `cd server && npm start`

### 관련 파일

#### Unity Editor

| 파일 | 역할 |
|------|------|
| `client/Assets/Editor/NaverLoginTestToolsWindow.cs` | EditorWindow UI, Full Reset 오케스트레이션 |
| `client/Assets/Editor/ChromeDevCleaner.cs` | PowerShell 스크립트 실행 |

#### Unity Runtime (테스트 연동)

| 파일 | 역할 |
|------|------|
| `client/Assets/NaverLoginTestMode.cs` | Fresh Test 플래그(PlayerPrefs), Naver logout/disconnect URL 상수 |
| `client/Assets/SessionTokenStore.cs` | `{persistentDataPath}/auth/sessionToken.txt` 저장/삭제 |
| `client/Assets/BrowserLauncher.cs` | Chrome/Edge 시크릿 창으로 URL 오픈 |
| `client/Assets/Main.cs` | Fresh Test 시 `?fresh=1` OAuth, HttpListener 콜백 |
| `client/Assets/AuthLogger.cs` | 클라이언트 구조화 로그 |

#### Server (테스트 전용)

| 파일 | 역할 |
|------|------|
| `server/routes/testReset.js` | `GET /test/status`, `POST /test/reset` |
| `server/services/testReset.js` | revoke → delete users, `getTestUserStats()` |
| `server/services/naverAuth.js` | `revokeNaverToken`, `refreshNaverToken` |
| `server/app.js` | `/auth/naver?fresh=1` reprompt 처리 |

#### Tools

| 파일 | 역할 |
|------|------|
| `tools/clear-chrome-browser-data.ps1` | Chrome 프로세스 종료 + Default 프로필 Cookies/Local Storage/Session Storage 삭제 |

### 테스트 API

```http
GET /test/status
```

```json
{
  "enabled": true,
  "userCount": 1,
  "usersWithRefreshToken": 1,
  "endpoints": { ... }
}
```

```http
POST /test/reset
Content-Type: application/json

{ "deleteAll": true }
```

```json
{
  "message": "users 테이블 전체 삭제 완료",
  "deletedCount": 1,
  "revokedTokenCount": 1
}
```

---

## 4. 다른 에이전트/세션에 넘길 프롬프트

아래 블록을 그대로 복사해 다른 AI 에이전트에게 전달하면, 동일한 Test Tools를 구현·유지보수할 수 있습니다.

---

```markdown
## 작업 컨텍스트

프로젝트: `naver-login-project` (Express OAuth 서버 + Unity 클라이언트)

목표: Unity Editor **Naver Test Tools** — Full Reset 한 버튼으로 Naver OAuth **동의하기(첫 로그인) 테스트** 환경을 초기화한다.

## 반드시 지켜야 할 사실 (Naver OAuth)

1. 동의 화면은 **브라우저 쿠키 삭제만으로는 재노출되지 않는다.** 네이버는 계정↔앱 연동을 서버에 저장한다.
2. `auth_type=reprompt` (`?fresh=1`)는 **이미 전체 동의한 사용자**에게 동의 화면을 강제하지 **않는다** (거부한 프로필 항목 재동의용).
3. 동의 화면을 다시 보려면 **앱 연동 해제**가 필요하다. 공식 방법: `POST https://nid.naver.com/oauth2.0/revoke` (구 `grant_type=delete` 사용 금지).
4. OAuth는 Unity에서 **Chrome 시크릿**으로 연다 (`BrowserLauncher.cs`). 일반 프로필 쿠키 삭제와 시크릿 세션은 별개다.
5. DB에 토큰이 없으면 revoke API를 호출할 수 없다 → [외부 서비스 연결] 페이지에서 **수동 해제** 필요.

## Full Reset이 해야 할 일 (순서 고정)

1. Chrome 종료 + Default 프로필 쿠키/세션 삭제 (`tools/clear-chrome-browser-data.ps1`)
2. Unity `SessionTokenStore.ClearToken()`
3. `POST /test/reset` `{ deleteAll: true }`:
   - DB users의 refresh_token(우선) 또는 access_token 복호화
   - `POST /oauth2.0/revoke` 로 네이버 앱 연동 해제
   - `DELETE FROM users`
4. `NaverLoginTestMode.SetFreshOAuthPending(true)` — 다음 Play 로그인 시 `/auth/naver?fresh=1`
5. Chrome 시크릿으로 열기:
   - Naver logout: `https://nid.naver.com/nidlogin.logout?returl=https%3A%2F%2Fwww.naver.com`
   - [외부 서비스 연결]: `https://nid.naver.com/user2/help/myInfo?m=viewSecurity`

## Unity Editor UI 요구사항

- 메뉴: `Tools/Naver Login/Test Tools`
- 클래스: `NaverLoginTestToolsWindow : EditorWindow`
- 버튼: **Full Reset 하나만** (복잡한 단계 버튼·Guided Test 등은 넣지 않음)
- 표시: Server URL, Status(Session Token, DB Users, Fresh Test, Test API), Log
- `ENABLE_TEST_ROUTES=false` 이면 경고 HelpBox

## 서버 요구사항

- `ENABLE_TEST_ROUTES=true` 일 때만:
  - `GET /test/status` → `enabled`, `userCount`, `usersWithRefreshToken`
  - `POST /test/reset` → `revokedTokenCount` 포함
- `GET /auth/naver?fresh=1` → `authType: 'reprompt'` (passport-naver-v2)
- revoke: `server/services/naverAuth.js` → `POST https://nid.naver.com/oauth2.0/revoke`

## Unity Runtime 연동

- `Main.cs`: `IsFreshOAuthPending`이면 `LoginUrl?fresh=1` + incognito
- Fresh Test 플래그는 PlayerPrefs (`NaverLogin.FreshOAuthTestPending`)

## 핵심 파일 목록

- `client/Assets/Editor/NaverLoginTestToolsWindow.cs`
- `client/Assets/Editor/ChromeDevCleaner.cs`
- `client/Assets/NaverLoginTestMode.cs`
- `client/Assets/BrowserLauncher.cs`
- `client/Assets/SessionTokenStore.cs`
- `client/Assets/Main.cs`
- `server/routes/testReset.js`
- `server/services/testReset.js`
- `server/services/naverAuth.js`
- `server/app.js` (`/auth/naver?fresh=1`)
- `tools/clear-chrome-browser-data.ps1`

## 검증 체크리스트

1. 서버 `npm start`, `.env`에 `ENABLE_TEST_ROUTES=true`
2. Unity Play → Naver 로그인 1회 (DB userCount ≥ 1)
3. Full Reset → 로그에 `revokedTokenCount: 1`, 시크릿 logout/disconnect 페이지 오픈
4. Play → 로그인 → **동의하기** 화면 표시
5. 동의 없이 바로 Unity 콜백으로 가면: revoke 실패 또는 연동 미해제 — DB 토큰·서버 로그 확인

## 하지 말 것

- `grant_type=delete` (구 토큰 삭제 API)로 연동 해제 대체하지 말 것
- reprompt만으로 동의 화면이 나온다고 가정하지 말 것
- EditorWindow에 불필요한 단계 버튼·Guided Test wizard 추가하지 말 것 (Full Reset 단일 버튼 유지)
- Unity C#에서 `init` 접근자·nullable struct 남용 금지 (구버전 Unity 호환)
```

---

## 5. 트러블슈팅

| 증상 | 원인 | 조치 |
|------|------|------|
| Test API Disabled | `ENABLE_TEST_ROUTES` 미설정 | `.env` 수정 후 서버 재시작 |
| `revokedTokenCount: 0` | DB에 토큰 없음 | 먼저 1회 로그인 후 Full Reset, 또는 시크릿 [외부 서비스 연결] 수동 해제 |
| 동의 없이 Unity 콜백 | 네이버 앱 연동仍 유지 | revoke API·disconnect 페이지 확인 |
| Chrome clear 실패 | Chrome 경로/권한 | PowerShell 스크립트 수동 실행 |
| incognito 실패 | Chrome 미설치 | Edge inprivate fallback (`BrowserLauncher.cs`) |

---

## 6. 참고 문서

- [네이버 로그인 개발가이드 — Token Revocation](https://developers.naver.com/docs/login/devguide/devguide.md)
- [네이버 로그인 API — /oauth2.0/revoke](https://developers.naver.com/docs/login/api/api.md)
- 프로젝트: `docs/integration-test-checklist.md`, `docs/naver-auto-login-guide.html`
