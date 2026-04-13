using UUcars.API.Data;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class OrderService
{
    private readonly ICarRepository _carRepository;
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IOrderRepository _orderRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ICarRepository carRepository,
        AppDbContext context,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _carRepository = carRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateAsync(
        int buyerId,
        OrderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 验证车辆存在
        var car = await _carRepository.GetByIdAsync(request.CarId, cancellationToken);
        if (car == null)
            throw new CarNotFoundException(request.CarId);

        // 2. 验证车辆是 Published 状态（只有上架的车才能下单）
        if (car.Status != CarStatus.Published)
            throw new CarNotAvailableException(request.CarId);

        // 3. 验证买家不是卖家本人
        if (car.SellerId == buyerId)
            throw new CannotOrderOwnCarException();

        // 4. 创建订单实体
        // Price 从车辆当前价格复制过来并锁定，后续卖家改价不影响这个订单
        // SellerId 从车辆冗余存储，方便后续"我卖出的订单"查询
        var order = new Order
        {
            CarId = car.Id,
            BuyerId = buyerId,
            SellerId = car.SellerId, // 从车辆冗余存储
            Price = car.Price, // 锁定当前价格
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 5. 更新车辆状态为 Sold
        car.Status = CarStatus.Sold;
        car.UpdatedAt = DateTime.UtcNow;

        // 6. 在同一个事务里保存订单和车辆状态变更
        // 为什么能保证事务性？
        // _context 是同一个 DbContext 实例，Add 和 Update 都只是把变更记录到追踪器里，
        // 只有调用 SaveChangesAsync 时才真正写数据库。
        // EF Core 会把所有待写入的变更包在一个数据库事务里一起提交，
        // 任何一步失败都会全部回滚。
        _context.Orders.Add(order);
        _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} created: buyer {BuyerId} purchased car {CarId} from seller {SellerId}",
            order.Id, buyerId, car.Id, car.SellerId);

        // 7. 查出完整订单（含关联的 Car / Buyer / Seller 信息）用于响应
        // 为什么要重新查一次？
        // 刚创建的 order 对象里，导航属性（Car、Buyer、Seller）是 null，
        // 因为 EF Core 在 Add 时不会自动加载关联对象。
        // 重新查一次，通过 Include 把所有关联数据加载进来，
        // 才能正确映射出 CarTitle、BuyerUsername、SellerUsername 等字段
        var created = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
        return MapToResponse(created!);
    }

    public async Task<OrderResponse> CancelAsync(
        int orderId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. 查询订单（含关联的 Car / Buyer / Seller）
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
            throw new OrderNotFoundException(orderId);

        // 2. 只有买家可以取消订单
        // 为什么卖家不能取消？
        // 买家已经下单，代表买家有购买意愿。如果卖家可以随意取消，
        // 会严重损害买家体验，也违背了平台交易的基本规则。
        // 取消权在买家，卖家如果不想卖，应该走其他流程（平台客服介入等）
        if (order.BuyerId != currentUserId)
            throw new ForbiddenException();

        // 3. 只有 Pending 状态可以取消
        if (order.Status != OrderStatus.Pending)
            throw new OrderStatusException(orderId, order.Status, OrderStatus.Pending);

        // 4. 更新订单状态
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        // 5. 恢复车辆状态为 Published
        // 为什么要恢复？
        // Step 26 里创建订单时把车辆状态改成了 Sold，
        // 取消订单等于这笔交易没有成立，车辆应该重新上架供其他买家购买
        var car = await _carRepository.GetByIdAsync(order.CarId, cancellationToken);
        if (car != null)
        {
            car.Status = CarStatus.Published;
            car.UpdatedAt = DateTime.UtcNow;
        }

        // 6. 在同一个事务里保存订单和车辆状态变更
        // 原理同 Step 26：同一个 DbContext 的 SaveChangesAsync 是原子性的
        // 订单取消和车辆恢复要么同时成功，要么同时回滚
        _context.Orders.Update(order);
        if (car != null)
            _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} cancelled by buyer {BuyerId}, car {CarId} restored to Published",
            orderId, currentUserId, order.CarId);

        return MapToResponse(order);
    }

    public async Task<PagedResponse<OrderResponse>> GetMyPurchasesAsync(int buyerId, CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Min(50, request.PageSize);

        var (orders, totalCount) = await _orderRepository.GetByBuyerAsync(buyerId, page, pageSize, cancellationToken);


        var items = orders.Select(MapToResponse).ToList();

        return PagedResponse<OrderResponse>.Create(items, totalCount, page, pageSize);
    }

    public async Task<PagedResponse<OrderResponse>> GetMySalesAsync(int sellerId, CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Min(50, request.PageSize);

        var (orders, totalCount) = await _orderRepository.GetBySellerAsync(sellerId, page, pageSize, cancellationToken);

        var items = orders.Select(MapToResponse).ToList();

        return PagedResponse<OrderResponse>.Create(items, totalCount, page, pageSize);
    }


    internal static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CarId = order.CarId,
            CarTitle = order.Car?.Title ?? string.Empty,
            BuyerId = order.BuyerId,
            BuyerUsername = order.Buyer?.Username ?? string.Empty,
            SellerId = order.SellerId,
            SellerUsername = order.Seller?.Username ?? string.Empty,
            Price = order.Price,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}