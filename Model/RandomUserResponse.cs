using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUserAPI.Model
{
    public class RandomUserResponse
    {
        public List<Result> results { get; set; }
        public Info info { get; set; }
    }
}
