# Error Protocol

클라이언트와 서버가 공통으로 사용하는 간단한 에러 프로토콜입니다.

## Response Format

서버 에러 응답은 아래 JSON 형식을 사용합니다.

```json
{
  "code": "SERVER_ERROR",
  "message": "서버 내부 오류가 발생했습니다."
}
```

## Common Error Codes

- `AUTH_CONFIG_MISSING`
  - HTTP `500`
  - 서버의 OAuth 환경변수 설정이 비어 있음

- `SESSION_INVALID`
  - HTTP `401`
  - 토큰이 없거나 유효하지 않음

- `SESSION_EXPIRED`
  - HTTP `401`
  - 토큰 만료

- `USER_NOT_FOUND`
  - HTTP `404`
  - 토큰에 해당하는 사용자가 DB에 없음

- `REAUTH_REQUIRED`
  - HTTP `401`
  - 재로그인이 필요함

- `NAVER_TOKEN_REVOKED`
  - HTTP `401`
  - 네이버 refresh token이 폐기됨

- `INVALID_SCORE`
  - HTTP `400`
  - 점수 요청 값이 잘못됨

- `SERVER_ERROR`
  - HTTP `500`
  - 라우트 처리 중 런타임 예외 발생

## Client-side Local Errors

아래 코드는 서버 응답이 아니라 클라이언트가 자체 판단해서 표시합니다.

- `REQUEST_TIMEOUT`
  - 5초 안에 응답이 오지 않음

- `NETWORK_ERROR`
  - 서버가 죽어 있거나 연결 자체가 실패함

- `PARSE_ERROR`
  - 응답 JSON 형식이 기대와 다름
