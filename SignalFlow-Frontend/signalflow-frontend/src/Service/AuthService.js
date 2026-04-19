const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080'

const parseResponseBody = async (response) => {
  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    return response.json()
  }

  return response.text()
}

const normalizeAuthResponse = (data) => ({
  id: data?.id ?? data?.Id ?? null,
  email: data?.email ?? data?.Email ?? '',
  username: data?.username ?? data?.Username ?? '',
  token: data?.token ?? data?.Token ?? null,
  refreshTokenExpiryTime:
    data?.refreshTokenExpiryTime ?? data?.RefreshTokenExpiryTime ?? null,
  refreshToken: data?.refreshToken ?? data?.RefreshToken ?? null,
})

const register = async ({ username, email, password }) => {
  const response = await fetch(`${API_BASE_URL}/api/user/register`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, email, password }),
  })

  const body = await parseResponseBody(response)

  if (!response.ok) {
    const message =
      typeof body === 'string'
        ? body
        : body?.message ?? body?.title ?? 'Registration failed'

    throw new Error(message)
  }

  return normalizeAuthResponse(body)
}

const login = async ({ username, password }) => {
  const response = await fetch(`${API_BASE_URL}/api/user/login`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, password }),
  })

  const body = await parseResponseBody(response)

  if (!response.ok) {
    const message =
      typeof body === 'string'
        ? body
        : body?.message ?? body?.title ?? 'Login failed'

    throw new Error(message)
  }

  return normalizeAuthResponse(body)
}

export { login, register }

