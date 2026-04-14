using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")] // Controller 级别：所有 Admin 接口都需要 Admin 角色
public class AdminController : ControllerBase
{
    private readonly AdminCarService _adminCarService;

    public AdminController(AdminCarService adminCarService)
    {
        _adminCarService = adminCarService;
    }

    // POST /admin/cars/{id}/approve
    [HttpPost("cars/{id:int}/approve")]
    public async Task<IActionResult> ApproveCar(int id, CancellationToken cancellationToken)
    {
        var car = await _adminCarService.ApproveAsync(id, cancellationToken);
        return Ok(ApiResponse<CarResponse>.Ok(car, "Car approved and published successfully."));
    }

    // POST /admin/cars/{id}/reject
    [HttpPost("cars/{id:int}/reject")]
    public async Task<IActionResult> RejectCar(int id, CancellationToken cancellationToken)
    {
        var car = await _adminCarService.RejectAsync(id, cancellationToken);
        return Ok(ApiResponse<CarResponse>.Ok(car, "Car rejected and returned to seller for revision."));
    }
}