// src/components/HeroBanner.tsx
// 职责：品牌定位 + 搜索框 + 热门品牌标签
// 背景：用户提供的图片 + 主题色蒙层压在上面，文字在最顶层

import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Search, X } from "lucide-react";
import { Button } from "@/components/ui/button";
// 背景图：放在 src/assets/hero-bg.jpg
// 替换成你自己的图片文件名即可，支持 .jpg / .png / .webp
import heroBg from "@/assets/hero-bg.jpg";

const POPULAR_BRANDS = [
  "Toyota",
  "Honda",
  "BMW",
  "Mercedes",
  "Mazda",
  "Nissan",
  "Ford",
  "Subaru",
];

export default function HeroBanner() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [input, setInput] = useState("");

  const currentBrand = searchParams.get("brand") ?? "";

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim()) return;
    const current = Object.fromEntries(searchParams.entries());
    delete current["page"];
    setSearchParams({ ...current, brand: input.trim() });
    setInput("");
  };

  const handleBrandTag = (brand: string) => {
    const current = Object.fromEntries(searchParams.entries());
    delete current["page"];
    if (current["brand"] === brand) {
      delete current["brand"];
    } else {
      current["brand"] = brand;
    }
    setSearchParams(current);
  };

  return (
    <div className="relative -mx-4 overflow-hidden sm:mx-0 sm:rounded-2xl ">
      {/* ── 第一层：背景图片 ── */}
      <img
        src={heroBg}
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover"
        // object-position 控制图片焦点，根据图片内容调整
        // 比如人物在右侧就用 "right center"
        style={{ objectPosition: "center center" }}
      />

      {/* ── 第二层：蒙层 ──
          用原来的渐变背景色作为蒙层，opacity 控制透明度
          opacity 越高图片越暗/越被遮住；越低图片越清晰
          这里设 0.72，让图片隐约可见但文字对比度仍然足够  */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(135deg, var(--color-primary) 0%,  var(--color-primary-light) 100%)",
          opacity: 0.5,
        }}
      />

      {/* ── 第四层：内容（文字 / 搜索框 / 标签） ── */}
      <div className="relative px-4 py-14 text-[var(--color-text-inverse)]">
        <div className="mx-auto max-w-2xl text-center">
          {/* 小标签 */}
          <div
            className="mb-4 inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium"
            style={{
              borderColor: "rgba(255,255,255,0.25)",
              backgroundColor: "rgba(255,255,255,0.12)",
              color: "rgba(255,255,255,0.9)",
            }}
          >
            <span>↗</span>
            New Zealand's trusted car marketplace
          </div>

          {/* 主标题 */}
          <h1
            className="mb-3 text-3xl leading-tight sm:text-4xl"
            style={{
              fontFamily: "'DM Serif Display', serif",
              letterSpacing: "-0.02em",
            }}
          >
            Find Your Perfect Car
            <br className="hidden sm:block" />
            <em
              className="not-italic "
              style={{ color: "var(--color-accent-hover)" }}
            >
              in New Zealand
            </em>
          </h1>

          <p
            className="mb-8 text-base sm:text-lg"
            style={{ color: "rgba(255,255,255,0.85)" }}
          >
            Browse quality pre-owned vehicles from trusted sellers across NZ.
            Simple, secure, and transparent.
          </p>

          {/* 搜索框 */}
          <form onSubmit={handleSearch} className="mx-auto max-w-xl">
            <div
              className="flex overflow-hidden rounded-[var(--radius-xl)] p-1.5"
              style={{
                backgroundColor: "rgba(255,255,255,0.95)",
                boxShadow: "0 8px 32px rgba(15, 76, 117, 0.35)",
              }}
            >
              <div className="flex flex-1 items-center gap-2 px-3">
                <Search
                  className="h-4 w-4 shrink-0"
                  style={{ color: "var(--color-text-muted)" }}
                />
                <input
                  type="text"
                  placeholder="Search by brand, model..."
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  className="flex-1 bg-transparent text-sm outline-none placeholder:opacity-60"
                  style={{ color: "var(--color-text-primary)" }}
                />
                {input && (
                  <button
                    type="button"
                    onClick={() => setInput("")}
                    className="shrink-0 opacity-40 hover:opacity-70 transition-opacity"
                  >
                    <X
                      className="h-3.5 w-3.5"
                      style={{ color: "var(--color-text-muted)" }}
                    />
                  </button>
                )}
              </div>
              <Button
                type="submit"
                size="sm"
                className="shrink-0 rounded-[var(--radius-lg)]"
              >
                Search
              </Button>
            </div>
          </form>

          {/* 热门品牌标签 */}
          <div className="mt-5 flex flex-wrap justify-center gap-2">
            {POPULAR_BRANDS.map((brand) => {
              const isActive = currentBrand === brand;
              return (
                <button
                  key={brand}
                  onClick={() => handleBrandTag(brand)}
                  className="rounded-full px-3 py-1 text-xs font-medium transition-all duration-150 hover:scale-105"
                  style={{
                    backgroundColor: isActive
                      ? "rgba(255,255,255,0.92)"
                      : "rgba(255,255,255,0.15)",
                    color: isActive
                      ? "var(--color-primary)"
                      : "rgba(255,255,255,0.9)",
                    border: "1px solid rgba(255,255,255,0.2)",
                    fontWeight: isActive ? 600 : 500,
                  }}
                >
                  {brand}
                </button>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
