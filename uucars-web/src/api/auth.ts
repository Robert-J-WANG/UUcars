import apiClient from "./client";
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  User,
  ApiResponse,
} from "@/types";

export const authApi = {
  register: async (data: RegisterRequest): Promise<User> => {
    const response = await apiClient.post<ApiResponse<User>>(
      "/auth/register",
      data
    );
    return response.data.data!;
  },

  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<ApiResponse<LoginResponse>>(
      "/auth/login",
      data
    );
    return response.data.data!;
  },

  verifyEmail: async (token: string): Promise<void> => {
    await apiClient.get("/auth/verify-email", { params: { token } });
  },

  resendVerification: async (email: string): Promise<void> => {
    await apiClient.post("/auth/resend-verification", { email });
  },

  forgotPassword: async (email: string): Promise<void> => {
    await apiClient.post("/auth/forgot-password", { email });
  },

  resetPassword: async (token: string, newPassword: string): Promise<void> => {
    await apiClient.post("/auth/reset-password", { token, newPassword });
  },
};
