using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUserAPI.Model
{
    public class Location
    {
        public Street street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        // Postcode, API'den gelen değerin tipine bağlı olarak int veya string olabilir.
        // Bu yüzden object veya string kullanabilirsin.
        public object postcode { get; set; }
        public Coordinates coordinates { get; set; }
        public Timezone timezone { get; set; }
    }
}
