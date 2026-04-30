// 对应后端 ReviewResponse
export interface Review {
  id: number;
  orderId: number;
  reviewerId: number;
  reviewerUsername: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

// 对应后端 SellerRatingResponse
export interface SellerRating {
  sellerId: number;
  averageRating: number;
  totalReviews: number;
  reviews: Review[];
}

// 对应后端 CreateReviewRequest
export interface CreateReviewRequest {
  orderId: number;
  rating: number;
  comment?: string;
}
