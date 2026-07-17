using System.ComponentModel.DataAnnotations;
using MiniCPQ.Application.DTOs;

namespace MiniCPQ.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "请输入用户名。")]
    [Display(Name = "用户名")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入密码。")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住登录状态")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class MaterialFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "请输入材料名称。")]
    [StringLength(100)]
    [Display(Name = "名称")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入材料类型。")]
    [StringLength(50)]
    [Display(Name = "类型")]
    public string Type { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "单价不能小于零。")]
    [Display(Name = "单价")]
    public decimal UnitPrice { get; set; }

    public Guid Version { get; set; }
}

public sealed class ServerFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "请输入服务器名称。")]
    [StringLength(100)]
    [Display(Name = "服务器名称")]
    public string Name { get; set; } = string.Empty;

    public List<ServerMaterialOptionViewModel> Materials { get; set; } = [];
}

public sealed class ServerMaterialOptionViewModel
{
    public Guid MaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool Selected { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "数量不能小于零。")]
    public int Quantity { get; set; }

    public int? MaxQuantity { get; set; }
    public string? LimitDescription { get; set; }
}

public sealed class QuoteCreateViewModel
{
    [Required(ErrorMessage = "请输入客户名称。")]
    [StringLength(200)]
    [Display(Name = "客户名称")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "报价币种")]
    public Guid? ExchangeRateId { get; set; }

    public IReadOnlyCollection<ExchangeRateDto> ExchangeRates { get; set; } = [];
}

public sealed class QuoteDetailsViewModel
{
    public required QuoteDto Quote { get; init; }
    public IReadOnlyCollection<ServerDto> AvailableServers { get; init; } = [];
}

public sealed class ExchangeRateFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "请输入币种代码。")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "币种代码必须是三个英文字母。")]
    [Display(Name = "币种代码")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入币种名称。")]
    [StringLength(50)]
    [Display(Name = "币种名称")]
    public string CurrencyName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.000001", "999999999999", ErrorMessage = "汇率必须大于零。")]
    [Display(Name = "1 单位外币兑换人民币")]
    public decimal CnyPerUnit { get; set; }

    public Guid Version { get; set; }
}
