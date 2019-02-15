using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp_backEnd.Dtos;
using DatingApp_backEnd.Helpers;
using DatingApp_backEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp_backEnd.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;

        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name="GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != userId)
            {
                return Unauthorized();
            }
            
            var messageFromRepo = await _repo.GetMessage(id);
            if(messageFromRepo == null)
                return NotFound();

            return Ok(messageFromRepo);

        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
        {
            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != userId)
                return Unauthorized();

            messageParams.UserId = userId;

            var messages = await _repo.GetMessagesForUser(messageParams);
            var messagesToReturn = _mapper.Map<IEnumerable<MessageToReturnDto>>(messages);            

            Response.AddPagination(messages.CurrentPage, messages.PageSize,
            messages.TotalCount, messages.TotalPages);

            return Ok(messagesToReturn);    

        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != userId)
                return Unauthorized();

            if(_repo.GetUser(recipientId) == null)
            {
                return BadRequest("User does not exist");
            }
            
            var messagesFromRepo = await _repo.GetMessageThread(userId, recipientId);
            var messagesToReturn = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            return Ok(messagesToReturn);
                
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            var sender = await _repo.GetUser(userId);
            //we added this line, as well as line for recipient, in order to store info
            //in memory about them, so that automapper can use it when mapping from message to messageToReturn
            //and when mapping from messageForCreation to message

            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != sender.Id)
            {
                return Unauthorized();
            }

            messageForCreationDto.SenderId = userId;

            var recipent = await _repo.GetUser(messageForCreationDto.RecipientId);
            if(recipent == null)
                return BadRequest("Could not find user.");
            
            var message = _mapper.Map<Message>(messageForCreationDto);
            _repo.Add<Message>(message);


            if(await _repo.SaveAll())
            {
                var messageToReturn = _mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {id = message.Id}, messageToReturn);
            }
               

            throw new Exception("Creating the message failed on save");

        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId) 
        {
            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != userId)
            {
                return Unauthorized();
            }

            var messageFromRepo = await _repo.GetMessage(id);

            if(messageFromRepo.SenderId == userId)
                messageFromRepo.SenderDeleted = true;
            
            if(messageFromRepo.RecipientId == userId)
                messageFromRepo.RecipientDeleted = true;
            
            if(messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                _repo.Delete(messageFromRepo);

            if(await _repo.SaveAll())
            {
                return NoContent();
            }

            throw new Exception("Error deleting the message");
            
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            if(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) != userId)
            {
                return Unauthorized();
            }
            
            var message = await _repo.GetMessage(id);
            if(message.RecipientId != userId)
                return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await _repo.SaveAll();
            return NoContent();
        }


    }
}