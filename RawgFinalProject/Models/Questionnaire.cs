using System;
using System.Collections.Generic;

namespace RawgFinalProject.Models
{
    public partial class Questionnaire
    {
        public int Id { get; set; }
        public string Genres { get; set; }
        public string Tags { get; set; }
        public string UserId { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
