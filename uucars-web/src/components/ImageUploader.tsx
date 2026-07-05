import { carsApi } from "@/api";
import type { CarImage } from "@/types";
import { QueryClient, useMutation } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "./ui/button";
import { RotateCcw, AlertCircle } from "lucide-react";
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  rectSortingStrategy,
  arrayMove,
} from "@dnd-kit/sortable";
import SortableImageItem from "./SortableImageItem";

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
  // 已上传的图片顺序（本地副本，拖拽时先在本地更新，再异步调接口）
  const [localImages, setLocalImages] = useState(images);

  // images prop 变化时（比如上传成功后 invalidateQueries 触发重新拉取），
  // 同步更新本地已上传图片列表
  if (images !== localImages && images.length !== localImages.length) {
    setLocalImages(images);
  }

  // pendingFiles：本次选中、还没成功上传完的文件列表
  // 每一项都有自己的 status，互不影响
  const [pendingFiles, setPendingFiles] = useState<PendingFile[]>([]);

  const queryClient = new QueryClient();
  // useRef 拿到 input 元素的引用，点击按钮时触发文件选择
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    // FileList 转成真正的数组，才能用 map 之类的数组方法
    const fileArray = Array.from(files);

    // 数量上限：已有图片数 + 正在处理的数 + 这次新选的数，超过上限就拒绝，不发任何请求
    const totalAfter =
      localImages.length + pendingFiles.length + fileArray.length;
    if (totalAfter > MAX_IMAGES) {
      toast.error(
        `A car can have at most ${MAX_IMAGES} images. ` +
          `Currently has ${localImages.length + pendingFiles.length}, ` +
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

  /* ----------- 排序 mutation ---------- */
  const reorderMutation = useMutation({
    mutationFn: (items: { imageId: number; sortOrder: number }[]) =>
      carsApi.reorderImages(carId, items),
    onError: (error) => {
      // 排序失败：提示用户，并让 invalidateQueries 重新拉取后端真实顺序
      // （后端没写入成功，拉回来的就是拖拽之前的顺序，界面自动"弹回"）
      toast.error(error.message);
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
    },
  });

  /* ----------- 拖拽传感器 ---------- */
  // PointerSensor 同时支持鼠标和触摸操作
  // activationConstraint：8px 拖动阈值，避免普通点击（比如点删除按钮）被误判成拖拽
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
  );

  /* ----------- 拖拽结束：算出新顺序，本地立刻更新，异步提交后端 ---------- */
  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = localImages.findIndex((img) => img.id === active.id);
    const newIndex = localImages.findIndex((img) => img.id === over.id);

    // 立即更新本地顺序（视觉立即响应，不等接口返回）
    const reordered = arrayMove(localImages, oldIndex, newIndex);
    setLocalImages(reordered);

    // 按新顺序生成 SortOrder，异步提交后端
    const items = reordered.map((img, index) => ({
      imageId: img.id,
      sortOrder: index,
    }));
    reorderMutation.mutate(items);
  };

  return (
    <div className="space-y-4">
      <h2 className="font-semibold">Images</h2>

      {/* 已上传的图片列表：可拖拽排序 */}
      {localImages.length > 0 && (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <SortableContext
            items={localImages.map((img) => img.id)}
            strategy={rectSortingStrategy}
          >
            <div className="flex flex-wrap gap-3">
              {localImages.map((image) => (
                <SortableImageItem
                  key={image.id}
                  image={image}
                  onDelete={() => deleteMutation.mutate(image.id)}
                  isDeleting={
                    deleteMutation.isPending &&
                    deleteMutation.variables === image.id
                  }
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
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
