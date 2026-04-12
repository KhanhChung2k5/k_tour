using System.ComponentModel.DataAnnotations;

namespace HeriStepAI.Web.Models;

public class ShopOwnerRegisterViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Từ 3–50 ký tự")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Không được có khoảng trắng")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Họ tên")]
    public string? FullName { get; set; }

    [Display(Name = "Điện thoại")]
    public string? Phone { get; set; }
}
