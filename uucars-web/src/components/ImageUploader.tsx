import { carsApi } from "@/api";
import type { CarImage } from "@/types";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "./ui/button";
import { RotateCcw, AlertCircle } from "lucide-react";

interface ImageUploaderProps {
  carId: number;
  images: CarImage[];
}

const MAX_IMAGES = 10;

// 每个待上传文件自己的状态
// 一个文件对应数组里的一项，互相独立，不会互相影响
interface PendingFile {
  id: string; // 临时 id，用来在数组里定位到这一项，React key 也用它
  file: File;
  previewUrl: string;
  status: "uploading" | "error";
  error?: string;
}

export default function ImageUploader({ carId, images }: ImageUploaderProps) {
  // pendingFiles：本次选中、还没成功上传完的文件列表
  // 每一项都有自己的 status，互不影响
  const [pendingFiles, setPendingFiles] = useState<PendingFile[]>([]);

  const queryClient = useQueryClient();
  // useRef 拿到 input 元素的引用，点击按钮时触发文件选择
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    // FileList 转成真正的数组，才能用 map 之类的数组方法
    const fileArray = Array.from(files);

    // 数量上限：已有图片数 + 正在处理的数 + 这次新选的数，超过上限就拒绝，不发任何请求
    const totalAfter = images.length + pendingFiles.length + fileArray.length;
    if (totalAfter > MAX_IMAGES) {
      toast.error(
        `A car can have at most ${MAX_IMAGES} images. ` +
          `Currently has ${images.length + pendingFiles.length}, ` +
          `you selected ${fileArray.length}.`,
      );
      if (inputRef.current) inputRef.current.value = "";
      return;
    }

    // 每个文件包装成一个 PendingFile：各自生成本地预览、状态先标记为 uploading
    const newPendingFiles: PendingFile[] = fileArray.map((file) => ({
      id: `pending-${Date.now()}-${Math.random()}`,
      file,
      previewUrl: URL.createObjectURL(file),
      status: "uploading",
    }));

    setPendingFiles((prev) => [...prev, ...newPendingFiles]);

    // 选完立即各自开始上传，不需要再手动点确认
    newPendingFiles.forEach((p) => uploadSingleFile(p.id, p.file));

    if (inputRef.current) inputRef.current.value = "";
  };

  /* ----------- 上传单个文件（普通函数，不用 useMutation/useCallback） ---------- */
  // 每次调用只负责这一个文件，成功了把它从 pendingFiles 里移除，
  // 失败了只把这一项标记成 error，不影响列表里其他文件
  const uploadSingleFile = async (pendingId: string, file: File) => {
    try {
      await carsApi.uploadImagesBatch(carId, [file]);

      // 上传成功：从 pendingFiles 里移除这一项，并释放它的预览 URL（避免内存泄漏）
      setPendingFiles((prev) => {
        const target = prev.find((p) => p.id === pendingId);
        if (target) URL.revokeObjectURL(target.previewUrl);
        return prev.filter((p) => p.id !== pendingId);
      });

      // 让车辆详情缓存失效，图片列表会刷新
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
    } catch (error) {
      // 上传失败：只把这一项标记为 error，其他文件的状态不受影响
      const message = error instanceof Error ? error.message : "Upload failed";
      setPendingFiles((prev) =>
        prev.map((p) =>
          p.id === pendingId ? { ...p, status: "error", error: message } : p,
        ),
      );
    }
  };

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

      {/* 待上传文件：每一项独立显示上传中或失败重试，互不影响 */}
      {pendingFiles.length > 0 && (
        <div className="flex flex-wrap gap-3">
          {pendingFiles.map((pending) => (
            <div key={pending.id} className="relative">
              <img
                src={pending.previewUrl}
                alt="Preview"
                className="h-24 w-24 rounded-lg object-cover"
              />

              {/* 上传中：遮罩提示 */}
              {pending.status === "uploading" && (
                <div className="absolute inset-0 flex items-center justify-center rounded-lg bg-black/40">
                  <span className="text-xs font-medium text-white">
                    Uploading...
                  </span>
                </div>
              )}

              {/* 上传失败：这一项单独显示重试按钮，不影响其他项 */}
              {pending.status === "error" && (
                <div className="absolute inset-0 flex flex-col items-center justify-center gap-1 rounded-lg bg-black/60">
                  <AlertCircle className="h-4 w-4 text-red-400" />
                  <button
                    type="button"
                    onClick={() => uploadSingleFile(pending.id, pending.file)}
                    className="flex items-center gap-0.5 text-[10px] font-medium text-white hover:text-red-300"
                  >
                    <RotateCcw className="h-2.5 w-2.5" />
                    Retry
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* 选择新图片 */}
      <div className="space-y-3">
        {/* 隐藏的原生文件选择 input，multiple 允许一次选多个文件 */}
        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          className="hidden"
          onChange={handleFileChange}
        />

        {/* 点击这个按钮触发文件选择 */}
        <Button
          type="button"
          variant="outline"
          onClick={() => inputRef.current?.click()}
        >
          Add Images
        </Button>

        <p className="text-xs text-gray-500">
          JPEG, PNG or WebP · Max 5 MB per image · Up to {MAX_IMAGES} images
          total
        </p>
      </div>
    </div>
  );
}
