export interface UserResponse {
  id: string
  username: string
  email: string
  profilePictureUrl?: string | null
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  user: UserResponse
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}


export interface ApiError {
  type?: string
  title?: string
  status?: number
  errors?: Record<string, string[]>
  detail?: string
}
