// src/components/SellerBanner.tsx
import { Link } from "react-router-dom";
import { ArrowRight, BadgeCheck, Clock } from "lucide-react";
// 静态图片：放在 src/assets/seller-banner.jpg
// Vite 构建时会自动压缩并加上 hash（如 seller-banner.a3f2c1.jpg）
import sellerImg from "@/assets/seller-banner.jpg";

export default function SellerBanner() {
  return (
    <div
      className="overflow-hidden rounded-[var(--radius-xl)] flex flex-col sm:flex-row"
      style={{
        border: "1px solid var(--color-border)",
        backgroundColor: "var(--color-accent-light)",
      }}
    >
      {/* ── 左侧：图片 ──
          固定高度，图片用 object-cover 填满，不变形
          sm 以下隐藏图片，只显示文字（小屏空间有限）     */}
      <div className="hidden sm:block sm:w-[42%] shrink-0">
        <img
          src={sellerImg}
          alt="Sell your car"
          className="h-full w-full object-cover"
          // 高度撑满右侧内容区，由右侧 padding 决定
          style={{ minHeight: "200px" }}
        />
      </div>

      {/* ── 右侧：说明文字 + CTA ── */}
      <div className="flex flex-col justify-center gap-8 px-8 py-8">
        <div>
          <h2
            className="text-2xl font-bold mb-2"
            style={{
              color: "var(--color-text-primary)",
              fontFamily: "'DM Serif Display', serif",
            }}
          >
            Sell your car. Free, forever.
          </h2>
          <p
            className="text-sm leading-relaxed"
            style={{ color: "var(--color-text-secondary)" }}
          >
            List your car in minutes and reach thousands of buyers. No waiting,
            no fees, no expiry — your listing stays live until it sells.
          </p>
        </div>

        {/* 两个亮点 */}
        <div className="flex flex-wrap gap-10">
          {[
            { icon: BadgeCheck, text: "No success fees" },
            { icon: Clock, text: "Live until sold" },
          ].map(({ icon: Icon, text }) => (
            <span
              key={text}
              className="flex items-center gap-1.5 text-sm font-medium"
              style={{ color: "var(--color-text-secondary)" }}
            >
              <Icon
                className="h-4 w-4"
                style={{ color: "var(--color-success)" }}
              />
              {text}
            </span>
          ))}
        </div>

        {/* CTA 按钮 */}

        <div>
          <Link
            to="/cars/new"
            className="inline-flex items-center gap-2 rounded-full px-6 py-2.5 text-sm font-semibold transition-all duration-150 hover:gap-3 bg-[var(--color-accent)] hover:bg-[var(--color-accent-hover)]"
            style={{
              // backgroundColor: "var(--color-accent)",
              color: "var(--color-text-inverse)",
            }}
          >
            List my car now
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </div>
  );
}
