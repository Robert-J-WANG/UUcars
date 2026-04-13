using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class CarServiceTests
{
    private static CarService CreateService(
        FakeCarRepository carRepo,
        FakeCarImageRepository? imageRepo = null)
    {
        return new CarService(
            carRepo,
            imageRepo ?? new FakeCarImageRepository(),
            NullLogger<CarService>.Instance
        );
    }

    // ===== 提交审核的测试 =====

    [Fact]
    public async Task SubmitForReviewAsync_WhenDraftAndOwner_ShouldChangeToPendingReview()
    {
        // Arrange
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
        var service = CreateService(repo);

        // Act
        var result = await service.SubmitForReviewAsync(1, currentUserId: 10);

        // Assert
        Assert.Equal("PendingReview", result.Status);
    }

    [Fact]
    public async Task SubmitForReviewAsync_WhenNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
        var service = CreateService(repo);

        // Act + Assert：用不同的 userId（99）操作
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SubmitForReviewAsync(1, currentUserId: 99));
    }

    [Fact]
    public async Task SubmitForReviewAsync_WhenAlreadyPendingReview_ShouldThrowCarStatusException()
    {
        // Arrange：车辆已经是 PendingReview 状态
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.PendingReview });
        var service = CreateService(repo);

        // Act + Assert：不能重复提交
        await Assert.ThrowsAsync<CarStatusException>(
            () => service.SubmitForReviewAsync(1, currentUserId: 10));
    }

    // ===== 车辆详情权限的测试 =====

    [Fact]
    public async Task GetDetailAsync_WhenPublished_ShouldBeVisibleToAnyone()
    {
        // Arrange：Published 状态，未登录用户（currentUserId = null）
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Published,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：未登录（null），不是 Admin（false）
        var result = await service.GetDetailAsync(1, currentUserId: null, isAdmin: false);

        // Assert：Published 的车任何人都能看到
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndOwner_ShouldBeVisible()
    {
        // Arrange：Draft 状态，车主查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：车主（currentUserId = 10）查看自己的草稿
        var result = await service.GetDetailAsync(1, currentUserId: 10, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndNotOwner_ShouldThrowCarNotFoundException()
    {
        // Arrange：Draft 状态，非车主查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act + Assert：非车主看不到 Draft 车辆，返回 404（而不是 403）
        await Assert.ThrowsAsync<CarNotFoundException>(
            () => service.GetDetailAsync(1, currentUserId: 99, isAdmin: false));
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndAdmin_ShouldBeVisible()
    {
        // Arrange：Draft 状态，Admin 查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：Admin（isAdmin = true）可以看任何状态的车辆
        var result = await service.GetDetailAsync(1, currentUserId: 99, isAdmin: true);

        Assert.NotNull(result);
    }
}