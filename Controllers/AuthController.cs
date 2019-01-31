using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers {
    [Route ("api/[controller]")]
    [ApiController] 
    //similar as modelstate
    //if we comment out this line, we would get 500 internal server error,
    //when we send smth that does not matches data annotations
    //its about paramters, but otherwise we would get exception null reference
    public class AuthController : ControllerBase {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController (IAuthRepository repo, IConfiguration config, IMapper mapper) {
            _config = config;
            _mapper = mapper;
            _repo = repo;
        }

        [HttpPost ("register")]
        public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto) {
            //validate request
            //dont need to add from body, because of apicontroller,
            //in the other case we would need to do it

            // if(!ModelState.IsValid)
            //     return BadRequest(ModelState);

            userForRegisterDto.Username = userForRegisterDto.Username.ToLower ();
            if (await _repo.UserExists (userForRegisterDto.Username))
                return BadRequest ("Username already exists");

            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var createdUser = await _repo.Register (userToCreate, userForRegisterDto.Password);
            var userToReturn = _mapper.Map<UserForDetailedDto>(createdUser);

            return CreatedAtRoute("GetUser", new {controller = "Users", id = createdUser.Id}, userToReturn);

        }

        [HttpPost ("login")]
        public async Task<IActionResult> Login (UserForLoginDto userForLoginDto) 
        {
           
            //    throw new Exception("noooo");
                var userFromRepo = await _repo.Login (userForLoginDto.Username, userForLoginDto.Password);
                // if (userFromRepo == null)
                //     return Unauthorized ();

                var claims = new [] {
                    new Claim (ClaimTypes.NameIdentifier, userFromRepo.Id.ToString ()),
                    new Claim (ClaimTypes.Name, userFromRepo.Username.ToString ()),
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8
                    .GetBytes(_config.GetSection("AppSettings:Token").Value));

                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddDays(1),
                    SigningCredentials = creds
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);

                var user = _mapper.Map<UserForListDto>(userFromRepo);

                return Ok(new {
                    token = tokenHandler.WriteToken(token),
                    user
                }); 
        }
    }
}