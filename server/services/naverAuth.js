const NAVER_TOKEN_URL = 'https://nid.naver.com/oauth2.0/token';

async function refreshNaverToken(refreshToken) {
  const clientId = process.env.NAVER_CLIENT_ID;
  const clientSecret = process.env.NAVER_CLIENT_SECRET;

  if (!clientId || !clientSecret) {
    throw new Error('NAVER 환경변수가 설정되지 않았습니다.');
  }

  const params = new URLSearchParams({
    grant_type: 'refresh_token',
    client_id: clientId,
    client_secret: clientSecret,
    refresh_token: refreshToken,
  });

  const response = await fetch(`${NAVER_TOKEN_URL}?${params.toString()}`, {
    method: 'GET',
  });

  const data = await response.json();

  if (!response.ok || data.error) {
    const error = new Error(data.error_description || data.error || 'Naver token refresh failed');
    error.code = 'NAVER_TOKEN_REVOKED';
    throw error;
  }

  return {
    accessToken: data.access_token,
    refreshToken: data.refresh_token || refreshToken,
    expiresIn: Number(data.expires_in || 3600),
  };
}

module.exports = { refreshNaverToken };
