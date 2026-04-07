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
}