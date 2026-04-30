// 车辆状态枚举——对应后端 CarStatus
// 用 const 对象而不是 enum，原因：
// TypeScript 的 enum 编译后会生成额外的 JS 代码
// const 对象 + 类型推导更轻量，是目前的主流做法
export const CarStatus = {
  Draft: "Draft",
  PendingReview: "PendingReview",
  Published: "Published",
  Sold: "Sold",
  Deleted: "Deleted",
} as const;

// 从 const 对象推导出联合类型：'Draft' | 'PendingReview' | 'Published' | 'Sold' | 'Deleted'
export type CarStatus = (typeof CarStatus)[keyof typeof CarStatus];

// 对应后端 CarResponse
export interface Car {
  id: number;
  title: string;
  brand: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  description: string | null;
  status: CarStatus;
  sellerId: number;
  sellerUsername: string;
  createdAt: string;
  updatedAt: string;
}

// 对应后端 CarDetailResponse（含图片列表）
export interface CarDetail extends Car {
  images: CarImage[];
}

// 对应后端 CarImageResponse
export interface CarImage {
  id: number;
  imageUrl: string;
  sortOrder: number;
  carId: number;
}

// 对应后端 CarCreateRequest
export interface CarCreateRequest {
  title: string;
  brand: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  description?: string; // ? 表示可选字段，对应后端的 nullable
}

// 对应后端 CarUpdateRequest（和 Create 字段一样，单独定义保持灵活性）
// export interface CarUpdateRequest extends CarCreateRequest {}
export type CarUpdateRequest = CarCreateRequest; // 直接使用类型别名，简化代码

// 对应后端 CarQueryRequest
export interface CarQueryRequest {
  page?: number;
  pageSize?: number;
  brand?: string;
  minPrice?: number;
  maxPrice?: number;
  minYear?: number;
  maxYear?: number;
}
