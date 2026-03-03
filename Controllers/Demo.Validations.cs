using System.ComponentModel.DataAnnotations;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Model untuk registrasi pengguna
/// </summary>
public class UserRegistrationModel
{
    /// <summary>
    /// Nama lengkap pengguna
    /// </summary>
    [Required(ErrorMessage = "Nama lengkap wajib diisi")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nama harus antara 3-100 karakter")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Alamat email pengguna
    /// </summary>
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Kata sandi pengguna
    /// </summary>
    [Required(ErrorMessage = "Kata sandi wajib diisi")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Kata sandi minimal 8 karakter")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Kata sandi harus mengandung huruf besar, huruf kecil, dan angka"
    )]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Konfirmasi kata sandi
    /// </summary>
    [Required(ErrorMessage = "Konfirmasi kata sandi wajib diisi")]
    [Compare(nameof(Password), ErrorMessage = "Kata sandi tidak cocok")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nomor telepon
    /// </summary>
    [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Umur pengguna
    /// </summary>
    [Range(18, 120, ErrorMessage = "Umur harus antara 18-120 tahun")]
    public int Age { get; set; }
}

/// <summary>
/// Demo Validations
/// </summary>
[Route("api/demo/Managements")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoValidations : ControllerBase
{
    /// <summary>
    /// Registrasi pengguna baru
    /// </summary>
    /// <param name="model">Data registrasi pengguna</param>
    /// <returns>Hasil registrasi</returns>
    [HttpPost("register")]
    [ProducesResponseType<APIResponseData<UserRegistrationModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIResponseData<List<string>>>(StatusCodes.Status400BadRequest)]
    public IActionResult Register([FromBody] UserRegistrationModel model)
    {
        // Jika sampai sini, berarti model sudah valid
        // Proses registrasi pengguna...

        return Ok(
            new APIResponseData<UserRegistrationModel>()
                .ChangeStatus("Success")
                .ChangeMessage("Registrasi berhasil")
                .ChangeData(model)
        );
    }

    /// <summary>
    /// Contoh endpoint dengan parameter query yang divalidasi
    /// </summary>
    /// <param name="email">Alamat email</param>
    /// <param name="age">Umur</param>
    /// <returns>Hasil validasi</returns>
    [HttpGet("validate")]
    [ProducesResponseType<APIResponseData<List<string>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIResponseData<List<string>>>(StatusCodes.Status400BadRequest)]
    public IActionResult ValidateUser(
        [FromQuery] [EmailAddress] string email,
        [FromQuery] [Range(18, 120)] int age
    )
    {
        return Ok(
            new APIResponseData<List<string>>().ChangeStatus("Success").ChangeMessage("Data valid")
        );
    }
}
