import { carsApi } from "@/api";
import type { CarImage } from "@/types";
import { QueryClient, useMutation } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "./ui/button";

interface ImageUploaderProps {
  carId: number;
  images: CarImage[];
}
export default function ImageUploader({ carId, images }: ImageUploaderProps) {
  // previewUrl：用户选择文件后的本地预览 URL
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  // selectedFile：用户选择的文件对象，上传时用
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const queryClient = new QueryClient();
  // useRef 拿到 input 元素的引用，点击按钮时触发文件选择
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // 释放上一个预览 URL（避免内存泄漏）
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }

    // 生成本地预览 URL
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    setSelectedFile(file);
  };

  /* ----------- 上传 mutation ---------- */
  const uploadMutation = useMutation({
    mutationFn: (file: File) => carsApi.uploadImage(carId, file),
    onSuccess: () => {
      toast.success("Image uploaded!");
      // 清除预览状态
      setPreviewUrl(null);
      setSelectedFile(null);
      // 让车辆详情缓存失效，图片列表会刷新
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  /* ----------- 删除 mutation ---------- */
  const deleteMutation = useMutation({
    mutationFn: (imageId: number) => carsApi.deleteImage(carId, imageId),
    onSuccess: () => {
      toast.success("Image deleted.");
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  return (
    <div className="space-y-4">
      <h2 className="font-semibold">Images</h2>

      {/* 已上传的图片列表 */}
      {images.length > 0 && (
        <div className="flex flex-wrap gap-3">
          {images.map((image) => (
            <div key={image.id} className="relative">
              <img
                src={image.imageUrl}
                alt="Car"
                className="h-24 w-24 rounded-lg object-cover"
              />
              {/* 删除按钮 */}
              <button
                onClick={() => deleteMutation.mutate(image.id)}
                disabled={deleteMutation.isPending}
                className="absolute -right-2 -top-2 flex h-5 w-5
                           items-center justify-center rounded-full
                           bg-red-500 text-xs text-white
                           hover:bg-red-600"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}

      {/* 选择新图片 */}
      <div className="space-y-3">
        {/* 隐藏的原生文件选择 input */}
        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="hidden"
          onChange={handleFileChange}
        />

        {/* 点击这个按钮触发文件选择 */}
        <Button
          type="button"
          variant="outline"
          onClick={() => inputRef.current?.click()}
        >
          Select Image
        </Button>

        {/* 选择后显示预览 */}
        {previewUrl && selectedFile && (
          <div className="space-y-2">
            <img
              src={previewUrl}
              alt="Preview"
              className="h-40 w-40 rounded-lg object-cover"
            />
            <p className="text-sm text-gray-500">{selectedFile.name}</p>

            {/* 点击按钮，实现上传 */}
            <Button
              type="button"
              size="sm"
              onClick={() => uploadMutation.mutate(selectedFile)}
              disabled={uploadMutation.isPending}
            >
              {uploadMutation.isPending ? "Uploading..." : "Upload"}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
