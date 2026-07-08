import { useRef } from "react";
import { toast } from "sonner";
import { Plus } from "lucide-react";
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
import type { LocalImage } from "@/pages/CreateCarPage";

const MAX_IMAGES = 10; // 与后端 AddImagesBatchAsync 的上限保持一致

interface ImagePickerProps {
  // 受控组件：状态由父组件持有，这里只负责展示和触发变化
  images: LocalImage[];
  onChange: (images: LocalImage[]) => void;
}

export default function ImagePicker({ images, onChange }: ImagePickerProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
  );
  const canAddMore = images.length < MAX_IMAGES;

  /* --------------- 选择 --------------- */
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    const fileArray = Array.from(files);

    // 数量上限：只在本地拦截，此时没有 carId，无法调后端校验
    if (images.length + fileArray.length > MAX_IMAGES) {
      toast.error(
        `A car can have at most ${MAX_IMAGES} images. ` +
          `Currently selected ${images.length}, ` +
          `you selected ${fileArray.length} more.`,
      );
      if (inputRef.current) inputRef.current.value = "";
      return;
    }

    const newImages: LocalImage[] = fileArray.map((file) => ({
      id: `local-${Date.now()}-${Math.random()}`,
      file,
      previewUrl: URL.createObjectURL(file),
    }));

    onChange([...images, ...newImages]);

    if (inputRef.current) inputRef.current.value = "";
  };
  /* --------------- 删除 --------------- */
  // 纯本地数组操作，没有对应的后端记录，不发请求
  const handleDelete = (id: string) => {
    const target = images.find((img) => img.id === id);
    if (target) URL.revokeObjectURL(target.previewUrl);
    onChange(images.filter((img) => img.id !== id));
  };

  /* --------------- 拖拽 --------------- */
  // 拖拽结束：纯本地重排，没有已持久化的 SortOrder 需要同步给后端
  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = images.findIndex((img) => img.id === active.id);
    const newIndex = images.findIndex((img) => img.id === over.id);

    onChange(arrayMove(images, oldIndex, newIndex));
  };

  return (
    <div className="space-y-2">
      <h2 className="font-semibold">Images</h2>
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={handleDragEnd}
      >
        <SortableContext
          items={images.map((img) => img.id)}
          strategy={rectSortingStrategy}
        >
          <div className="flex flex-wrap gap-3">
            {images.map((img) => (
              <SortableImageItem
                key={img.id}
                image={{ id: img.id, imageUrl: img.previewUrl }}
                onDelete={() => handleDelete(img.id)}
                isDeleting={false}
              />
            ))}

            {/* 添加图片：跟图片同尺寸的方块，始终排在最后一个 */}
            {canAddMore && (
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
        </SortableContext>
      </DndContext>

      {/* 隐藏的原生文件选择 input，multiple 允许一次选多个文件 */}
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        className="hidden"
        onChange={handleFileChange}
      />
      <p className="text-xs text-gray-500">
        JPEG, PNG or WebP · Max 5 MB per image · Up to {MAX_IMAGES} images total
      </p>
    </div>
  );
}
