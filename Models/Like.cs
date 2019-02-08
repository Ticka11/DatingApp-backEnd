using DatingApp.API.Models;

namespace DatingApp_backEnd.Models
{
    public class Like
    {
        public int LikerId { get; set; }
        //the one that liked us
        public int LikeeId { get; set; }
        //the one that we liked
        public User Liker { get; set; }
        public User Likee { get; set; }
    }
}