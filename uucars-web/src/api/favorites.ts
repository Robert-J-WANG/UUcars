import apiClient from "./client";
import type { Favorite, PagedResponse, ApiResponse } from "@/types";

export const favoritesApi = {
  add: async (carId: number): Promise<Favorite> => {
    const response = await apiClient.post<ApiResponse<Favorite>>(
      `/favorites/${carId}`,
    );
    return response.data.data!;
  },

  remove: async (carId: number): Promise<void> => {
    await apiClient.delete(`/favorites/${carId}`);
  },

  // ✅ 新增：查询当前用户是否已收藏某辆车
  check: async (carId: number): Promise<boolean> => {
    const response = await apiClient.get<ApiResponse<boolean>>(
      `/favorites/${carId}`,
    );
    return response.data.data!;
  },

  getMyFavorites: async (
    page = 1,
    pageSize = 20,
  ): Promise<PagedResponse<Favorite>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Favorite>>>(
      "/favorites",
      { params: { page, pageSize } },
    );
    return response.data.data!;
  },
};
