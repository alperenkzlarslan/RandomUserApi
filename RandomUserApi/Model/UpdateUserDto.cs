namespace RandomUserApi.Model
{
    public class UpdateUserDto
    {
        public string gender { get; set; }
        public string username { get; set; }
        public Name name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
    }
} 