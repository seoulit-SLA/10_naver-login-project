# 기술 용어 설명집 (완전 초보용)

이 문서는 **naver-login-project**를 처음 접하는 분을 위해, 코드·가이드·테스트 도구에 나오는 기술 용어를 **아주 쉽게** 풀어 쓴 설명집입니다.

> **읽는 방법:** 모르는 단어가 나오면 여기서 찾아보세요. 각 항목은 **한 줄 요약 → 비유 → 이 프로젝트에서** 순서로 적혀 있습니다.

---

## 목차

1. [인터넷·프로그램 기본](#1-인터넷프로그램-기본)
2. [클라이언트와 서버](#2-클라이언트와-서버)
3. [브라우저·웹 관련](#3-브라우저웹-관련)
4. [로그인·보안 (가장 중요)](#4-로그인보안-가장-중요)
5. [네이버 OAuth 전용 용어](#5-네이버-oauth-전용-용어)
6. [서버(Node.js) 쪽 용어](#6-서버nodejs-쪽-용어)
7. [데이터베이스(DB)](#7-데이터베이스db)
8. [Unity·게임 클라이언트](#8-unity게임-클라이언트)
9. [테스트 도구 (Naver Test Tools)](#9-테스트-도구-naver-test-tools)
10. [헷갈리기 쉬운 것 정리표](#10-헷갈리기-쉬운-것-정리표)

---

## 1. 인터넷·프로그램 기본

### 프로그램 / 앱 (Application)

**한 줄:** 컴퓨터나 폰에서 실행되는 소프트웨어.

**비유:** 스마트폰의 "카카오톡", PC의 "Chrome"처럼 눌러서 쓰는 것.

**이 프로젝트에서:** Unity로 만든 **게임(클라이언트)** 과 Node.js로 만든 **서버 프로그램** 두 가지가 있습니다.

---

### 코드 / 소스 코드

**한 줄:** 개발자가 작성한 명령 목록. 컴퓨터가 읽고 실행합니다.

**비유:** 요리 **레시피**. 재료(데이터)와 순서(로직)가 적혀 있습니다.

**이 프로젝트에서:** `Main.cs`(Unity), `app.js`(서버) 등이 소스 코드 파일입니다.

---

### 파일 / 폴더

**한 줄:** 컴퓨터에 저장된 문서(파일)와 그룹(폴더).

**비유:** 서랍(폴더) 안의 종이(파일).

**이 프로젝트에서:**
- `client/` — Unity 프로젝트
- `server/` — Express 서버
- `docs/` — 설명 문서
- `tools/` — 보조 스크립트 (Chrome 쿠키 삭제 등)

---

### 환경 변수 (.env)

**한 줄:** 프로그램 밖에 적어 두는 **비밀 설정값** 모음.

**비유:** 금고 번호·열쇠를 코드 안에 직접 쓰지 않고, **별도 메모**에 적어 두는 것.

**이 프로젝트에서:** `server/.env`에 네이버 Client ID, DB 비밀번호, `ENABLE_TEST_ROUTES=true` 등을 넣습니다. Git에 올리면 안 됩니다.

---

### API (에이피아이)

**한 줄:** 프로그램끼리 **정해진 방식으로 요청하고 답 받는 창구**.

**비유:** 식당 **주문 창구**. "로그인 상태 알려줘"처럼 정해진 메뉴를 주문하면 JSON 같은 형태로 답이 옵니다.

**이 프로젝트에서:**
- `POST /auth/me` — "내가 로그인돼 있어?"
- `POST /test/reset` — "테스트용 DB 초기화해줘"

---

### HTTP / HTTPS

**한 줄:** 인터넷에서 **요청·응답을 주고받는 규칙**.

**비유:** 택배 **표준 포장 규격**. 누가 보내도 받는 쪽이 같은 방식으로 이해합니다.

**HTTPS**는 내용이 **암호화**된 HTTP입니다 (주소창 자물쇠 🔒).

**이 프로젝트에서:** Unity ↔ 서버, 서버 ↔ 네이버 모두 HTTP(S)로 통신합니다.

---

### URL (주소)

**한 줄:** 인터넷上的 **정확한 위치**.

**예:** `http://127.0.0.1:3000/auth/naver`

**구성:**
| 부분 | 의미 | 예 |
|------|------|-----|
| `http://` | 프로토콜(규칙) | http 또는 https |
| `127.0.0.1` | **호스트**(어느 컴퓨터) | IP 주소 |
| `:3000` | **포트**(문 번호) | 같은 PC의 여러 프로그램 구분 |
| `/auth/naver` | **경로**(어느 기능) | 라우트와 연결 |

---

### IP 주소 / localhost / 127.0.0.1

**한 줄:** 네트워크에 연결된 기기의 **집 주소**.

- **127.0.0.1** = **localhost** = "지금 내 컴퓨터 자신"
- 다른 사람 PC 주소가 아닙니다. **내 PC에서만** 접속 가능한 개발용 주소입니다.

**이 프로젝트에서:**
- 서버: `http://127.0.0.1:3000`
- Unity 콜백: `http://127.0.0.1:7777/naver-login/`

---

### 포트 (Port)

**한 줄:** 한 컴퓨터 안에서 **프로그램을 구분하는 번호**.

**비유:** 같은 건물(127.0.0.1)의 **방 번호**. 3000번 방=서버, 7777번 방=Unity 콜백.

---

## 2. 클라이언트와 서버

### 클라이언트 (Client)

**한 줄:** 서비스를 **이용하는 쪽** (사용자 쪽 프로그램).

**비유:** 식당 **손님**.

**이 프로젝트에서:** **Unity 게임**이 클라이언트입니다. 로그인 버튼을 누르고, sessionToken을 저장합니다.

---

### 서버 (Server)

**한 줄:** 요청을 **받아 처리하고 결과를 돌려주는** 프로그램. 보통 24시간 켜져 있습니다.

**비유:** 식당 **주방 + 계산대**.

**이 프로젝트에서:** `server/` 폴더의 Express 앱. 네이버와 통신하고, DB에 사용자·토큰을 저장합니다.

---

### 요청 (Request) / 응답 (Response)

**한 줄:** 클라이언트가 **보내는 것** / 서버가 **돌려주는 것**.

**비유:** "로그인 상태 확인해 주세요"(요청) → "네, 홍길동님입니다"(응답).

**HTTP 메서드 (자주 나오는 것):**
| 메서드 | 의미 | 예 |
|--------|------|-----|
| **GET** | **가져와** (조회) | 브라우저 주소창 입력, `/test/status` |
| **POST** | **보내서 처리해** (등록·로그인 등) | `/auth/me`, `/test/reset` |

---

### JSON

**한 줄:** 데이터를 **텍스트로** `{ "이름": "값" }` 형태로 표현한 형식.

**예:**
```json
{ "uid": "abc123", "email": "user@example.com" }
```

**이 프로젝트에서:** API 응답·요청 body 대부분이 JSON입니다.

---

## 3. 브라우저·웹 관련

### 브라우저 (Browser)

**한 줄:** 웹 페이지를 여는 프로그램 (Chrome, Edge 등).

**이 프로젝트에서:** Naver 로그인·동의 화면은 **브라우저**에서 열립니다. Unity 안이 아니라 **외부 Chrome 시크릿 창**입니다.

---

### 쿠키 (Cookie)

**한 줄:** 웹사이트가 브라우저에 **작은 메모를 저장**해 두는 것.

**비유:** 단골 카페 **스탬프 카드**. 다음에 와도 "아, 이 사람이구나" 기억.

**특징:**
- **사이트별**로 저장 (naver.com 쿠키, google.com 쿠키 따로)
- **브라우저·프로필별**로 저장 (Chrome 일반 / 시크릿은 **완전히 별개**)

**이 프로젝트에서:**
- Naver **로그인 상태**가 쿠키에 남을 수 있음
- Full Reset 시 **Chrome 기본 프로필** 쿠키를 삭제하지만, **시크릿 창**은 별도라서 **로그아웃 URL**도 따로 엽니다

---

### 시크릿 / Incognito (익명) 모드

**한 줄:** **임시** 브라우저 창. 닫으면 대부분 기록·쿠키가 사라집니다.

**비유:** **일회용** 방. 일반 Chrome 집(프로필)과 **쿠키를 공유하지 않음**.

**이 프로젝트에서:** `BrowserLauncher.cs`가 OAuth를 **시크릿**으로 열어, 평소 Chrome에 남은 Naver 로그인과 섞이지 않게 합니다.

---

### 리다이렉트 (Redirect)

**한 줄:** "이 주소 말고 **저 주소로 이동**해"라고 브라우저에게 알려주는 것.

**비유:** 안내판 **"→ 출구는 저쪽".

**이 프로젝트에서:** Naver 로그인 성공 후 → 서버 → `http://127.0.0.1:7777/naver-login/?token=...` 로 Unity에게 돌아옵니다.

---

### 콜백 (Callback) / Callback URL

**한 줄:** "일 끝나면 **여기로 돌아와**"라고 미리 정해 둔 주소.

**이 프로젝트에서:**
- **서버 콜백:** Naver → `http://localhost:3000/auth/naver/callback`
- **Unity 콜백:** 서버 → `http://127.0.0.1:7777/naver-login/?token=...`

---

### 쿼리 파라미터 (? 뒤)

**한 줄:** URL 끝에 붙는 **옵션**.

**예:** `/auth/naver?fresh=1`
- `fresh=1` → "테스트용 fresh OAuth 모드로 시작해줘"

---

## 4. 로그인·보안 (가장 중요)

### 로그인 (Login)

**한 줄:** "나는 **등록된 사용자 ○○**입니다"라고 **증명**하는 과정.

**이 프로젝트에서:** 처음은 **Naver OAuth**, 이후는 저장된 **sessionToken**으로 자동 로그인.

---

### 인증 (Authentication)

**한 줄:** **신원 확인** — "너 누구야?"

**예:** Naver 아이디·비밀번호, sessionToken 검증.

---

### 인가 (Authorization)

**한 줄:** **권한 확인** — "너 이거 해도 돼?"

**예:** "이름·이메일 제공 **동의**했니?" (동의하기 화면).

> 초보자가 자주 헷갈림: **인증=누구인지**, **인가=뭘 해도 되는지**.

---

### 세션 (Session)

**한 줄:** 로그인한 **상태가 유지되는 기간**.

**비유:** 놀이공원 **손목밴드**. 밴드 있으면 "입장한 사람"으로 취급.

**두 가지 의미 (헷갈림 주의):**

| 종류 | 어디에 | 이 프로젝트 |
|------|--------|-------------|
| **웹 서버 세션** | 서버 메모리/쿠키 | Express `express-session` (OAuth 중간 단계) |
| **로그인 상태(일반적 의미)** | "지금 로그인됐다"는 상태 전체 | sessionToken으로 표현 |

---

### 토큰 (Token)

**한 줄:** **신분을 증명하는 짧은 문자열** (열쇠, 입장권).

**비유:** 
- **콘서트 wristband** — 보여주면 통과
- 비밀번호 전체를 매번 보내지 않고 **토큰**만 보냄

**이 프로젝트의 토큰 종류 (각각 다름!):**

| 이름 | 누가 발급 | 어디 저장 | 용도 |
|------|-----------|-----------|------|
| **Naver access_token** | Naver | 서버 DB (암호화) | Naver API 호출 |
| **Naver refresh_token** | Naver | 서버 DB (암호화) | access_token 만료 시 갱신 |
| **sessionToken** (JWT) | **우리 서버** | Unity 로컬 파일 | 게임 자동 로그인 |

> **절대 혼동 금지:** sessionToken ≠ Naver access_token. **이름만 비슷한 다른 열쇠**입니다.

---

### JWT (JSON Web Token)

**한 줄:** JSON 내용을 **서명**해서 위조하기 어렵게 만든 토큰.

**우리 sessionToken**이 JWT입니다. 서버만 만들 수 있고, Unity는 **검증 요청**(`POST /auth/me`)으로 유효성을 확인합니다.

---

### Bearer Token

**한 줄:** HTTP 헤더에 `Authorization: Bearer {토큰}` 형태로 **토큰을 실어 보내는** 방식.

**예:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

### 암호화 (Encryption) / 복호화 (Decryption)

**한 줄:** 
- **암호화** — 읽을 수 있는 글을 **난독화**해서 저장
- **복호화** — 다시 **원래대로** 읽기

**이 프로젝트에서:** Naver refresh_token 등은 DB에 **암호화**해서 저장 (`TOKEN_ENCRYPTION_KEY` 사용). 평문으로 DB에 넣지 않습니다.

---

### OAuth / OAuth 2.0

**한 줄:** "비밀번호를 우리 앱에 주지 않고, **Naver 같은 큰 서비스**가 로그인·동의를 대신 처리"하는 **표준 방식**.

**비유:** 
- 직접: "우리 게임 비밀번호 알려줘" ❌
- OAuth: "Naver야, 이 사람 로그인 맞는지 확인하고 **필요한 정보만** 줘" ✅

**흐름 (아주 단순):**
1. Unity → 브라우저로 Naver 로그인 페이지
2. 사용자 동의
3. Naver → 우리 서버에 **code** 전달
4. 서버 → Naver에 code 교환 → **access_token** 받음
5. 서버 → Unity에 **sessionToken** 전달

---

### 동의하기 화면 / Scope (스코프)

**한 줄:** "이름, 이메일 등 **어떤 정보를 앱에 줄지**" 사용자에게 묻는 화면.

**스코프** = 요청하는 **정보 항목 목록**.

**중요:** 한 번 **전체 동의**하면, Naver는 같은 앱에 **다시 동의 화면을 안 보여줄 수 있음**. 그래서 Full Reset + **연동 해제(revoke)** 가 필요합니다.

---

### 연동 / 연동 해제

**한 줄:** 
- **연동** — "내 Naver 계정 ↔ 이 앱" 연결
- **연동 해제** — 그 연결 **끊기**

**비유:** 카카오 **간편로그인 연결된 앱 목록**에서 삭제.

**이 프로젝트에서:** `POST /oauth2.0/revoke` 또는 Naver [외부 서비스 연결] 페이지.

---

### Revoke (리보크 / 토큰 폐기)

**한 줄:** 이미 준 토큰·연동을 **공식적으로 취소**하는 것.

**이 프로젝트에서:** Full Reset 시 서버가 Naver **`/oauth2.0/revoke`** API 호출 → 앱 연동 삭제 + 다음 로그인 시 동의 가능.

---

### reprompt / fresh=1

**한 줄:** OAuth 시작할 때 "동의 **다시** 받아줘"라고 Naver에 요청하는 옵션.

**주의:** **이미 전체 동의한 사용자**에게는 **거의 안 먹힘**. 네이버 공식: **거부했던 항목** 재동의용. **동의 화면의 핵심은 revoke(연동 해제)** 입니다.

---

## 5. 네이버 OAuth 전용 용어

### Client ID / Client Secret

**한 줄:** Naver 개발자센터에서 받은 **앱 신분증** (ID) + **비밀키** (Secret).

- **Client ID** — 공개해도 되는 편 (앱 이름판)
- **Client Secret** — **절대** 클라이언트(Unity)에 넣으면 안 됨. **서버만** 사용

---

### Redirect URI / Callback URL (네이버 등록)

**한 줄:** Naver 개발자센터에 **미리 등록**해 둔 "로그인 후 돌아올 주소".

등록과 **완전히 일치**해야 합니다. 틀리면 로그인 실패.

---

### authorization code (코드)

**한 줄:** Naver가 로그인 성공 후 **잠깐** 주는 **일회용 교환권**.

서버만 Naver와 교환해서 **access_token**을 받습니다. Unity는 code를 직접 다루지 않습니다.

---

### access_token / refresh_token

| | access_token | refresh_token |
|---|--------------|---------------|
| **역할** | 지금 당장 Naver API 쓸 **출입증** | access_token **새로 받을** 장기 열쇠 |
| **수명** | 짧음 (예: 1시간) | 김 |
| **저장** | 서버 DB (암호화) | 서버 DB (암호화) |
| **Unity** | ❌ 저장 안 함 | ❌ 저장 안 함 |

---

### Token Revocation API

**한 줄:** Naver 공식 **연동 해제** API.  
`POST https://nid.naver.com/oauth2.0/revoke`

---

### 외부 서비스 연결

**한 줄:** Naver 웹사이트 **내정보 → 보안설정** 메뉴. 연결된 앱 목록에서 **수동 해제** 가능.

Full Reset 시 시크릿 창으로 이 페이지를 엽니다.

---

## 6. 서버(Node.js) 쪽 용어

### Node.js

**한 줄:** JavaScript로 **서버 프로그램**을 만들 수 있게 해 주는 실행 환경.

**이 프로젝트에서:** `server/` 폴더, `npm start`로 실행.

---

### Express

**한 줄:** Node.js용 **웹 서버 프레임워크** (HTTP 요청 받기 쉽게).

---

### 라우트 (Route) / 라우터 (Router)

**한 줄:** 
- **라우트** — "이 **주소**로 오면 이 **기능** 실행"
- **라우터** — 라우트들을 **묶어 관리**하는 모듈

**비유:** 
- `/auth/naver` → "Naver 로그인 시작"
- `/auth/me` → "sessionToken 검증"
- `/test/reset` → "테스트 DB 초기화"

**파일:** `server/routes/auth.js`, `server/routes/testReset.js`

---

### 미들웨어 (Middleware)

**한 줄:** 요청이 목적지(라우트)에 도착하기 **전·중간**에 거치는 **검문소**.

**예:** "테스트 API 켜져 있니?" (`ensureTestRoutesEnabled`)

---

### Passport / passport-naver-v2

**한 줄:** Node.js에서 **OAuth 로그인**을 쉽게 연결해 주는 라이브러리.

**NaverStrategy** — Naver 전용 OAuth 설정.

---

### npm / package.json

**한 줄:** 
- **npm** — Node **패키지(라이브러리) 설치·실행** 도구
- **package.json** — 프로젝트 의존성 목록

**자주 쓰는 명령:**
- `npm install` — 패키지 설치
- `npm start` — 서버 실행

---

### 마이그레이션 (Migration)

**한 줄:** DB **테이블 구조를 버전에 맞게 바꾸는** 작업.

**예:** `npm run migrate:users-tokens` — users 테이블에 token 컬럼 추가.

---

## 7. 데이터베이스(DB)

### DB (Database) / MySQL

**한 줄:** 데이터를 **표 형태**로 **영구 저장**하는 창고.

**비유:** **엑셀 파일**을 서버가 관리. 행(row)=사용자 한 명.

---

### 테이블 (Table) / users 테이블

**한 줄:** DB 안의 **한 장의 표**.

**users 테이블 예시 컬럼:**
| 컬럼 | 의미 |
|------|------|
| uid | Naver 사용자 고유 ID |
| email, name | 프로필 |
| naver_access_token | (암호화) Naver 토큰 |
| naver_refresh_token | (암호화) 갱신 토큰 |
| token_expires_at | access_token 만료 시각 |

---

### SQL / DELETE / SELECT

**한 줄:** DB에게 명령하는 **언어**.

- `SELECT` — 조회
- `INSERT` — 추가
- `UPDATE` — 수정
- `DELETE` — 삭제

Full Reset: `DELETE FROM users` (전체 사용자 삭제)

---

### gamedb

**한 줄:** 이 프로젝트에서 쓰는 **DB 이름** (MySQL database name).

---

## 8. Unity·게임 클라이언트

### Unity

**한 줄:** **게임·앱**을 만드는 개발 도구 (엔진).

**이 프로젝트에서:** `client/` 폴더. 로그인 버튼, 자동 로그인 UI.

---

### Play 모드 (▶ Play)

**한 줄:** 에디터에서 **게임을 실행**해 보는 모드 (빌드 없이 테스트).

로그인 버튼·HttpListener는 **Play 중**에만 동작합니다.

---

### Editor / EditorWindow

**한 줄:** 
- **Editor** — Unity **개발 화면** (게임 만드는 쪽)
- **EditorWindow** — 에디터에 뜨는 **커스텀 창**

**Naver Test Tools** = `NaverLoginTestToolsWindow.cs` (Editor 전용, 빌드 게임에는 포함 안 됨)

---

### MonoBehaviour / Main.cs

**한 줄:** Unity **게임 오브젝트에 붙는** 스크립트. `Main.cs`가 로그인·콜백 처리의 **중심**.

---

### PlayerPrefs

**한 줄:** Unity가 PC에 **작은 설정값**을 저장하는 곳 (키-값).

**이 프로젝트에서:** `NaverLogin.FreshOAuthTestPending` — "다음 로그인 fresh 모드" 플래그.

---

### persistentDataPath / SessionTokenStore

**한 줄:** 게임이 **사용자 PC에 파일 저장**하는 경로.

**sessionToken**은 `{persistentDataPath}/auth/sessionToken.txt`에 저장됩니다. PlayerPrefs보다 **파일**로 관리.

---

### HttpListener

**한 줄:** Unity(클라이언트)가 **직접 작은 웹 서버**처럼 `127.0.0.1:7777`에서 **대기**하며, 브라우저 리다이렉트를 **받는** 기능.

**비유:** "로그인 끝나면 **7777번 방**으로 token 보내줘"라고 미리 정해 둔 **우편함**.

---

### Coroutine (코루틴)

**한 줄:** Unity에서 **시간을 나눠** 실행하는 함수 (`yield return`).

**예:** `TryAutoLogin()` — sessionToken으로 `/auth/me` 호출 후 결과 처리.

---

## 9. 테스트 도구 (Naver Test Tools)

### Full Reset

**한 줄:** 테스트를 **처음부터** 다시 하기 위한 **원클릭 초기화**.

**하는 일:** Chrome 쿠키 삭제 → sessionToken 삭제 → DB 삭제 + Naver revoke → Fresh Test ON → 브라우저 logout/disconnect 페이지.

자세한 내용: `docs/naver-test-tools-guide.md`

---

### Fresh Test / Fresh OAuth

**한 줄:** Full Reset 후 **다음 Play 로그인 한 번**을 "동의 화면 테스트" 모드로 켜는 **플래그**.

로그인 URL에 `?fresh=1` 붙음 → 서버에서 `auth_type=reprompt` 시도.

---

### Test API / ENABLE_TEST_ROUTES

**한 줄:** **개발용** `/test/status`, `/test/reset` 를 켜는 스위치.

`.env`에 `ENABLE_TEST_ROUTES=true`. **운영(실서비스)에서는 끄기.**

---

### ChromeDevCleaner / clear-chrome-browser-data.ps1

**한 줄:** Chrome **종료** 후 **기본 프로필** 쿠키·세션 스토리지 삭제 스크립트.

**주의:** **모든 사이트** 쿠키가 지워질 수 있음 (개발 PC 전용).

---

### BrowserLauncher

**한 줄:** URL을 **Chrome/Edge 시크릿**으로 여는 Unity 유틸.

---

### AuthLogger

**한 줄:** 클라이언트 **로그**를 `[AUTH]`, `[OAUTH]` 태그로 Unity Console에 출력.

---

## 10. 헷갈리기 쉬운 것 정리표

### 세션 vs 토큰 vs 쿠키

| | 한 줄 | 비유 | 이 프로젝트 |
|---|------|------|-------------|
| **쿠키** | 브라우저 **저장 메모** | 카페 스탬프 | Naver **브라우저** 로그인 상태 |
| **세션** | 로그인 **유지 상태** (넓은 의미) | 손목밴드 | "지금 로그인됨" 전체 개념 |
| **토큰** | 신분 **증명 문자열** | 입장권 | sessionToken, access_token 등 |

---

### sessionToken vs Naver access_token

| | sessionToken | Naver access_token |
|---|--------------|-------------------|
| **발급** | 우리 서버 | Naver |
| **저장** | Unity PC | 서버 DB |
| **용도** | 게임 자동 로그인 | Naver API·토큰 갱신 |
| **Unity가 알아야 하나?** | ✅ | ❌ |

---

### 쿠키 삭제 vs revoke vs sessionToken 삭제

| 동작 | 효과 |
|------|------|
| Chrome 쿠키 삭제 | 브라우저 **Naver 로그인** 풀릴 수 있음 (기본 프로필) |
| sessionToken 삭제 | Unity **자동 로그인**만 풀림 |
| DB DELETE | 서버 **사용자 기록** 삭제 |
| **revoke API** | Naver **앱 연동** 해제 → **동의 화면** 다시 가능 |

**동의 화면**을 보려면 보통 **revoke(연동 해제)** 가 필요합니다.

---

### 클라이언트 vs 서버 vs Naver

```
[Unity 게임]  ←sessionToken→  [우리 서버]  ←OAuth 토큰→  [Naver]
     ↑                              ↑
  로그인 버튼                   DB, revoke
  브라우저 OAuth
```

---

### GET vs POST (이 프로젝트)

| GET | POST |
|-----|------|
| `/auth/naver` — 브라우저 OAuth 시작 | `/auth/me` — 토큰 검증 |
| `/test/status` — 상태 조회 | `/auth/refresh` — 갱신 |
| | `/test/reset` — DB 초기화 |

---

### localhost:3000 vs localhost:7777

| 포트 | 누가 | 역할 |
|------|------|------|
| **3000** | Express **서버** | OAuth, API, DB |
| **7777** | Unity **HttpListener** | 로그인 후 token **받기** |

---

## 부록: 처음 읽을 때 추천 순서

1. [클라이언트와 서버](#2-클라이언트와-서버)
2. [토큰](#토큰-token) · [OAuth](#oauth--oauth-20)
3. [쿠키](#쿠키-cookie) · [세션](#세션-session)
4. [라우트](#라우트-route--라우터-router)
5. [Full Reset](#full-reset) · [revoke](#revoke-리보크--토큰-폐기)
6. [헷갈리기 쉬운 것 정리표](#10-헷갈리기-쉬운-것-정리표)

---

## 관련 문서

- [Naver Test Tools & Full Reset 가이드](./naver-test-tools-guide.md)
- [통합 테스트 체크리스트](./integration-test-checklist.md)
- [Naver OAuth 자동 로그인 아키텍처](./naver-auto-login-guide.html)

---

*이 설명집은 naver-login-project 코드 기준으로 작성되었습니다. 용어는 프로젝트마다 조금씩 다를 수 있습니다.*
