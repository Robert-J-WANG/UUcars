import { useState, useEffect } from "react";

// 泛型 T：让这个 Hook 能处理任意类型的值
// 不只是字符串，数字、对象都可以防抖
function useDebounce<T>(value: T, delay: number): T {
  // debouncedValue：防抖后的值，这才是真正用来发请求的值
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    // value 变化时，设置一个定时器
    // delay 毫秒后，把 debouncedValue 更新为最新的 value
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    // useEffect 的清理函数：在下次 effect 执行前调用
    // 如果 value 在 delay 内又变化了，先清除上一个定时器
    // 这就实现了"重置计时器"的效果
    return () => {
      clearTimeout(timer);
    };

    // value 或 delay 变化时重新执行这个 effect
  }, [value, delay]);

  return debouncedValue;
}

export default useDebounce;
