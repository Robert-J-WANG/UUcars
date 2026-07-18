// 把字符串里匹配 keyword 的部分用 <mark> 包裹
// 返回 React 节点数组（因为包含了 JSX 元素）
export function highlight(text: string, keyword: string): React.ReactNode {
  if (!keyword.trim()) return text;

  // 'gi' 标志：g = 全局匹配（不只匹配第一个），i = 不区分大小写
  const regex = new RegExp(`(${escapeRegex(keyword)})`, "gi");
  const parts = text.split(regex);

  return parts.map((part, i) =>
    // test 返回 true 说明这个 part 是匹配到的词
    regex.test(part) ? (
      <mark
        key={i}
        style={{
          backgroundColor: "var(--color-warning-light)",
          color: "var(--color-text-primary)",
        }}
      >
        {part}
      </mark>
    ) : (
      part
    ),
  );
}

// 对用户输入做转义，避免正则特殊字符（. * + ? 等）引发意外匹配
// 例如用户搜 "3.5"，不转义的话 "." 会匹配任意字符
function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
