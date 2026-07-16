import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import CarCard from "@/components/CarCard";
import type { Car } from "@/types";

// 测试用的假车辆数据
// 只填测试需要验证的字段，其余用合理的默认值
const mockCar: Car = {
  id: 1,
  title: "2020 Toyota Corolla - Low Mileage",
  brand: "Toyota",
  model: "Corolla",
  year: 2020,
  price: 18000,
  mileage: 35000,
  status: "Published",
  sellerId: 10,
  sellerUsername: "seller1",
  createdAt: "2024-01-01T00:00:00Z",
  updatedAt: "2024-01-01T00:00:00Z",
};

const renderCard = (props?: Partial<Parameters<typeof CarCard>[0]>) =>
  render(
    <MemoryRouter>
      <CarCard car={mockCar} {...props} />
    </MemoryRouter>,
  );

describe("CarCard", () => {
  it("应该正确渲染车辆的品牌、价格和里程", () => {
    renderCard();

    // getByText：找到包含这段文字的元素
    // 用用户实际看到的文字来定位，而不是 class 名
    expect(screen.getByText("Toyota")).toBeInTheDocument();
    expect(screen.getByText("$18,000")).toBeInTheDocument();
    expect(screen.getByText("35,000 km")).toBeInTheDocument();
  });

  it("没有图片时应该显示占位图标", () => {
    renderCard();

    // getByRole("img") 找的是可访问树里 role 为 img 的元素
    // 它的 name 来自 alt 属性
    // 没有 coverImageUrl 时组件应该渲染占位 SVG 而不是 <img>
    // 用 queryByRole（找不到时返回 null，不报错）断言它不存在
    expect(
      screen.queryByRole("img", { name: /corolla/i }),
    ).not.toBeInTheDocument();
  });

  it("有图片时应该正确渲染图片", () => {
    const carWithImage = {
      ...mockCar,
      coverImageUrl: "https://example.com/car.jpg",
    };
    render(
      <MemoryRouter>
        <CarCard car={carWithImage} />
      </MemoryRouter>,
    );

    const img = screen.getByRole("img", { name: /corolla/i });
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute("src", "https://example.com/car.jpg");
  });

  it("有搜索词时应该高亮匹配的品牌文字", () => {
    renderCard({ highlightKeyword: "Toyota" });

    // highlight 函数会把 "Toyota" 包裹在 <mark> 标签里
    // <mark> 元素在 DOM 里仍然包含文字 "Toyota"，getByText 依然能找到它
    const searchTexts = screen.getAllByText("Toyota");
    expect(searchTexts.some((el) => el.tagName === "MARK")).toBe(true);
  });
});
