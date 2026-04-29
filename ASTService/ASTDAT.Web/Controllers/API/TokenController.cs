using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using ASTDAT.Web.Infrastructure;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity.Owin;

namespace ASTDAT.Web.Controllers.API
{
    public class TokenRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [AllowAnonymous]
    public class TokenController : ApiController
    {
        [HttpPost]
        [Route("api/Token")]
        public async Task<IHttpActionResult> Post([FromBody] TokenRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return Content(HttpStatusCode.BadRequest, "email and password required");

            var userManager = HttpContext.Current?.GetOwinContext()?.GetUserManager<ApplicationUserManager>();
            if (userManager == null) return InternalServerError();

            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null) return StatusCode(HttpStatusCode.Unauthorized);

            if (!await userManager.CheckPasswordAsync(user, model.Password))
                return StatusCode(HttpStatusCode.Unauthorized);

            var roles = await userManager.GetRolesAsync(user.Id);
            var key = System.Configuration.ConfigurationManager.AppSettings["JwtSigningKey"] ?? "";
            if (string.IsNullOrEmpty(key) || key.Length < 16)
                return InternalServerError();

            var ttl = 3600;
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["JwtTtlSeconds"], out var t) && t > 0) ttl = t;
            var token = SimpleJwt.CreateAccessToken(user.Id, user.UserName, roles.ToList(), key, ttl);
            return Ok(new
            {
                access_token = token,
                token_type = "bearer",
                expires_in = ttl
            });
        }
    }
}
