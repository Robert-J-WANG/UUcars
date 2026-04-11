using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("favorites")]
[Authorize] // 收藏系统所有接口都需要登录
public class FavoritesController : ControllerBase
{
    private readonly FavoriteService _favoriteService;
    private readonly CurrentUserService _currentUserService;

    public FavoritesController(
        FavoriteService favoriteService,
        CurrentUserService currentUserService)
    {
        _favoriteService = favoriteService;
        _currentUserService = currentUserService;
    }

    // POST /favorites/{carId}
    [HttpPost("{carId:int}")]
    public async Task<IActionResult> AddFavorite(int carId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var result = await _favoriteService.AddFavoriteAsync(userId.Value, carId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<FavoriteResponse>.Ok(result, "Car added to favorites."));
    }

    // DELETE /favorites/{carId}
    [HttpDelete("{carId:int}")]
    public async Task<IActionResult> RemoveFavorite(int carId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        await _favoriteService.RemoveFavoriteAsync(userId.Value, carId, cancellationToken);

        // 删除成功返回 204 No Content，和 DELETE /cars/{id} 保持一致
        return NoContent();
    }
}