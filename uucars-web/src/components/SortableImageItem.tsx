import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

// 只依赖"展示 + 拖拽 + 删除"真正需要的字段，不绑定具体是
// 已持久化的 CarImage，还是本地文件生成的预览对象——
// CarImage 结构上天然满足这个形状，可以直接传入，不需要改 ImageUploader
export interface ImageLike {
  id: string | number;
  imageUrl: string;
}

interface SortableImageItemProps {
  image: ImageLike;
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
  // attributes/listeners 绑在整个卡片上（不再是独立手柄）：
  // 单纯点击（没有产生 8px 位移，见 ImageUploader 里的 activationConstraint）
  // 不会触发拖拽，配合删除按钮自己拦截事件（见下面），足以区分"点删除"和"开始拖拽"
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
      className="group relative cursor-move active:cursor-grabbing"
      {...attributes}
      {...listeners}
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

      {/* 悬浮遮罩：只在鼠标移入时显现，提示"这里可以拖动"；
          pointer-events-none 避免挡住下面删除按钮的点击 */}
      <div className="pointer-events-none absolute inset-0 rounded-lg bg-black/0 transition-colors group-hover:bg-black/20" />

      {/* 删除按钮：onPointerDown 阻止冒泡，保证按下这个按钮永远不会被
          外层的拖拽感知捕获，不管按下后有没有轻微位移 */}
      <button
        onClick={onDelete}
        onPointerDown={(e) => {
          e.stopPropagation();
        }}
        disabled={isDeleting}
        className="absolute -right-2 -top-2 flex h-5 w-5
                           items-center justify-center rounded-full
                           bg-red-500 text-xs text-white
                           hover:bg-red-600 cursor-pointer"
      >
        ×
      </button>
    </div>
  );
}

export default SortableImageItem;
