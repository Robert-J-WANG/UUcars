# UUcars 数据库 Schema

## 表关系

Users  ──<  Cars         一个用户可以发布多辆车（一对多）
Cars   ──<  CarImages    一辆车可以有多张图片（一对多）
Users  ──<  Orders       一个用户可以有多个订单（一对多）
Cars   ──<  Orders       一辆车可以有多条订单记录（一对多）
Users  >──< Cars         用户和车辆之间有收藏关系（多对多，通过 Favorites 实现）

## Users

| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK, Identity | 自增主键 |
| Username | nvarchar(50) | NOT NULL | |
| Email | nvarchar(100) | NOT NULL, UNIQUE | 登录凭证，不能重复 |
| PasswordHash | nvarchar(256) | NOT NULL | Hash 后的密码，不存明文 |
| Role | nvarchar(20) | NOT NULL | User / Admin |
| EmailConfirmed | bit | NOT NULL, DEFAULT 0 | V2 邮箱验证时使用 |
| CreatedAt | datetime2 | NOT NULL | |
| UpdatedAt | datetime2 | NOT NULL | |

V2 预留字段（届时通过 Migration 添加）：
EmailConfirmationToken / ResetPasswordToken / ResetPasswordTokenExpiry

## Cars

| 字段         | 类型           | 约束          | 说明                            |
|---|---|---|---|
| Id           | int            | PK, Identity  |                                 |
| Title        | nvarchar(100)  | NOT NULL      | 车辆标题                        |
| Brand        | nvarchar(50)   | NOT NULL      | 品牌，如 BMW                    |
| Model        | nvarchar(50)   | NOT NULL      | 车型，如 3 Series               |
| Year         | int            | NOT NULL      | 出厂年份                        |
| Price        | decimal(18,2)  | NOT NULL      | 售价                            |
| Mileage      | int            | NOT NULL      | 里程（公里）                    |
| Description  | nvarchar(2000) | NULL          | 详细描述，允许为空              |
| SellerId     | int            | FK → Users.Id | 卖家                            |
| Status       | nvarchar(20)   | NOT NULL      | 见下方状态流转                  |
| CreatedAt    | datetime2      | NOT NULL      |                                 |
| UpdatedAt    | datetime2      | NOT NULL      |                                 |

Status 流转：
Draft → PendingReview → Published → Sold / Deleted
PendingReview 被拒绝 → Draft（退回修改）

## CarImages

| 字段      | 类型          | 约束          | 说明                        |
|---|---|---|---|
| Id        | int           | PK, Identity  |                             |
| CarId     | int           | FK → Cars.Id  |                             |
| ImageUrl  | nvarchar(500) | NOT NULL      | V1 存 URL，V2 做真实上传    |
| SortOrder | int           | NOT NULL, DEFAULT 0 | 显示顺序，第一张为封面 |

## Orders

| 字段      | 类型          | 约束           | 说明                              |
|---|---|---|---|
| Id        | int           | PK, Identity   |                                   |
| CarId     | int           | FK → Cars.Id   |                                   |
| BuyerId   | int           | FK → Users.Id  |                                   |
| SellerId  | int           | FK → Users.Id  | 从 Car 冗余存储，方便卖家查询订单 |
| Price     | decimal(18,2) | NOT NULL       | 下单时锁定，不随车辆价格变动      |
| Status    | nvarchar(20)  | NOT NULL       | Pending / Completed / Cancelled   |
| CreatedAt | datetime2     | NOT NULL       |                                   |
| UpdatedAt | datetime2     | NOT NULL       |                                   |

## Favorites

| 字段      | 类型      | 约束          | 说明                    |
|---|---|---|---|
| UserId    | int       | FK → Users.Id |                         |
| CarId     | int       | FK → Cars.Id  |                         |
| CreatedAt | datetime2 | NOT NULL      |                         |

PRIMARY KEY (UserId, CarId)  — 联合主键，天然防重复收藏