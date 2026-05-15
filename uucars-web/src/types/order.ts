export const OrderStatus = {
  Pending: "Pending",
  Completed: "Completed",
  Cancelled: "Cancelled",
} as const;

export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

// 对应后端 OrderResponse
export interface Order {
  id: number;
  carId: number;
  carTitle: string;
  buyerId: number;
  buyerUsername: string;
  sellerId: number;
  sellerUsername: string;
  price: number;
  status: OrderStatus;
  createdAt: string;
  updatedAt: string;
}

// 对应后端 OrderCreateRequest
export interface OrderCreateRequest {
  carId: number;
}
