using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("orders")]
[Authorize] // 订单系统所有接口都需要登录
public class OrdersController : ControllerBase
{
    private readonly CurrentUserService _currentUserService;
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService, CurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    // POST /orders
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] OrderCreateRequest request,
        CancellationToken cancellationToken)
    {
        var buyerId = _currentUserService.GetCurrentUserId();
        if (buyerId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var order = await _orderService.CreateAsync(buyerId.Value, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<OrderResponse>.Ok(order, "Order created successfully."));
    }

    // POST /orders/{id}/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var order = await _orderService.CancelAsync(id, currentUserId.Value, cancellationToken);
        return Ok(ApiResponse<OrderResponse>.Ok(order, "Order cancelled successfully."));
    }

    // GET /orders/my-purchases
// 注意：必须放在 GET /orders/{id:int} 之前定义（如果以后有这个接口）
// 原因和 Step 20 里 /cars/my-listings 的路由顺序问题一样：
// 固定路径要优先于参数路径，:int 约束虽然能保护，但显式顺序更清晰
    [HttpGet("my-purchases")]
    public async Task<IActionResult> GetMyPurchases(
        [FromQuery] CarQueryRequest query,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var result = await _orderService.GetMyPurchasesAsync(
            currentUserId.Value, query, cancellationToken);

        return Ok(ApiResponse<PagedResponse<OrderResponse>>.Ok(result));
    }

// GET /orders/my-sales
    [HttpGet("my-sales")]
    public async Task<IActionResult> GetMySales(
        [FromQuery] CarQueryRequest query,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var result = await _orderService.GetMySalesAsync(
            currentUserId.Value, query, cancellationToken);

        return Ok(ApiResponse<PagedResponse<OrderResponse>>.Ok(result));
    }
}