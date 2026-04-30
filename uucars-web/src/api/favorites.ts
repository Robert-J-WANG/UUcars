import apiClient from "./client";
import type { Favorite, PagedResponse, ApiResponse } from "@/types";

export const favoritesApi = {
  add: async (carId: number): Promise<Favorite> => {
    const response = await apiClient.post<ApiResponse<Favorite>>(
      `/favorites/${carId}`
    );
    return response.data.data!;
  },

  remove: async (carId: number): Promise<void> => {
    await apiClient.delete(`/favorites/${carId}`);
  },

  getMyFavorites: async (
    page = 1,
    pageSize = 20
  ): Promise<PagedResponse<Favorite>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Favorite>>>(
      "/favorites",
      { params: { page, pageSize } }
    );
    return response.data.data!;
  },
};
