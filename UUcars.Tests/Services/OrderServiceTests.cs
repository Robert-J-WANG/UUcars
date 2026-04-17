// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging.Abstractions;
// using UUcars.API.Data;
// using UUcars.API.DTOs.Requests;
// using UUcars.API.Entities;
// using UUcars.API.Entities.Enums;
// using UUcars.API.Exceptions;
// using UUcars.API.Repositories;
// using UUcars.API.Services;
// using UUcars.Tests.Fakes;
//
// namespace UUcars.Tests.Services;
//
// public class OrderServiceTests
// {
//     // 每个测试用一个独立的 InMemory 数据库（通过唯一的数据库名区分）
//     // 为什么要独立？InMemory 数据库是共享的，如果多个测试用同一个，
//     // 一个测试插入的数据会影响另一个测试的结果
//     private static AppDbContext CreateDbContext()
//     {
//         var options = new DbContextOptionsBuilder<AppDbContext>()
//             .UseInMemoryDatabase(Guid.NewGuid().ToString())
//             .Options;
//         return new AppDbContext(options);
//     }
//
//     private static OrderService CreateService(
//         AppDbContext context,
//         FakeCarRepository? carRepo = null)
//     {
//         // 这个测试需要验证事务性保存，用真实的 EfOrderRepository + InMemory 数据库
//         // 而不是 FakeOrderRepository——因为 OrderService 直接操作 _context 保存，
//         // FakeOrderRepository._store 感知不到这个操作，GetByIdAsync 会返回 null
//         var orderRepo = new EfOrderRepository(context);
//         return new OrderService(
//             orderRepo,
//             carRepo ?? new FakeCarRepository(),
//             context,
//             NullLogger<OrderService>.Instance
//         );
//     }
//
//     // ===== 创建订单的测试 =====
//
//     [Fact]
//     public async Task CreateAsync_WithValidData_ShouldCreateOrderAndMarkCarAsSold()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         // 把 Buyer、Seller、Car 都加进 InMemory 数据库
//         // 原因：EfOrderRepository.GetByIdAsync 用了 Include(o => o.Buyer) 和 Include(o => o.Seller)
//         // InMemory 数据库里必须有这些关联数据，Include 才能正确加载
//         var buyer = new User
//         {
//             Id = 2,
//             Username = "buyer",
//             Email = "buyer@test.com",
//             PasswordHash = "hash",
//             Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//         var seller = new User
//         {
//             Id = 10,
//             Username = "seller",
//             Email = "seller@test.com",
//             PasswordHash = "hash",
//             Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//         var car = new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Price = 260000,
//             Status = CarStatus.Published,
//             Title = "宝马3系",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Mileage = 15000,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//
//         context.Users.AddRange(buyer, seller);
//         context.Cars.Add(car);
//         await context.SaveChangesAsync();
//         carRepo.Seed(car);
//
//         var service = CreateService(context, carRepo);
//
//         // Act
//         var result = await service.CreateAsync(
//             2,
//             new OrderCreateRequest { CarId = 1 });
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(OrderStatus.Pending.ToString(), result.Status);
//         Assert.Equal(260000, result.Price);
//         Assert.Equal(10, result.SellerId);
//         Assert.Equal("buyer", result.BuyerUsername); // 现在能正确加载了
//         Assert.Equal("seller", result.SellerUsername); // 现在能正确加载了
//
//         var updatedCar = await context.Cars.FindAsync(1);
//         Assert.Equal(CarStatus.Sold, updatedCar!.Status);
//     }
//
//     [Fact]
//     public async Task CreateAsync_WhenCarNotPublished_ShouldThrowCarNotAvailableException()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         // 不需要User数据
//         // 在业务规则判断阶段就抛了异常，不涉及 Include 查询
//
//         // Arrange：车辆是 Draft 状态，不可下单
//         var car = new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Price = 260000,
//             Status = CarStatus.Draft,
//             Title = "宝马3系",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Mileage = 15000,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//
//         carRepo.Seed(car);
//         var service = CreateService(context, carRepo);
//
//         // Act + Assert
//
//         // Act + Assert
//         await Assert.ThrowsAsync<CarNotAvailableException>(() => service.CreateAsync(
//             2,
//             new OrderCreateRequest { CarId = 1 }));
//     }
//
//     [Fact]
//     public async Task CreateAsync_WhenBuyerIsSeller_ShouldThrowCannotOrderOwnCarException()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         // 不需要准备user的数据
//         // 在业务规则判断阶段就抛了异常，不涉及 Include 查询
//         var car = new Car
//         {
//             Id = 1,
//             SellerId = 10,
//             Price = 260000,
//             Status = CarStatus.Published, // ← 改成 Published，让代码能走到买家/卖家检查
//             Title = "宝马3系",
//             Brand = "BMW",
//             Model = "3 Series",
//             Year = 2020,
//             Mileage = 15000,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//         carRepo.Seed(car);
//
//         var service = CreateService(context, carRepo);
//
//         // Act + Assert：buyerId = 10，和 SellerId = 10 相同
//         await Assert.ThrowsAsync<CannotOrderOwnCarException>(() =>
//             service.CreateAsync(10, new OrderCreateRequest { CarId = 1 }));
//     }
//
//     [Fact]
//     public async Task CancelAsync_WhenPendingAndBuyer_ShouldCancelOrderAndRestoreCar()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         // 和创建订单的测试一样，CancelAsync 也调用了 _orderRepository.GetByIdAsync（含 Include），
//         // 所以订单、车辆、用户都需要放进 InMemory 数据库。
//
//         var buyer = new User
//         {
//             Id = 2, Username = "buyer", Email = "buyer@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var seller = new User
//         {
//             Id = 10, Username = "seller", Email = "seller@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var car = new Car
//         {
//             Id = 1, SellerId = 10, Status = CarStatus.Sold,
//             Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
//             Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var order = new Order
//         {
//             Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
//             Price = 260000, Status = OrderStatus.Pending,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//
//         context.Users.AddRange(buyer, seller);
//         context.Cars.Add(car);
//         context.Orders.Add(order);
//         await context.SaveChangesAsync();
//         carRepo.Seed(car);
//
//         var service = CreateService(context, carRepo);
//
//         // Act
//         var result = await service.CancelAsync(1, 2);
//
//         // Assert：订单已取消
//         Assert.Equal(OrderStatus.Cancelled.ToString(), result.Status);
//
//         // 验证车辆状态已恢复为 Published
//         var updatedCar = await context.Cars.FindAsync(1);
//         Assert.Equal(CarStatus.Published, updatedCar!.Status);
//     }
//
//     [Fact]
//     public async Task CancelAsync_WhenNotBuyer_ShouldThrowForbiddenException()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         var buyer = new User
//         {
//             Id = 2, Username = "buyer", Email = "buyer@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var seller = new User
//         {
//             Id = 10, Username = "seller", Email = "seller@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var car = new Car
//         {
//             Id = 1, SellerId = 10, Status = CarStatus.Sold,
//             Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
//             Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var order = new Order
//         {
//             Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
//             Price = 260000, Status = OrderStatus.Pending,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//
//         context.Users.AddRange(buyer, seller);
//         context.Cars.Add(car);
//         context.Orders.Add(order);
//         await context.SaveChangesAsync();
//
//         carRepo.Seed(car);
//         var service = CreateService(context, carRepo);
//
//         // Act + Assert：currentUserId = 10（卖家），不是买家，不能取消
//         await Assert.ThrowsAsync<ForbiddenException>(() =>
//             service.CancelAsync(1, 10));
//     }
//
//     [Fact]
//     public async Task CancelAsync_WhenAlreadyCancelled_ShouldThrowOrderStatusException()
//     {
//         // Arrange
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//
//         var buyer = new User
//         {
//             Id = 2, Username = "buyer", Email = "buyer@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var seller = new User
//         {
//             Id = 10, Username = "seller", Email = "seller@test.com",
//             PasswordHash = "hash", Role = UserRole.User,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//         var car = new Car
//         {
//             Id = 1, SellerId = 10, Status = CarStatus.Sold,
//             Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
//             Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//
//         var order = new Order
//         {
//             Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
//             Price = 260000, Status = OrderStatus.Cancelled,
//             CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
//         };
//
//         context.Users.AddRange(buyer, seller);
//         context.Cars.Add(car);
//         context.Orders.Add(order);
//         await context.SaveChangesAsync();
//         carRepo.Seed(car);
//
//         var service = CreateService(context, carRepo);
//
//         // Act + Assert：不能重复取消
//         await Assert.ThrowsAsync<OrderStatusException>(() => service.CancelAsync(1, 2));
//     }
//     
//     [Fact]
//     public async Task CreateAsync_WhenCarNotFound_ShouldThrowCarNotFoundException()
//     {
//         // Arrange：空 carRepo，没有任何车辆
//         // 模拟客户端传了一个不存在的 CarId
//         var context = CreateDbContext();
//         var carRepo = new FakeCarRepository();
//         var service = CreateService(context, carRepo);
//
//         // Act + Assert
//         await Assert.ThrowsAsync<CarNotFoundException>(
//             () => service.CreateAsync(
//                 buyerId: 2,
//                 new OrderCreateRequest { CarId = 999 }));
//     }
// }

