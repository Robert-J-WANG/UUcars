using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    private readonly CurrentUserService _currentUserService;
    private readonly ReviewService _reviewService;

    public ReviewsController(
        ReviewService reviewService,
        CurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
    }

    // POST /reviews
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var review = await _reviewService.CreateAsync(
            currentUserId.Value, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ReviewResponse>.Ok(review, "Review submitted successfully."));
    }

    // GET /reviews/seller/{id}
    // 公开接口，不需要登录，任何人都可以查看卖家的评价
    [HttpGet("seller/{id:int}")]
    public async Task<IActionResult> GetSellerRating(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetSellerRatingAsync(
            id, cancellationToken);

        return Ok(ApiResponse<SellerRatingResponse>.Ok(result));
    }
}