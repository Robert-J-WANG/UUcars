import { carsApi } from "@/api";
import type { CarFormValues } from "@/components/CarForm";
import CarForm from "@/components/CarForm";
import ImageUploader from "@/components/ImageUploader";
import { Button } from "@/components/ui/button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";

export default function EditCarPage() {
  const { id } = useParams();
  const carId = Number(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  /* ---------- 请求车辆数据-用于表单回填 --------- */
  const { data: car, isLoading } = useQuery({
    queryKey: ["car", carId],
    queryFn: () => carsApi.getById(carId),
    enabled: !isNaN(carId),
  });

  /* ----------- 更新 mutation ---------- */
  const updateMutation = useMutation({
    mutationFn: (values: CarFormValues) => carsApi.update(carId, values),
    onSuccess: () => {
      toast.success("Changes saved!");
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  const handleSubmit = async (values: CarFormValues) => {
    updateMutation.mutate(values);
  };

  /* ---------- 提交审核 mutation --------- */
  const submitMutation = useMutation({
    mutationFn: () => carsApi.submit(carId),
    onSuccess: () => {
      toast.success("Submitted for review!");
      navigate("/profile/listings");
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  if (isLoading) return <div className="p-8">Loading...</div>;
  if (!car) return <div className="p-8">Car not found.</div>;

  // 不是草稿状态，不允许编辑
  if (car.status !== "Draft") {
    return (
      <div className="mx-auto max-w-2xl p-8 text-center">
        <p className="text-gray-500">
          This car cannot be edited in its current status ({car.status}).
        </p>
        <Button
          variant="outline"
          className="mt-4"
          onClick={() => navigate("/profile/listings")}
        >
          Back to My Listings
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Edit Car</h1>
        {/* 提交审核按钮 */}
        <Button
          onClick={() => submitMutation.mutate()}
          disabled={submitMutation.isPending}
        >
          {submitMutation.isPending ? "Submitting..." : "Submit for Review"}
        </Button>
      </div>

      {/* 车辆信息表单，defaultValues 填入已有数据 */}
      <CarForm
        defaultValues={{
          title: car.title,
          brand: car.brand,
          model: car.model,
          year: car.year,
          price: car.price,
          mileage: car.mileage,
          description: car.description ?? "",
        }}
        onSubmit={handleSubmit}
        isSubmitting={updateMutation.isPending}
        submitLabel="Save Changes"
      />

      {/* 图片上传 */}
      <ImageUploader carId={carId} images={car.images} />
    </div>
  );
}
