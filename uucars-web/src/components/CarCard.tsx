import { Link } from "react-router-dom";
import type { Car } from "@/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

interface CarCardProps {
  car: Car;
}

export default function CarCard({ car }: CarCardProps) {
  return (
    <Link to={`/cars/${car.id}`}>
      <Card className="cursor-pointer transition-shadow hover:shadow-md">
        <CardHeader className="pb-2">
          <CardTitle className="line-clamp-2 text-base">{car.title}</CardTitle>
          <Badge variant="secondary" className="w-fit">
            {car.brand}
          </Badge>
        </CardHeader>
        <CardContent className="space-y-1">
          <p className="text-2xl font-bold text-blue-600">
            ${car.price.toLocaleString()}
          </p>
          <p className="text-sm text-gray-500">
            {car.year} · {car.mileage.toLocaleString()} km
          </p>
        </CardContent>
      </Card>
    </Link>
  );
}
