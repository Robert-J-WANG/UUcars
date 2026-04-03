namespace UUcars.API.Entities.Enums;

public enum CarStatus
{
    Draft,          // 草稿，卖家刚创建
    PendingReview,  // 已提交，等待 Admin 审核
    Published,      // 审核通过，公开可见
    Sold,           // 已售出
    Deleted         // 逻辑删除
}