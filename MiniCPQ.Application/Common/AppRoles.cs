namespace MiniCPQ.Application.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Sales = "Sales";
    public const string AdminOrSales = Admin + "," + Sales;
}
