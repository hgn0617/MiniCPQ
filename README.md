# MiniCPQ 服务器报价系统

MiniCPQ 是一个基于 ASP.NET Core MVC 开发的服务器报价练习项目。管理员可以维护材料价格、配置服务器型号并审批报价；销售可以选择服务器、填写数量并提交报价。系统会根据材料成本自动计算报价，并在提交后保存价格快照，避免后续材料调价影响历史报价。

## 主要功能

- Admin/Sales 角色登录与权限控制
- 材料价格和服务器配置管理
- 销售创建、修改和提交报价
- 报价提交时计算并冻结成本快照
- 管理员按毛利率计算售价，或直接修改售价
- Razor 管理页面与 Swagger API 文档

## 技术栈

- .NET 10、ASP.NET Core MVC、Razor Views
- Entity Framework Core 10、ASP.NET Core Identity
- PostgreSQL 17、Npgsql
- Swagger / OpenAPI
- xUnit

## 项目结构

```text
MiniCPQ.Domain          领域实体和报价状态
MiniCPQ.Application     DTO、业务接口和业务异常
MiniCPQ.Infrastructure  EF Core、Identity、服务实现和数据库迁移
MiniCPQ.Web             MVC 页面、API Controller 和 Swagger
MiniCPQ.Tests           自动化业务测试
```

## 如何运行

### 1. 准备环境

请先安装：

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/)（推荐 17）
- Git

### 2. 下载代码

```bash
git clone https://github.com/hgn0617/MiniCPQ.git
cd MiniCPQ
```

### 3. 创建数据库

先启动 PostgreSQL，然后创建名为 `minicpq` 的空数据库：

```bash
createdb minicpq
```

如果没有 `createdb` 命令，也可以在 PostgreSQL 或 DBeaver 中执行：

```sql
CREATE DATABASE minicpq;
```

### 4. 配置数据库连接

推荐通过环境变量传入自己电脑的 PostgreSQL 用户名和密码。

macOS / Linux：

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=minicpq;Username=你的用户名;Password=你的密码'
```

Windows PowerShell：

```powershell
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=minicpq;Username=你的用户名;Password=你的密码'
```

本机 PostgreSQL 使用免密码认证时，可以省略连接字符串中的 `Password`。

### 5. 启动项目

```bash
dotnet tool restore
dotnet restore
dotnet run --project MiniCPQ.Web
```

程序会自动应用 EF Core Migration、创建数据表和初始化演示账号。启动成功后访问：

- 项目首页：<http://localhost:5113>
- 登录页面：<http://localhost:5113/account/login>
- Swagger：<http://localhost:5113/swagger>

## 演示账号

| 角色 | 用户名 | 密码 |
|---|---|---|
| 管理员 | `admin` | `123456` |
| 销售 | `sales` | `123456` |

这些账号只用于本地学习和演示，不应直接用于生产环境。

## 推荐体验流程

1. 使用 `admin` 登录，在“材料管理”中添加材料及成本。
2. 在“服务器配置”中创建服务器型号并选择所需材料和数量。
3. 退出管理员账号，使用 `sales` 登录并创建报价。
4. 向报价中添加服务器、修改数量，然后提交报价。
5. 再次使用 `admin` 登录，设置毛利率或售价并审批报价。

## 运行测试

```bash
dotnet test MiniCPQ.slnx
```

仓库不包含本机数据库数据或真实数据库密码。每位使用者都需要创建自己的 PostgreSQL 数据库。
