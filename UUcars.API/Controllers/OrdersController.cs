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
    private readonly OrderService _orderService;
    private readonly CurrentUserService _currentUserService;
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
}