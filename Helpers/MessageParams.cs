namespace DatingApp_backEnd.Helpers
{
    public class MessageParams
    {
        public int PageNumber { get; set; } = 1;
        private int pageSize = 10;
        private const int MaxPageSize = 50;
        public int PageSize
        {
            get { return pageSize;}
            set { pageSize = (value > MaxPageSize) ?  MaxPageSize : value;}
        }
        public int UserId { get; set; }
        public string MessageContainer { get; set; }
    }
}