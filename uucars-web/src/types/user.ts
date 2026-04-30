// 对应后端 UserResponse
export interface User {
  id: number;
  username: string;
  email: string;
  role: "User" | "Admin"; // 联合类型：只能是这两个值之一
  createdAt: string;
}

// 对应后端 LoginResponse
export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

// 对应后端 RegisterRequest
export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

// 对应后端 LoginRequest
export interface LoginRequest {
  email: string;
  password: string;
}
