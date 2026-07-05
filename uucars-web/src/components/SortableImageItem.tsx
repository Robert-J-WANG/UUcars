import type { CarImage } from "@/types";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical } from "lucide-react";

interface SortableImageItemProps {
  image: CarImage;
  onDelete: () => void;
  isDeleting: boolean;
}
/* -------- 已上传图片：可拖拽排序的单个图片项 ------- */
function SortableImageItem({
  image,
  onDelete,
  isDeleting,
}: SortableImageItemProps) {
  // useSortable 把这个元素注册为可排序项
  // attributes/listeners 只绑定到拖拽手柄，不绑定整个卡片
  // 原因：整个卡片都能拖拽时，"点删除"和"开始拖拽"会冲突，浏览器无法区分意图
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: image.id });

  return (
    <div
      ref={setNodeRef}
      className="relative"
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
      }}
    >
      <img
        src={image.imageUrl}
        alt="Car"
        className="h-24 w-24 rounded-lg object-cover"
      />

      {/* 删除按钮 */}
      <button
        onClick={onDelete}
        disabled={isDeleting}
        className="absolute -right-2 -top-2 flex h-5 w-5
                           items-center justify-center rounded-full
                           bg-red-500 text-xs text-white
                           hover:bg-red-600"
      >
        ×
      </button>

      {/* 拖拽手柄：独立于卡片，避免和删除按钮抢事件 */}
      <button
        {...attributes}
        {...listeners}
        type="button"
        aria-label="Drag to reorder"
        className="absolute bottom-1 right-1 flex h-5 w-5 cursor-grab
                   items-center justify-center rounded bg-black/50 text-white
                   active:cursor-grabbing"
      >
        <GripVertical className="h-3 w-3" />
      </button>
    </div>
  );
}

export default SortableImageItem;
