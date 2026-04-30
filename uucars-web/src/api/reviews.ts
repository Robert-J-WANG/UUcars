import type {
  ApiResponse,
  CreateReviewRequest,
  Review,
  SellerRating,
} from "@/types";
import apiClient from "./client";

export const reviewsApi = {
  create: async (data: CreateReviewRequest): Promise<Review> => {
    const response = await apiClient.post<ApiResponse<Review>>(
      "/reviews",
      data
    );
    return response.data.data!;
  },

  getSellerRating: async (sellerId: number): Promise<SellerRating> => {
    const response = await apiClient.get<ApiResponse<SellerRating>>(
      `/reviews/seller/${sellerId}`
    );
    return response.data.data!;
  },
};
