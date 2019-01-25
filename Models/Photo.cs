using System;

namespace DatingApp.API.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; }
        //id that sends cloudinary in response
        public string PublicId { get; set; }
        //relation to user
        public User User { get; set; }
        public int UserId { get; set; }
        //it'll be restrict on delete if its nullable, and cascade if it's not
        //we want cascade, when user is deleted, photo should be deleted as well
    }
}