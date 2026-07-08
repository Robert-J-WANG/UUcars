import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { carsApi } from "@/api";
import CarForm from "@/components/CarForm";
import type { CarFormValues } from "@/components/CarForm";
import { useState } from "react";
import ImagePicker from "@/components/ImagePicker";

// 车辆创建前本地暂存的图片：还没有 carId，不会真正上传，
// 只在本地生成预览、支持删除和拖拽排序；真正的上传由父组件
// 在拿到 carId 之后调用 carsApi.uploadImagesBatch 完成
export interface LocalImage {
  id: string;
  file: File;
  previewUrl: string;
}

export default function CreateCarPage() {
  const navigate = useNavigate();

  // 本地暂存的图片：车辆还不存在，选择、删除、排序都只发生在本地，
  // 全部逻辑交给 ImageSelector，这里只持有状态，提交时读出来用
  const [localImages, setLocalImages] = useState<LocalImage[]>([]);
  // 提交状态：涵盖"创建车辆 + 上传图片"整个过程，不只是创建这一步
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (values: CarFormValues) => {
    setIsSubmitting(true);
    try {
      // 第一步：创建车辆，拿到真实的 carId
      const car = await carsApi.create(values);
      toast.success("Draft created!");

      // 第二步：如果用户选过图片，用刚拿到的 carId 批量上传
      if (localImages.length > 0) {
        try {
          await carsApi.uploadImagesBatch(
            car.id,
            localImages.map((img) => img.file),
          );
        } catch {
          // 图片上传失败不影响车辆已创建这个事实，只提示用户去编辑页补传
          toast.error(
            "Draft created, but images failed to upload. You can add them on the next page.",
          );
        }
      }

      // 第三步：跳转编辑页（车辆一定已存在，图片传没传成功都可以在这里补）
      navigate(`/cars/${car.id}/edit`);
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Failed to create draft.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">List Your Car</h1>

      <ImagePicker images={localImages} onChange={setLocalImages} />

      <CarForm
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        submitLabel="Create Draft"
      />
    </div>
  );
}
