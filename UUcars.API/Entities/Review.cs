namespace UUcars.API.Entities;

public class Review : BaseEntity
{
    // 关联的订单（唯一索引保证一个订单只能评价一次）
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // 评价人（买家）
    public int ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    // 被评价人（卖家）
    public int RevieweeId { get; set; }
    public User Reviewee { get; set; } = null!;

    // 评分 1-5 星
    public int Rating { get; set; }

    // 评价内容，允许为空（用户可以只打分不写评论）
    public string? Comment { get; set; }
}