import apiClient from "./client";
import type {
  Car,
  CarDetail,
  CarImage,
  CarCreateRequest,
  CarUpdateRequest,
  CarQueryRequest,
  PagedResponse,
  ApiResponse,
} from "@/types";

export const carsApi = {
  // 公开列表（含搜索过滤）
  getPaged: async (params?: CarQueryRequest): Promise<PagedResponse<Car>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Car>>>(
      "/cars",
      { params }
    );
    return response.data.data!;
  },

  // 车辆详情
  getById: async (id: number): Promise<CarDetail> => {
    const response = await apiClient.get<ApiResponse<CarDetail>>(`/cars/${id}`);
    return response.data.data!;
  },

  // 我的车辆列表
  getMyListings: async (
    params?: CarQueryRequest
  ): Promise<PagedResponse<Car>> => {
    const response = await apiClient.get<ApiResponse<PagedResponse<Car>>>(
      "/cars/my-listings",
      { params }
    );
    return response.data.data!;
  },

  // 创建草稿
  create: async (data: CarCreateRequest): Promise<Car> => {
    const response = await apiClient.post<ApiResponse<Car>>("/cars", data);
    return response.data.data!;
  },

  // 修改草稿
  update: async (id: number, data: CarUpdateRequest): Promise<Car> => {
    const response = await apiClient.put<ApiResponse<Car>>(`/cars/${id}`, data);
    return response.data.data!;
  },

  // 逻辑删除
  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/cars/${id}`);
  },

  // 提交审核
  submit: async (id: number): Promise<Car> => {
    const response = await apiClient.post<ApiResponse<Car>>(
      `/cars/${id}/submit`
    );
    return response.data.data!;
  },

  // 上传图片（multipart/form-data）
  uploadImage: async (
    carId: number,
    file: File,
    sortOrder = 0
  ): Promise<CarImage> => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("sortOrder", sortOrder.toString());
    const response = await apiClient.post<ApiResponse<CarImage>>(
      `/cars/${carId}/images`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return response.data.data!;
  },

  // 删除图片
  deleteImage: async (carId: number, imageId: number): Promise<void> => {
    await apiClient.delete(`/cars/${carId}/images/${imageId}`);
  },
};
