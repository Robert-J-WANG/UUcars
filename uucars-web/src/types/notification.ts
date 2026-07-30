// 对应后端 NotificationResponse
export interface Notification {
  id: number;
  type: string;
  message: string;
  relatedId: number | null;
  isRead: boolean;
  createdAt: string;
}
