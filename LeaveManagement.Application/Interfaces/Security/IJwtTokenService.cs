using LeaveManagement.Domain.Identity;

namespace LeaveManagement.Application.Interfaces.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user);

    DateTime GetAccessTokenExpiryUtc();
}