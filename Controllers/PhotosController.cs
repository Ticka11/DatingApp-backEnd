using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    //automatically validates our request, and shows if it has model state errors
    //if it has, returns 400 bad request
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _datingRepo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository datingRepo, IMapper mapper, IOptions<CloudinarySettings> cloudinarySettings)
        {
            _mapper = mapper;
            _datingRepo = datingRepo;
            _cloudinarySettings = cloudinarySettings;

            Account acc = new Account(
                _cloudinarySettings.Value.CloudName,
                _cloudinarySettings.Value.ApiKey,
                _cloudinarySettings.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id) 
        {
            var photoFromRepo = await _datingRepo.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);
            return Ok(photo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id) 
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                Unauthorized();

            var user = await _datingRepo.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
            {
                return Unauthorized();
            } 
            var photoFromRepo = await _datingRepo.GetPhoto(id);

            if(photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                 //we need to make deletion params, cannot pass only an publicId
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);

                if(result.Result == "ok") 
                {
                    _datingRepo.Delete(photoFromRepo);
                }
            }

            if(photoFromRepo.PublicId == null)
            {
            _datingRepo.Delete(photoFromRepo);
            }
    
           
            if(await _datingRepo.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to delete photo");

        }
        

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId,
        [FromForm] PhotoForCreationDto photoForCreationDto)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                Unauthorized();
            }

            var userFromRepo = await _datingRepo.GetUser(userId);
            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();
            if(file.Length > 0) 
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    //we need for every photo to be tha sam esize, and to be cropped
                    //response from cloudinary in uploadResult
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);
            if(!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;

            userFromRepo.Photos.Add(photo);

            if(await _datingRepo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                Unauthorized();
            var user = await _datingRepo.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
            {
                return Unauthorized();
            } //to be sure that we are not changing other user's photo, but our own

            var photoFromRepo = await _datingRepo.GetPhoto(id);

            if(photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            var currentMainPhoto = await _datingRepo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if(await _datingRepo.SaveAll()) 
                return NoContent();

            return BadRequest("Could not set photo to main!");
        }

    }
}