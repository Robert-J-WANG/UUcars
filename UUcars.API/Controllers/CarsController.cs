using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("cars")]
public class CarsController : ControllerBase
{
    private readonly CarService _carService;
    private readonly CurrentUserService _currentUserService;

    public CarsController(CarService carService, CurrentUserService currentUserService)
    {
        _carService = carService;
        _currentUserService = currentUserService;
    }

    // POST /cars
    // 不在 Controller 级别加 [Authorize]，因为后续会有公开接口（车辆列表、搜索、详情）
    // 只在需要登录的 Action 上单独加 [Authorize]
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CarCreateRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = _currentUserService.GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var car = await _carService.CreateAsync(sellerId.Value, request, cancellationToken);

        // 201 Created：创建成功，返回新建的资源
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CarResponse>.Ok(car, "Car draft created successfully."));
    }

    // POST /cars/{id}/submit
    [HttpPost("{id:int}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var car = await _carService.SubmitForReviewAsync(id, currentUserId.Value, cancellationToken);
        return Ok(ApiResponse<CarResponse>.Ok(car, "Car submitted for review successfully."));
    }
    
    // PUT /cars/{id}
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CarUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var car = await _carService.UpdateAsync(id, currentUserId.Value, request, cancellationToken);
        return Ok(ApiResponse<CarResponse>.Ok(car, "Car updated successfully."));
    }
    
    // DELETE /cars/{id}
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _carService.DeleteAsync(id, currentUserId.Value, cancellationToken);

        // 204 No Content：删除成功，无响应体
        // 不用 ApiResponse 包装，因为没有数据需要返回
        return NoContent();
    }
    
    // POST /cars/{id}/images
    [HttpPost("{id:int}/images")]
    [Authorize]
    public async Task<IActionResult> AddImage(
        int id,
        [FromBody] CarImageAddRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var image = await _carService.AddImageAsync(id, currentUserId.Value, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CarImageResponse>.Ok(image, "Image added successfully."));
    }
    
    // DELETE /cars/{id}/images/{imageId}
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _carService.DeleteImageAsync(id, imageId, currentUserId.Value, cancellationToken);

        return NoContent();
    }
    
    // GET /cars?page=1&pageSize=20
    // 这个接口不需要 [Authorize]，公开访问
    // [FromQuery]：告诉框架从 URL 的 Query 参数里绑定 CarQueryRequest 的属性
    // 比如 /cars?page=2&pageSize=10 会自动绑定成 query.Page=2, query.PageSize=10

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] CarQueryRequest request ,CancellationToken cancellationToken)
    {
        var result = await _carService.GetPublishedCarsAsync(request, cancellationToken);
        
        return StatusCode(StatusCodes.Status200OK, ApiResponse<PagedResponse<CarResponse>>.Ok(result, "Cars retrieved successfully."));
    }
    
    // GET /cars/my-listings
    // 必须放在 GET /cars/{id:int} 之前定义，虽然 :int 约束已经能区分，
    // 但显式把固定路径放在参数路由之前是更好的习惯，意图更清晰
    [HttpGet("my-listings")]
    [Authorize]     // 卖家接口，必须登录
    public async Task<IActionResult> GetMyListings([FromQuery] CarQueryRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if(currentUserId==null) return Unauthorized(ApiResponse<object>.Fail("Invalid token."));
        
        var result= await _carService.GetSellerCarsAsync(currentUserId.Value, request, cancellationToken);
        
        return StatusCode(StatusCodes.Status200OK, ApiResponse<PagedResponse<CarResponse>>.Ok(result, "Cars retrieved successfully."));
        
    }
    
    // GET /cars/{id}
    // 不需要 [Authorize]：公开接口，但权限判断在 Service 层处理
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        // 从 Token 里获取当前用户信息（未登录时为 null）
        var currentUserId = _currentUserService.GetCurrentUserId();
        
        // 判断当前用户是否是 Admin
        // User.IsInRole：ASP.NET Core 提供的方法，读取 ClaimsPrincipal 里的 Role Claim
        // 未登录时 User.IsInRole 返回 false，不会抛异常
        var isAdmin = User.IsInRole("Admin");
        
        var car = await _carService.GetDetailAsync(id, currentUserId, isAdmin, cancellationToken);

        return StatusCode(StatusCodes.Status200OK,
            ApiResponse<CarDetailResponse>.Ok(car, "Car retrieved successfully."));
    }
}