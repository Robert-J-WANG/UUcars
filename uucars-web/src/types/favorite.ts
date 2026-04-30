import type { Car } from "./car";

// 对应后端 FavoriteResponse
export interface Favorite {
  carId: number;
  userId: number;
  createdAt: string;
  car: Car | null;
}
