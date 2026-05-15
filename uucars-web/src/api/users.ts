import apiClient from "./client";
import type { User, ApiResponse } from "@/types";

export const usersApi = {
  getMe: async (): Promise<User> => {
    const response = await apiClient.get<ApiResponse<User>>("/users/me");
    return response.data.data!;
  },

  updateMe: async (data: {
    username: string;
    email: string;
  }): Promise<User> => {
    const response = await apiClient.put<ApiResponse<User>>("/users/me", data);
    return response.data.data!;
  },
};
