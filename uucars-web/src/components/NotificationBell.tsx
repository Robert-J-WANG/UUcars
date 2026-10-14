import * as signalR from "@microsoft/signalr";
import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationApi } from "@/api";
import { Bell } from "lucide-react";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";
import type { Notification } from "@/types";
import { useNavigate } from "react-router-dom";

// 辅助函数，用于生成一个根据`userId`动态变化的query key
const notificationKeys = {
  unread: (userId: number | undefined) =>
    ["notifications", "unread", userId] as const,
};

// 辅助函数, 根据通知类型决定点击后跳转到哪里
function getNotificationUrl(notification: Notification): string {
  switch (notification.type) {
    case "CarApproved":
      return `/cars/${notification.relatedId}`;
    case "CarRejected":
      return `/cars/${notification.relatedId}/edit`;
    case "NewOrder":
    case "OrderCancelled":
      return "/profile/sales";
    default:
      return "/";
  }
}

export default function NotificationBell() {
  const queryClient = useQueryClient();
  // 当前用户的id
  const userId = useAuthStore((state) => state.user?.id);
  const [isOpen, setIsOpen] = useState(false);
  const navigate = useNavigate();

  // 1. 获取未读消息列表（自动托管 loading / error / data 状态）
  const { data: notifications = [] } = useQuery({
    queryKey: notificationKeys.unread(userId),
    queryFn: () => notificationApi.getAll(),
    retry: false, // 通知拉取失败不影响主流程，无需反复重试
  });

  // 派生状态：不需要额外的 unreadCount State，直接算就行，效率更高！
  const unreadCount = notifications.length;

  // 2. 标记单条已读 Mutation
  const readMutation = useMutation({
    mutationFn: (id: number) => notificationApi.read(id),
    onSuccess: () => {
      // 操作成功后统一作废 key，让 TanStack Query 重新同步后台数据
      queryClient.invalidateQueries({
        queryKey: notificationKeys.unread(userId),
      });
    },
    onError: (error) => toast.error(error.message),
  });

  // 3. 全部标记已读 Mutation
  const readAllMutation = useMutation({
    mutationFn: () => notificationApi.readAll(),
    onSuccess: () => {
      toast.success("All notifications marked as read.");
      queryClient.invalidateQueries({
        queryKey: notificationKeys.unread(userId),
      });
    },
    onError: (error) => toast.error(error.message),
  });

  // 4. SignalR 监听逻辑（仅在组件挂载时建立一次连接）
  useEffect(() => {
    if (userId === undefined) return;

    /*
     * queryKey 定义在 Effect 内部，不需要放进依赖数组。
     * userId 改变时，Effect 会重新执行并生成新用户的 Query Key。
     */
    const queryKey = notificationKeys.unread(userId);
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_API_BASE_URL}/hubs/notification`, {
        // 告诉 SignalR 建立连接时带上 Token
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect()
      .build();

    // 收到实时消息时，让 query 缓存失效，自动拉取最新的通知列表
    connection.on("ReceiveNotification", () => {
      queryClient.invalidateQueries({ queryKey: queryKey });
      // 弹出 Toast 提示
      toast.info("You have received a new notification!", {
        duration: 5000,
      });
    });

    connection.start().catch((err) => console.error(err));

    return () => {
      connection.stop();
    };
  }, [queryClient, userId]);

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="relative flex h-9 w-9 items-center justify-center rounded-full transition-colors"
        style={{ color: "var(--color-text-secondary)" }}
        aria-label={`Notifications (${unreadCount} unread)`}
      >
        <Bell className="h-5 w-5" />
        {unreadCount > 0 && (
          <span
            className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center
                       justify-center rounded-full text-[10px] font-bold text-white"
            style={{ backgroundColor: "var(--color-danger)" }}
          >
            {unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <>
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />

          <div
            className="absolute right-0 top-10 z-20 w-80 rounded-[var(--radius-lg)] border shadow-lg"
            style={{
              backgroundColor: "var(--color-warning-light)",
              borderColor: "var(--color-border)",
            }}
          >
            <div
              className="flex items-center justify-between border-b px-4 py-3"
              style={{ borderColor: "var(--color-border)" }}
            >
              <span
                className="text-sm font-semibold"
                style={{ color: "var(--color-text-primary)" }}
              >
                Notifications
              </span>
              {unreadCount > 0 && (
                <button
                  type="button"
                  onClick={() => readAllMutation.mutate()}
                  className="text-xs"
                  style={{ color: "var(--color-primary)" }}
                >
                  Mark all as read
                </button>
              )}
            </div>

            <div className="max-h-80 overflow-y-auto">
              {notifications.length === 0 ? (
                <p
                  className="px-4 py-6 text-center text-sm"
                  style={{ color: "var(--color-text-muted)" }}
                >
                  No unread notifications
                </p>
              ) : (
                notifications.map((n) => (
                  <button
                    key={n.id}
                    type="button"
                    onClick={() => {
                      readMutation.mutate(n.id);
                      setIsOpen(false);
                      navigate(getNotificationUrl(n));
                    }}
                    className="w-full border-b px-4 py-3 text-left transition-colors last:border-b-0"
                    style={{
                      borderColor: "var(--color-border)",
                      backgroundColor: "transparent",
                    }}
                  >
                    <p
                      className="text-sm"
                      style={{ color: "var(--color-text-primary)" }}
                    >
                      {n.message}
                    </p>
                    <p
                      className="mt-0.5 text-xs"
                      style={{ color: "var(--color-text-muted)" }}
                    >
                      {new Date(n.createdAt).toLocaleDateString()}
                    </p>
                  </button>
                ))
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
