import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { carsApi } from "@/api";
import CarForm from "@/components/CarForm";
import type { CarFormValues } from "@/components/CarForm";
import React, { useRef, useState } from "react";
import { Plus, X } from "lucide-react";

const MAX_IMAGES = 10; // 与后端 AddImagesBatchAsync 的上限保持一致

// 创建阶段本地暂存的图片：车辆还不存在，不能真正上传，
// 只在本地生成预览，等提交成功拿到 carId 才批量上传
interface LocalImage {
  id: string; // 临时 id，用于 React key 和移除定位
  file: File;
  previewUrl: string;
}

export default function CreateCarPage() {
  const navigate = useNavigate();

  const inputRef = useRef<HTMLInputElement>(null);

  // 本地暂存的图片（车辆还不存在，不能真正上传）
  const [localImages, setLocalImages] = useState<LocalImage[]>([]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // 获取选择的文件
    const files = e.target.files;
    // 没有选上或者是空文件
    if (!files || files.length === 0) return;

    /* -------------- 选到了文件 ------------- */
    // 转换成真数组， 便于使用map方法
    const fileArray = Array.from(files);
    console.log(fileArray);
  };

  const handleRemoveLocalImage = (imgId: string) => {
    console.log(imgId);
  };

  const createMutation = useMutation({
    mutationFn: (values: CarFormValues) => carsApi.create(values),
    onSuccess: (car) => {
      toast.success("Draft created!");
      // 创建成功后跳转到编辑页，继续上传图片
      navigate(`/cars/${car.id}/edit`);
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  const handleSubmit = async (values: CarFormValues) => {
    createMutation.mutate(values);
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">List Your Car</h1>
      {/* 图片选择：放在表单前面，选中即预览，提交时才真正上传 */}
      <div className="space-y-2">
        <h2 className="font-semibold">Images</h2>
        <p className="text-xs text-gray-500">
          Selected images will upload after you create the draft.
        </p>

        <div className="flex flex-wrap gap-3">
          {localImages.map((img) => (
            <div key={img.id} className="group relative">
              <img
                src={img.previewUrl}
                alt="Preview"
                className="h-24 w-24 rounded-lg object-cover"
              />
              <div className="pointer-events-none absolute inset-0 rounded-lg bg-black/0 transition-colors group-hover:bg-black/20" />
              <button
                type="button"
                onClick={() => handleRemoveLocalImage(img.id)}
                className="absolute -right-2 -top-2 flex h-5 w-5
                         items-center justify-center rounded-full
                         bg-red-500 text-xs text-white
                         hover:bg-red-600 cursor-pointer"
              >
                <X className="h-3 w-3" />
              </button>
            </div>
          ))}

          {localImages.length < MAX_IMAGES && (
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              className="flex h-24 w-24 flex-col items-center justify-center gap-1
                       rounded-lg border-2 border-dashed border-gray-300
                       text-gray-400 transition-colors
                       hover:border-gray-400 hover:text-gray-500"
            >
              <Plus className="h-5 w-5" />
              <span className="text-[10px]">Add</span>
            </button>
          )}
        </div>

        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          className="hidden"
          onChange={handleFileChange}
        />
      </div>
      <CarForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel="Create Draft"
      />
    </div>
  );
}
