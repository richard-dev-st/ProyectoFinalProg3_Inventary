using System.Security.Claims;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Presentation.API.Authorization
{
    public class PermisoRequirement : IAuthorizationRequirement
    {
        public string Permiso { get; }
        public PermisoRequirement(string permiso) => Permiso = permiso;
    }

    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly IApplicationDbContext _db;
        public PermisoAuthorizationHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;

            if(Guid.TryParse(userIdClaim, out var id) && await _db.PermisosUsuario.AnyAsync(p => p.UsuarioId == id && p.Permiso == requirement.Permiso))
            {
                context.Succeed(requirement);
            }
        }
    }
}
