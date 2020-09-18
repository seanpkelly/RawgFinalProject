using System;
using System.Collections.Generic;

namespace RawgFinalProject.Models
{
    public partial class UserFavorite
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public double UserRating { get; set; }
        public string UserId { get; set; }
        public bool IsFavorite { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
