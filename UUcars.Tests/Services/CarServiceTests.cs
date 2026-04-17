// using Microsoft.Extensions.Logging.Abstractions;
// using UUcars.API.DTOs.Requests;
// using UUcars.API.Entities;
// using UUcars.API.Entities.Enums;
// using UUcars.API.Exceptions;
// using UUcars.API.Services;
// using UUcars.Tests.Fakes;
//
// namespace UUcars.Tests.Services;
//
// public class CarServiceTests
// {
//     private static CarService CreateService(
//         FakeCarRepository carRepo,
//         FakeCarImageRepository? imageRepo = null)
//     {
//         return new CarService(
//             carRepo,
//             imageRepo ?? new FakeCarImageRepository(),
//             NullLogger<CarService>.Instance
//         );
//     }
//
//     // ===== 创建车辆草稿的测试 =====
//
//     [Fact]
//     public async Task CreateAsync_ShouldSetStatusToDraftAndAssignSeller()
//     {
//         // Arrange
//         var repo = new FakeCarRepository();
//         var service = CreateService(repo);
//
//         var request = new CarCreateRequest
//         {
//             Title = "2020 BMW 3 Series",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Price = 260000,
//             Mileage = 15000
//         };
//
//         // Act
//         var result = await service.CreateAsync(10, request);
//
//         // Assert
//         Assert.NotNull(result);
//         // 无论客户端传什么，Status 必须强制为 Draft
//         // 这是核心安全规则：客户端不能绕过审核直接创建 Published 的车辆
//         Assert.Equal("Draft", result.Status);
//         // SellerId 必须是当前登录用户，不能是客户端传入的值
//         Assert.Equal(10, result.SellerId);
//     }
//
//     // ===== 修改车辆的测试 =====
//
//     [Fact]
//     public async Task UpdateAsync_WhenDraftAndOwner_ShouldUpdateFields()
//     {
//         // Arrange
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1, 
//             SellerId = 10, 
//             Status = CarStatus.Draft,
//             Title = "2020 BMW 3 Series",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Price = 260000,
//             Mileage = 15000,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         });
//         var service = CreateService(repo);
//
//         var request = new CarUpdateRequest
//         {
//             Title = "New 2020 BMW 3 Series", 
//             Brand = "BMW", 
//             Model = "3 Series",
//             Year = 2020, 
//             Price = 260000, 
//             Mileage = 15000
//         };
//
//         // Act
//         var result = await service.UpdateAsync(1, currentUserId: 10, request);
//
//         // Assert
//         Assert.Equal("New 2020 BMW 3 Series", result.Title);
//         Assert.Equal(260000, result.Price);
//         // 修改后状态仍是 Draft，修改操作不会改变状态
//         Assert.Equal("Draft", result.Status);
//     }
//     
//     [Fact]
//     public async Task UpdateAsync_WhenPendingReview_ShouldThrowCarStatusException()
//     {
//         // Arrange：已提交审核的车不能修改
//         // 原因：审核中改信息会导致 Admin 看到的和最终上架的内容不一致
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1, 
//             SellerId = 10, 
//             Status = CarStatus.PendingReview,
//             Title = "2020 BMW 3 Series",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Price = 260000,
//             Mileage = 15000,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         });
//         var service = CreateService(repo);
//
//         var request = new CarUpdateRequest
//         {
//             Title = "New 2020 BMW 3 Series", 
//             Brand = "BMW", 
//             Model = "3 Series",
//             Year = 2020, 
//             Price = 260000, 
//             Mileage = 15000
//         };
//
//
//         // Act + Assert
//         await Assert.ThrowsAsync<CarStatusException>(
//             () => service.UpdateAsync(1, currentUserId: 10, request));
//     }
//
//     // ===== 提交审核的测试 =====
//
//     [Fact]
//     public async Task SubmitForReviewAsync_WhenDraftAndOwner_ShouldChangeToPendingReview()
//     {
//         // Arrange
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
//         var service = CreateService(repo);
//
//         // Act
//         var result = await service.SubmitForReviewAsync(1, 10);
//
//         // Assert
//         Assert.Equal("PendingReview", result.Status);
//     }
//
//     [Fact]
//     public async Task SubmitForReviewAsync_WhenNotOwner_ShouldThrowForbiddenException()
//     {
//         // Arrange
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
//         var service = CreateService(repo);
//
//         // Act + Assert：用不同的 userId（99）操作
//         await Assert.ThrowsAsync<ForbiddenException>(() => service.SubmitForReviewAsync(1, 99));
//     }
//
//     [Fact]
//     public async Task SubmitForReviewAsync_WhenAlreadyPendingReview_ShouldThrowCarStatusException()
//     {
//         // Arrange：车辆已经是 PendingReview 状态
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.PendingReview });
//         var service = CreateService(repo);
//
//         // Act + Assert：不能重复提交
//         await Assert.ThrowsAsync<CarStatusException>(() => service.SubmitForReviewAsync(1, 10));
//     }
//
//     // ===== 车辆详情权限的测试 =====
//
//     [Fact]
//     public async Task GetDetailAsync_WhenPublished_ShouldBeVisibleToAnyone()
//     {
//         // Arrange：Published 状态，未登录用户（currentUserId = null）
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Status = CarStatus.Published,
//             Seller = new User { Id = 10, Username = "seller" },
//             Images = []
//         });
//         var service = CreateService(repo);
//
//         // Act：未登录（null），不是 Admin（false）
//         var result = await service.GetDetailAsync(1, null, false);
//
//         // Assert：Published 的车任何人都能看到
//         Assert.NotNull(result);
//         Assert.Equal("Published", result.Status);
//     }
//
//     [Fact]
//     public async Task GetDetailAsync_WhenDraftAndOwner_ShouldBeVisible()
//     {
//         // Arrange：Draft 状态，车主查看
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Status = CarStatus.Draft,
//             Seller = new User { Id = 10, Username = "seller" },
//             Images = []
//         });
//         var service = CreateService(repo);
//
//         // Act：车主（currentUserId = 10）查看自己的草稿
//         var result = await service.GetDetailAsync(1, 10, false);
//
//         Assert.NotNull(result);
//         Assert.Equal("Draft", result.Status);
//     }
//
//     [Fact]
//     public async Task GetDetailAsync_WhenDraftAndNotOwner_ShouldThrowCarNotFoundException()
//     {
//         // Arrange：Draft 状态，非车主查看
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Status = CarStatus.Draft,
//             Seller = new User { Id = 10, Username = "seller" },
//             Images = []
//         });
//         var service = CreateService(repo);
//
//         // Act + Assert：非车主看不到 Draft 车辆，返回 404（而不是 403）
//         await Assert.ThrowsAsync<CarNotFoundException>(() => service.GetDetailAsync(1, 99, false));
//     }
//
//     [Fact]
//     public async Task GetDetailAsync_WhenDraftAndAdmin_ShouldBeVisible()
//     {
//         // Arrange：Draft 状态，Admin 查看
//         var repo = new FakeCarRepository();
//         repo.Seed(new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Status = CarStatus.Draft,
//             Seller = new User { Id = 10, Username = "seller" },
//             Images = []
//         });
//         var service = CreateService(repo);
//
//         // Act：Admin（isAdmin = true）可以看任何状态的车辆
//         var result = await service.GetDetailAsync(1, 99, true);
//
//         Assert.NotNull(result);
//     }
//
// }

