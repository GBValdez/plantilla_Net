using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using project.users.dto;
using project.utils.dto;
using project.utils.services;
namespace project.users;

public class userSvc(UserManager<userEntity> users, emailService email, IConfiguration configuration)
{
    public async Task<errorMessageDto?> register(userCreationDto credentials, IList<string> roles)
    {
        var user = new userEntity { UserName = credentials.userName, Email = credentials.email };
        var result = await users.CreateAsync(user, credentials.password);
        if (!result.Succeeded) return new errorMessageDto("No se pudo crear la cuenta. Revisa los datos y los requisitos de contraseña.");
        var roleResult = await users.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded) throw new InvalidOperationException("No fue posible asignar los roles de la cuenta.");
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
        var link = QueryHelpers.AddQueryString(configuration["FrontUrl"]!.TrimEnd('/') + "/user/confirmEmail",
            new Dictionary<string, string?> { ["email"] = credentials.email, ["token"] = token });
        await email.SendEmailAsync(new emailSendDto
        {
            email = credentials.email,
            subject = "Confirma tu cuenta de PanelBase",
            message = $"<h1>Confirma tu correo</h1><p>Completa el registro de tu cuenta.</p><a href='{HtmlEncoder.Default.Encode(link)}'>Confirmar correo</a>"
        });
        return null;
    }
}
