using System;

namespace DapperLostParameters
{
    public class IncClass
    {
        public int Inc { get; set; }
        public UserClass User { get; set; }
        public GroupClass Group { get; set; }
        public StatusClass StatusObj { get; set; }
        public NameClass NameObj { get; set; }
    }
    public class NameClass
    {
        public string Name { get; set; }
        public int? Age { get; set; }
    }

    public class StatusClass
    {
        public int Status { get; set; }
    }

    public class GroupClass
    {
        public Guid? GroupId { get; set;}
    }

    public class UserClass
    {
        public Guid UserId { get; set; }
    }
}