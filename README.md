# Mini CPQ 服务器报价系统

根据 `AspNetCore练习.pdf` 实现的 ASP.NET Core 练习项目。管理员维护硬件材料和服务器配置，销售创建并提交报价，系统在提交时计算金额并冻结材料价格快照。项目同时提供 Razor 业务页面和 Swagger API 调试界面。

## 技术栈

- .NET 10 / ASP.NET Core MVC Controller + Razor Views
- ASP.NET Core Identity（Cookie 登录、Admin/Sales 角色）
- Entity Framework Core 10 + Migration
- PostgreSQL 17（Npgsql）
- Swagger / OpenAPI
- xUnit + SQLite 内存数据库（自动化业务测试）

## 解决方案结构

```text
MiniCPQ.Domain          领域实体和报价状态
MiniCPQ.Application     DTO、服务接口、业务异常
MiniCPQ.Infrastructure  EF Core、Identity、Service 实现、Migration
MiniCPQ.Web             MVC 页面、API Controller、Swagger、全局异常处理
MiniCPQ.Tests           核心业务流程测试
```

## 本机启动

当前项目按实习环境备忘录使用 PostgreSQL 17。练习环境预置了简单的用户名和密码，便于直接演示角色权限。

```bash
cd MiniCPQ

brew services run postgresql@17
createdb minicpq  # 数据库已存在时跳过

dotnet tool restore
dotnet run --project MiniCPQ.Web
```

其他电脑上的 PostgreSQL 用户名不同时，可在启动前覆盖连接字符串：

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=minicpq;Username=你的数据库用户名;Password=你的数据库密码'
```

本机采用免密码认证时可以省略 `Password`。仓库不会包含真实数据库密码或现有数据库数据。

程序启动时会自动应用 Migration、创建 `Admin`/`Sales` 角色，并创建或同步以下练习账号：

- 管理员：用户名 `admin`，密码 `123456`
- 销售：用户名 `sales`，密码 `123456`

角色仍由 ASP.NET Core Identity 的角色关系确定，程序不会根据用户名字符串硬编码权限。`123456` 仅适合本机练习，正式环境应从环境变量或密钥系统覆盖密码并恢复强密码策略。

打开终端输出中的地址，然后访问：

- `/`：项目入口页
- `/account/login`：业务系统登录
- `/quotes`：报价管理
- `/admin/materials`：管理员材料管理
- `/admin/servers`：管理员服务器配置
- `/swagger`：Swagger API 文档

在 Swagger 中先调用 `POST /api/auth/login`。登录成功后浏览器会自动携带 HttpOnly Cookie。

## 常用命令

```bash
dotnet build MiniCPQ.slnx
dotnet test MiniCPQ.slnx

dotnet tool run dotnet-ef migrations add <MigrationName> \
  --project MiniCPQ.Infrastructure/MiniCPQ.Infrastructure.csproj \
  --output-dir Data/Migrations

dotnet tool run dotnet-ef database update \
  --project MiniCPQ.Infrastructure/MiniCPQ.Infrastructure.csproj
```

## 主要 API

| 模块 | 方法与路由 | 角色 |
|---|---|---|
| 登录 | `POST /api/auth/login` | 匿名 |
| 当前用户 | `GET /api/auth/me` | 已登录 |
| 材料 CRUD | `/api/materials` | Admin |
| 查询服务器 | `GET /api/servers` | Admin、Sales |
| 服务器增删改 | `/api/servers` | Admin |
| 查询报价 | `GET /api/quotes` | Admin、Sales |
| 创建报价 | `POST /api/quotes` | Sales |
| 报价条目 | `/api/quotes/{id}/items` | Sales |
| 提交报价 | `POST /api/quotes/{id}/submit` | Sales |
| 审批报价 | `POST /api/quotes/{id}/approve` | Admin |

## 业务页面

- 未登录首页与登录页
- 根据 Admin/Sales 角色变化的工作台和导航
- 管理员材料新增、编辑、删除和并发冲突提示
- 管理员服务器材料组合、数量配置和实时成本展示
- 销售报价创建、服务器增删、数量修改和提交
- 提交后的服务器成本、材料展开数量与价格快照展示
- 管理员报价列表、详情、双向联动定价和审批操作

页面使用 MVC Controller 调用现有 Service，业务规则没有复制到 View 或页面 Controller 中。Swagger 保留用于查看和调试同一套后端 API。

## 已实现的业务规则

- 服务器不保存价格；查询时根据当前材料单价动态计算。
- 材料价格使用 `Version` 并发令牌，过期版本更新返回 HTTP 409。
- Draft 报价允许增删服务器、修改数量。
- 提交报价在数据库事务中展开服务器材料、计算并冻结成本、保存快照并改为 Submitted。
- Submitted/Approved 报价不能再修改内容。
- 材料后续改价不会改变历史报价金额和快照单价。
- Submitted 报价等待管理员定价；输入毛利率会反算售价，修改售价也会实时反算毛利率。
- 最终售价不得低于成本，管理员确认售价并审批后，`Price` 与利润数据随报价冻结。
- PDF 给出了 Approved 状态但未定义审批接口，因此补充为 Admin 定价并审批 Submitted 报价。

## 验证范围

自动化测试覆盖：

- 服务器价格随材料价格动态变化。
- 报价提交后生成材料快照并冻结历史金额。
- 管理员定价不能低于成本，审批后售价和毛利率可正确计算。
- 已提交报价禁止继续修改。
- 材料过期版本更新发生并发冲突。

此外已使用本地 PostgreSQL 和浏览器完成 Admin/Sales 真实页面流程验收，包括登录、角色菜单、创建报价、提交快照和管理员审批。

Git 仓库和提交历史尚未初始化，按要求留到后续处理。
