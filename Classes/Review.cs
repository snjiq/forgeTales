using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForgeTales.Classes
{
        public class Review
        {
            public int ReviewId { get; set; }
            public int NovelId { get; set; }
            public int ReaderId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public Reader Reader { get; set; }
    }
}
