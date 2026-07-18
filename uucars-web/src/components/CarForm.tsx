import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Label } from "./ui/label";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import { useEffect, useRef } from "react";
import { toast } from "sonner";

const carSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required")
    .max(100, "Title must not exceed 100 characters"),
  brand: z
    .string()
    .min(1, "Brand is required")
    .max(50, "Brand must not exceed 50 characters"),
  model: z
    .string()
    .min(1, "Model is required")
    .max(50, "Model must not exceed 50 characters"),
  year: z
    .number({ error: "Year is required" })
    .int()
    .min(1900, "Year must be after 1900")
    .max(new Date().getFullYear() + 1, "Invalid year"),
  price: z
    .number({ error: "Price is required" })
    .positive("Price must be greater than 0"),
  mileage: z
    .number({ error: "Mileage is required" })
    .min(0, "Mileage cannot be negative"),
  description: z.string().max(2000).optional(),
});

export type CarFormValues = z.infer<typeof carSchema>;

interface CarFormProps {
  // defaultValues：编辑时传入已有数据，发布时不传（空表单）
  defaultValues?: CarFormValues;
  // onSubmit：父组件（页面）负责实际的 API 调用
  onSubmit: (values: CarFormValues) => Promise<void>;
  // isSubmitting：从父组件传入，控制按钮禁用状态
  isSubmitting: boolean;
  // submitLabel：按钮文字（"Create Draft" 或 "Save Changes"）
  submitLabel: string;
  // ✅ 新增：由调用方传入，格式如 "car-draft-new" 或 "car-draft-5"
  draftKey: string;
}

export default function CarForm({
  defaultValues,
  onSubmit,
  isSubmitting,
  submitLabel,
  draftKey,
}: CarFormProps) {
  const {
    register,
    handleSubmit,
    // 用来订阅表单所有字段的变化
    watch,
    // 用于把恢复的草稿数据重新填回表单
    reset,
    formState: { errors },
  } = useForm<CarFormValues>({
    resolver: zodResolver(carSchema),
    defaultValues,
  });

  /* -------------- 保存草稿 -------------- */
  // 内容变化时：2 秒 debounce 自动保存
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const subscription = watch((values) => {
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
      saveTimerRef.current = setTimeout(() => {
        localStorage.setItem(draftKey, JSON.stringify(values));
      }, 2000);
    });
    return () => {
      subscription.unsubscribe();
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    };
  }, [watch, draftKey]);

  /* ------------- 草稿数据回填 ------------- */
  // 进入页面时：检查是否有未保存的草稿
  const draftRestoredRef = useRef(false);

  useEffect(() => {
    if (draftRestoredRef.current) return; // 只在初次渲染时执行一次
    draftRestoredRef.current = true;

    const saved = localStorage.getItem(draftKey);
    if (!saved) return;

    try {
      const parsed = JSON.parse(saved) as CarFormValues;
      // setTimeout(300ms)：等 Toaster 完成挂载再调用 toast()，
      // 否则组件刚挂载时 Toaster 可能还没准备好，Toast 不会显示
      setTimeout(() => {
        toast("Unsaved draft found.", {
          description: "Do you want to restore your previous draft?",
          action: {
            label: "Restore",
            onClick: () => {
              reset(parsed);
              toast.success("Draft restored.");
            },
          },
          cancel: {
            label: "Discard",
            onClick: () => localStorage.removeItem(draftKey),
          },
          duration: 10000, // 给用户足够时间决定
        });
      }, 300);
    } catch {
      // JSON 解析失败（数据损坏），静默清除
      localStorage.removeItem(draftKey);
    }
  }, [draftKey, reset]);

  /* ------------- 提交处理函数 ------------- */
  // 封装了清除草稿的逻辑
  const handleFormSubmit = async (values: CarFormValues) => {
    //提交表单数据
    await onSubmit(values);
    // 手动清除草稿
    localStorage.removeItem(draftKey);
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="title">Title</Label>
        <Input
          id="title"
          placeholder="e.g. 2020 BMW 3 Series - Low Mileage"
          {...register("title")}
        />
        {errors.title && (
          <p className="text-sm text-red-500">{errors.title.message}</p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="brand">Brand</Label>
          <Input id="brand" placeholder="e.g. BMW" {...register("brand")} />
          {errors.brand && (
            <p className="text-sm text-red-500">{errors.brand.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="model">Model</Label>
          <Input
            id="model"
            placeholder="e.g. 3 Series"
            {...register("model")}
          />
          {errors.model && (
            <p className="text-sm text-red-500">{errors.model.message}</p>
          )}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="space-y-2">
          <Label htmlFor="year">Year</Label>
          <Input
            id="year"
            type="number"
            placeholder="2020"
            // valueAsNumber：告诉 RHF 把这个字段的值转成数字
            // 不加这个，RHF 收到的是字符串，Zod 的 z.number() 会报错
            {...register("year", { valueAsNumber: true })}
          />
          {errors.year && (
            <p className="text-sm text-red-500">{errors.year.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="price">Price ($)</Label>
          <Input
            id="price"
            type="number"
            placeholder="25000"
            {...register("price", { valueAsNumber: true })}
          />
          {errors.price && (
            <p className="text-sm text-red-500">{errors.price.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="mileage">Mileage (km)</Label>
          <Input
            id="mileage"
            type="number"
            placeholder="50000"
            {...register("mileage", { valueAsNumber: true })}
          />
          {errors.mileage && (
            <p className="text-sm text-red-500">{errors.mileage.message}</p>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description (optional)</Label>
        <textarea
          id="description"
          rows={4}
          placeholder="Describe the car's condition, features, history..."
          className="w-full rounded-md border px-3 py-2 text-sm
                     focus:outline-none focus:ring-2 focus:ring-blue-500"
          {...register("description")}
        />
        {errors.description && (
          <p className="text-sm text-red-500">{errors.description.message}</p>
        )}
      </div>

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Saving..." : submitLabel}
      </Button>
    </form>
  );
}
