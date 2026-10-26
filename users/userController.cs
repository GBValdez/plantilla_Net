using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using project.utils.mapping;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using project.Models;
using project.users.dto;
using project.users.Models;
using project.utils;
using project.utils.dto;
using project.utils.services;
namespace project.users;

[ApiController]
[Route("user")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
public class userController : controllerCommons<userEntity, userUpdateDto, userDto, userQueryDto, object, string>
{
    private readonly UserManager<userEntity> users;
    private readonly SignInManager<userEntity> signIn;
    private readonly IConfiguration config;
    private readonly emailService emailSender;
    private readonly IDataProtector protector;
    private readonly userSvc registration;
    protected override bool showDeleted { get; set; } = true;
    public userController(UserManager<userEntity> users, IConfiguration config, SignInManager<userEntity> signIn,
        emailService email, IDataProtectionProvider protection, DBProyContext db, IObjectMapper mapper, userSvc registration) : base(db, mapper)
    {
        this.users = users; this.config = config; this.signIn = signIn; emailSender = email;
        protector = protection.CreateProtector("PanelBase.PasswordReset.v1"); this.registration = registration;
    }
    // Identity updates must use UserManager, never the generic creation endpoint.
    [NonAction]
    public override Task<ActionResult<userDto>> post(userUpdateDto data, [FromQuery] object query) =>
        Task.FromResult<ActionResult<userDto>>(new NotFoundResult());

    public override async Task<ActionResult> put(userUpdateDto data, [FromRoute] string id, [FromQuery] object queryCreation)
    {
        var user = await users.FindByNameAsync(id);
        if (user is null) return NotFound();
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier) &&
            (!data.status || !data.roles.Contains("ADMINISTRATOR")))
            return BadRequest(new errorMessageDto("No puedes desactivar tu propia cuenta ni quitarte el rol de administrador."));
        var knownRoles = await context.Roles.Where(r => r.deleteAt == null).Select(r => r.Name!).ToListAsync();
        if (data.roles.Any(r => !knownRoles.Contains(r))) return BadRequest(new errorMessageDto("Uno o más roles no existen."));
        await using var transaction = await context.Database.BeginTransactionAsync();
        user.deleteAt = data.status ? null : DateTime.UtcNow;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);
        var current = await users.GetRolesAsync(user);
        result = await users.RemoveFromRolesAsync(user, current.Except(data.roles));
        if (!result.Succeeded) return BadRequest(result.Errors);
        result = await users.AddToRolesAsync(user, data.roles.Except(current));
        if (!result.Succeeded) return BadRequest(result.Errors);
        // Already issued JWTs expire after the configured short lifetime.
        await users.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync();
        return NoContent();
    }
    [HttpGet("{userName}")]
    public async Task<ActionResult<userDto>> getByUserName(string userName)
    {
        var user = await users.FindByNameAsync(userName);
        if (user is null) return NotFound();
        var dto = mapper.Map<userDto>(user);
        dto.roles = (await users.GetRolesAsync(user)).ToList();
        return dto;
    }
    public override async Task<ActionResult> delete(string id)
    {
        var user = await users.FindByNameAsync(id);
        if (user is null) return NotFound();
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return BadRequest(new errorMessageDto("No puedes desactivar tu propia cuenta."));
        user.deleteAt = DateTime.UtcNow;
        var result = await users.UpdateAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> register(clientCreationDto data)
    {
        // Account, assigned role and client are committed together only if email delivery succeeds.
        await using var transaction = await context.Database.BeginTransactionAsync();
        var error = await registration.register(mapper.Map<userCreationDto>(data), ["userNormal"]);
        if (error is not null) return BadRequest(error);
        var user = await users.FindByEmailAsync(data.email);
        var client = mapper.Map<Client>(data); client.userId = user!.Id;
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }
    protected override async Task<IQueryable<userEntity>> modifyGet(IQueryable<userEntity> query, userQueryDto data)
    {
        if (!string.IsNullOrWhiteSpace(data.UserNameCont)) query = query.Where(u => u.UserName != null && u.UserName.Contains(data.UserNameCont));
        if (!string.IsNullOrWhiteSpace(data.EmailCont)) query = query.Where(u => u.Email != null && u.Email.Contains(data.EmailCont));
        if (data.roles?.Count > 0)
        {
            var ids = await context.UserRoles.Where(r => data.roles.Contains(r.RoleId)).Select(r => r.UserId).ToListAsync();
            query = query.Where(u => ids.Contains(u.Id));
        }
        if (data.isActive.HasValue) query = query.Where(u => (u.deleteAt == null) == data.isActive.Value);
        return query;
    }
    [HttpPost("forgotPassword"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> forgotPassword([FromBody] emailDto data)
    {
        var user = await users.FindByEmailAsync(data.email);
        if (user is null || !user.EmailConfirmed || user.deleteAt is not null) return NoContent();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var protectedEmail = Uri.EscapeDataString(protector.Protect(data.email));
        var protectedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protector.Protect(token)));
        var link = config["FrontUrl"]!.TrimEnd('/') + "/user/resetPassword/" + protectedEmail + "/" + protectedToken;
        await emailSender.SendEmailAsync(new emailSendDto
        {
            email = data.email,
            subject = "Recupera tu cuenta de PanelBase",
            message = $"<h1>Recuperar contraseña</h1><a href='{HtmlEncoder.Default.Encode(link)}'>Crear una nueva contraseña</a>"
        });
        return NoContent();
    }
    [HttpPost("resetPassword"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> resetPassword([FromBody] resetPasswordDto data)
    {
        try
        {
            var address = protector.Unprotect(data.email);
            var token = protector.Unprotect(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(data.token)));
            var user = await users.FindByEmailAsync(address);
            if (user is null || user.deleteAt is not null) return InvalidLink();
            var result = await users.ResetPasswordAsync(user, token, data.password);
            return result.Succeeded ? Ok() : InvalidLink();
        }
        catch (Exception error) when (error is CryptographicException or FormatException or ArgumentException) { return InvalidLink(); }
    }
    [HttpGet("confirmEmail"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult> confirmEmail([FromQuery] string email, [FromQuery] string token)
    {
        try
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null || user.deleteAt is not null) return InvalidLink();
            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await users.ConfirmEmailAsync(user, decoded);
            return result.Succeeded ? Ok() : InvalidLink();
        }
        catch (Exception error) when (error is FormatException or ArgumentException) { return InvalidLink(); }
    }
    private BadRequestObjectResult InvalidLink() => BadRequest(new errorMessageDto("El enlace es inválido o ha expirado."));

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult<authenticationDto>> login(credentialsDto data)
    {
        var user = await users.FindByEmailAsync(data.email);
        if (user is null || !user.EmailConfirmed || user.deleteAt is not null)
            return BadRequest(new errorMessageDto("Credenciales inválidas."));
        var result = await signIn.CheckPasswordSignInAsync(user, data.password, lockoutOnFailure: true);
        if (!result.Succeeded) return BadRequest(new errorMessageDto("Credenciales inválidas."));
        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim> { new("email", user.Email!), new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!), new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new("securityStamp", user.SecurityStamp!) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var client = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.userId == user.Id && c.deleteAt == null);
        if (client is not null) claims.Add(new("clientId", client.Id.ToString()));
        var expiration = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:LifetimeMinutes", 30));
        var token = new JwtSecurityToken(issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims,
            notBefore: DateTime.UtcNow, expires: expiration, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new authenticationDto { token = new JwtSecurityTokenHandler().WriteToken(token), expiration = expiration };
    }
}
