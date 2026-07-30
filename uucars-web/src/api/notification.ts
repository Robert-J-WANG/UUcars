import type { Notification, ApiResponse } from "@/types";
import apiClient from "./client";

export const notificationApi = {
  // 历史未读通知
  getAll: async (): Promise<Notification[]> => {
    const response =
      await apiClient.get<ApiResponse<Notification[]>>("/notifications");
    return response.data.data!;
  },

  // 单条已读
  read: async (id: number): Promise<void> => {
    await apiClient.put(`/notifications/${id}/read`);
  },

  // 全部已读
  readAll: async (): Promise<void> => {
    await apiClient.put("/notifications/read-all");
  },
};
