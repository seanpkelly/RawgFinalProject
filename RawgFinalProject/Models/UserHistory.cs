using System;
using System.Collections.Generic;

namespace RawgFinalProject.Models
{
    public partial class UserHistory
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string UserId { get; set; }
        public string GameName { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
