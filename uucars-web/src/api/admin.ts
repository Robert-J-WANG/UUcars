import apiClient from "./client";
import type { Car, PagedResponse, ApiResponse } from "@/types";

export const adminApi = {
  getPendingCars: async (
    page = 1,
    pageSize = 20
  ): Promise<PagedResponse<Car>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Car>>>(
      "/admin/cars/pending",
      { params: { page, pageSize } }
    );
    return response.data.data!;
  },

  approve: async (carId: number): Promise<Car> => {
    const response = await apiClient.post<ApiResponse<Car>>(
      `/admin/cars/${carId}/approve`
    );
    return response.data.data!;
  },

  reject: async (carId: number): Promise<Car> => {
    const response = await apiClient.post<ApiResponse<Car>>(
      `/admin/cars/${carId}/reject`
    );
    return response.data.data!;
  },

  deleteCar: async (carId: number): Promise<void> => {
    await apiClient.delete(`/admin/cars/${carId}`);
  },
};
