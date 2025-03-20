using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace RandomUserApi.Controllers
{
    [Route("api/[controller]")] //API'nin URL formatını belirler. Bu: /api/users şeklinde bir endpoint oluşturur.

    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string connectionString = "Host=localhost;Username=postgres;Password=alperen4423;Database=postgres"; //Veritabanı bağlantısı 

        [HttpGet] //Bu metod HTTP GET isteklerini işler.
        public IActionResult GetUsers() // API, JSON yanıt döndüreceği için genel bir dönüş türü kullanıyoruz
        {
            var users = new List<object>(); //Bu liste, veritabanından çekilen kullanıcıları JSON formatında saklamak için kullanılır.
            try
            {
                using (var conn = new NpgsqlConnection(connectionString)) //nesnesi, PostgreSQL veritabanına bağlantı açıyor. Veritabanı bağlantısını açıyor.
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT * FROM users LIMIT 1", conn)) //Tablodaki tüm verileri çekiyor.
                    using (var reader = cmd.ExecuteReader()) //Sorgunun sonucunu okuyan bir reader (okuyucu) oluşturuyor.
                    {
                    //    var a = "asda";
                    //    Convert.ToInt32(a);
                        while (reader.Read()) //Tüm satırları tek tek okuyoruz.
                        {
                          
                            var user = new //JSON nesnesini oluşturuyoruz.
                            {
                                gender = reader["gender"].ToString(),
                                name = new
                                {
                                    title = reader["title"].ToString(),
                                    first = reader["first_name"].ToString(),
                                    last = reader["last_name"].ToString()
                                },
                                location = new
                                {
                                    street = new
                                    {
                                        number = reader["street_number"],
                                        name = reader["street_name"].ToString()
                                    },
                                    city = reader["city"].ToString(),
                                    state = reader["state"].ToString(),
                                    country = reader["country"].ToString(),
                                    postcode = reader["postcode"],
                                    coordinates = new
                                    {
                                        latitude = reader["latitude"],
                                        longitude = reader["longitude"]
                                    },
                                    timezone = new
                                    {
                                        offset = reader["timezone_offset"].ToString(),
                                        description = reader["timezone_description"].ToString()
                                    }
                                },
                                email = reader["email"].ToString(),
                                login = new
                                {
                                    uuid = reader["login_uuid"].ToString(),
                                    username = reader["login_username"].ToString(),
                                    password = reader["login_password"].ToString(),
                                    salt = reader["login_salt"].ToString(),
                                    md5 = reader["login_md5"].ToString(),
                                    sha1 = reader["login_sha1"].ToString(),
                                    sha256 = reader["login_sha256"].ToString()
                                },
                                dob = new
                                {
                                    date = reader["dob_date"].ToString(),
                                    age = reader["dob_age"]
                                },
                                registered = new
                                {
                                    date = reader["registered_date"].ToString(),
                                    age = reader["registered_age"]
                                },
                                phone = reader["phone"].ToString(),
                                cell = reader["cell"].ToString(),
                                id = new
                                {
                                    name = reader["id_name"].ToString(),
                                    value = reader["id_value"].ToString()
                                },
                                picture = new
                                {
                                    large = reader["picture_large"].ToString(),
                                    medium = reader["picture_medium"].ToString(),
                                    thumbnail = reader["picture_thumbnail"].ToString()
                                },
                                nat = reader["nat"].ToString()
                            };

                            users.Add(user); //Her bir kullanıcı nesnesi, users listesine ekleniyor.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSuccess = false,
                    message = ex.Message
                });
            }
          

            return Ok(new { isSucces=true,results = users }); //Tüm kullanıcıları içeren results nesnesini döndürüyoruz.
        }
    }
}
