using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForgeTales.Classes
{
    public class GenreGroup
    {
        public int GenreId { get; set; }
        public string Name { get; set; }

        public GenreGroup(int genreId, string name)
        {
            GenreId = genreId;
            Name = name;
        }
    }
}
