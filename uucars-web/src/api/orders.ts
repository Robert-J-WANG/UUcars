import apiClient from "./client";
import type {
  Order,
  OrderCreateRequest,
  PagedResponse,
  ApiResponse,
} from "@/types";

export const ordersApi = {
  create: async (data: OrderCreateRequest): Promise<Order> => {
    const response = await apiClient.post<ApiResponse<Order>>("/orders", data);
    return response.data.data!;
  },

  cancel: async (id: number): Promise<Order> => {
    const response = await apiClient.post<ApiResponse<Order>>(
      `/orders/${id}/cancel`
    );
    return response.data.data!;
  },

  complete: async (id: number): Promise<Order> => {
    const response = await apiClient.post<ApiResponse<Order>>(
      `/orders/${id}/complete`
    );
    return response.data.data!;
  },

  getMyPurchases: async (
    page = 1,
    pageSize = 20
  ): Promise<PagedResponse<Order>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Order>>>(
      "/orders/my-purchases",
      { params: { page, pageSize } }
    );
    return response.data.data!;
  },

  getMySales: async (
    page = 1,
    pageSize = 20
  ): Promise<PagedResponse<Order>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Order>>>(
      "/orders/my-sales",
      { params: { page, pageSize } }
    );
    return response.data.data!;
  },
};
