using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PortunusAdiutor.Exceptions;
using PortunusAdiutor.Models;
using PortunusAdiutor.Services.TokenBuilder;
using PortunusAdiutor.Services.UsersManager;
using PortunusAdiutor.Static;
using PortunusTester.Data;
using PortunusTester.Models;

namespace PortunusTester.Controllers
{
	[ApiController]
	[Route("[controller]/[action]")]
	public class AuthorizationController : ControllerBase
	{
		private readonly ILogger<AuthorizationController> _logger;
		private readonly ApplicationDbContext _context;
		private readonly ITokenBuilder _tokenBuilder;
		private readonly IUsersManager<ApplicationUser, Guid> _userManager;

		public AuthorizationController(
			ILogger<AuthorizationController> logger,
			ApplicationDbContext context,
			ITokenBuilder tokenBuilder,
			IUsersManager<ApplicationUser, Guid> userManager
		)
		{
			_logger = logger;
			_context = context;
			_tokenBuilder = tokenBuilder;
			_userManager = userManager;
		}

		[HttpGet]
		public IActionResult Ping()
		{
			return Ok(DateTime.UtcNow);
		}

		[HttpGet]
		[Authorize]
		public IActionResult WhoAmI()
		{
			return Ok(User.Claims.ToDictionary(e => e.Type, e => e.Value));
		}

		[HttpGet]
		[Authorize(Policy = "Administrator")]
		public IActionResult GetUsersCount()
		{
			return Ok(_context.Users.Count());
		}

		[HttpPost]
		public IActionResult SignUp([FromBody] CredentialsDto credentials)
		{
			try {
				ArgumentNullException.ThrowIfNullOrEmpty(credentials.Email);
				ArgumentNullException.ThrowIfNullOrEmpty(credentials.Password);
				var user = _userManager.CreateUser(
					e => e.Email == credentials.Email,
					() => new ApplicationUser(
						credentials.Email,
						credentials.Password,
						// In a real app, use a safe way to give privileges to an user
						credentials.Email.Substring(credentials.Email.Length-3) == "adm"
					)
				);

				return Ok(_tokenBuilder.BuildToken(user.GetClaims()));
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}

		[HttpPost]
		public IActionResult SignIn([FromBody] CredentialsDto credentials)
		{
			try {
				var user = _userManager.ValidateUser(
					e => e.Email == credentials.Email,
					credentials.Password!
				);

				return Ok(_tokenBuilder.BuildToken(user.GetClaims()));
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}

		[HttpPost]
		public IActionResult SendEmailConfirmation(string email)
		{
			try {
				var user =
					_userManager.SendEmailConfirmation(e => e.Email == email);

				return Ok();
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}

		[HttpPost]
		public IActionResult SendPasswordRedefinition(string email)
		{
			try {
				var user =
					_userManager.SendPasswordRedefinition(e => e.Email == email);

				return Ok();
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}


		[HttpPost]
		public IActionResult ConfirmEmail([FromBody] CredentialsDto cred)
		{
			try {
				var user = _userManager.FindUser(e => e.Email == cred.Email);
				var token =
					SingleUseToken<ApplicationUser, Guid>.GetTokenFrom(
						user.Id,
						cred.Xdc!,
						MessageTypes.EmailConfirmation
					);
				var confirmedUser = _userManager.ConfirmEmail(token);

				return Ok();
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}

		[HttpPost]
		public IActionResult RedefinePassword([FromBody] CredentialsDto cred)
		{
			try {
				var user = _userManager.FindUser(e => e.Email == cred.Email);
				var token =
					SingleUseToken<ApplicationUser, Guid>.GetTokenFrom(
						user.Id,
						cred.Xdc!,
						MessageTypes.PasswordRedefinition
					);
				var redefinedUser = _userManager.RedefinePassword(
					token,
					cred.Password!
				);

				return Ok();
			} catch (PortunusException e) {
				return Problem(e.ShortMessage);
			} catch (Exception e) {
				_logger.LogError(e, "An error has occurred.");
				return Problem();
			}
		}
	}
}