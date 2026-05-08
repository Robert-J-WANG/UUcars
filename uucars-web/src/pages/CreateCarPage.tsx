import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { carsApi } from "@/api";
import CarForm from "@/components/CarForm";
import type { CarFormValues } from "@/components/CarForm";

export default function CreateCarPage() {
  const navigate = useNavigate();

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
      <CarForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel="Create Draft"
      />
    </div>
  );
}
