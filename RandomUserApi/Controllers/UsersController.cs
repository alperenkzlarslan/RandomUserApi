using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace RandomUserApi.Controllers
{
    [Route("api/[controller]")] //API'nin URL formatını belirler. Bu: /api/users şeklinde bir endpoint oluşturur.
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet] //Bu metod HTTP GET isteklerini işler.
        public IActionResult GetUsers([FromQuery] string? gender, [FromQuery] int? limit) // API, JSON yanıt döndüreceği için genel bir dönüş türü kullanıyoruz
        {
            string? userSha256 = null;

            try
            {
                var users = new List<User>();
                var sql = "SELECT * FROM users"; // Temel sorgu

                // Koşullar için bir liste oluşturalım
                var whereClauses = new List<string>();

                // Gender parametresi varsa
                if (!string.IsNullOrEmpty(gender))
                {
                    whereClauses.Add("gender = @gender");
                }

                // Eğer herhangi bir filtre varsa "WHERE" ekleyip birleştiriyoruz
                if (whereClauses.Any())
                {
                    sql += " WHERE " + string.Join(" AND ", whereClauses);
                }

                // Limit parametresi varsa
                if (limit.HasValue && limit.Value > 0)
                {
                    sql += " LIMIT @limit";
                }

                using (var conn = new NpgsqlConnection(_connectionString)) //nesnesi, PostgreSQL veritabanına bağlantı açıyor. Veritabanı bağlantısını açıyor.
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        // Gender parametresi eklenecekse
                        if (!string.IsNullOrEmpty(gender))
                        {
                            cmd.Parameters.AddWithValue("gender", gender);
                        }

                        // Limit parametresi eklenecekse
                        if (limit.HasValue && limit.Value > 0)
                        {
                            cmd.Parameters.AddWithValue("limit", limit.Value);
                        }

                        using (var reader = cmd.ExecuteReader()) //Sorgunun sonucunu okuyan bir reader (okuyucu) oluşturuyor.
                        {
                            while (reader.Read()) //Tüm satırları tek tek okuyoruz.
                            {
                                // Hata oluşmadan önce login_sha256 bilgisini alalım.
                                userSha256 = reader["login_sha256"].ToString();

                                var user = new User //JSON nesnesini oluşturuyoruz.
                                {
                                    Gender = reader["gender"].ToString(),
                                    Name = new Name
                                    {
                                        Title = reader["title"].ToString(),
                                        First = reader["first_name"].ToString(),
                                        Last = reader["last_name"].ToString()
                                    },
                                    Location = new Location
                                    {
                                        Street = new Street
                                        {
                                            Number = reader["street_number"],
                                            Name = reader["street_name"].ToString()
                                        },
                                        City = reader["city"].ToString(),
                                        State = reader["state"].ToString(),
                                        Country = reader["country"].ToString(),
                                        Postcode = reader["postcode"],
                                        Coordinates = new Coordinates
                                        {
                                            Latitude = reader["latitude"].ToString(),
                                            Longitude = reader["longitude"].ToString()
                                        },
                                        Timezone = new Timezone
                                        {
                                            Offset = reader["timezone_offset"].ToString(),
                                            Description = reader["timezone_description"].ToString()
                                        }
                                    },
                                    Email = reader["email"].ToString(),
                                    Login = new Login
                                    {
                                        Uuid = reader["login_uuid"].ToString(),
                                        Username = reader["login_username"].ToString(),
                                        Password = reader["login_password"].ToString(),
                                        Salt = reader["login_salt"].ToString(),
                                        Md5 = reader["login_md5"].ToString(),
                                        Sha1 = reader["login_sha1"].ToString(),
                                        Sha256 = reader["login_sha256"].ToString()
                                    },
                                    Dob = new Dob
                                    {
                                        Date = reader["dob_date"].ToString(),
                                        Age = reader["dob_age"]
                                    },
                                    Registered = new Registered
                                    {
                                        Date = reader["registered_date"].ToString(),
                                        Age = reader["registered_age"]
                                    },
                                    Phone = reader["phone"].ToString(),
                                    Cell = reader["cell"].ToString(),
                                    Id = new Id
                                    {
                                        Name = reader["id_name"].ToString(),
                                        Value = reader["id_value"].ToString()
                                    },
                                    Picture = new Picture
                                    {
                                        Large = reader["picture_large"].ToString(),
                                        Medium = reader["picture_medium"].ToString(),
                                        Thumbnail = reader["picture_thumbnail"].ToString()
                                    },
                                    Nat = reader["nat"].ToString()
                                };
                                users.Add(user); //Her bir kullanıcı nesnesi, users listesine ekleniyor.
                            }
                        }
                    }
                }
                return Ok(new { results = users }); //Tüm kullanıcıları içeren results nesnesini döndürüyoruz.
            }
            catch (Exception ex)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        // Log tablosunun adı SQL'de "Log" gibi reserved kelimeler içerebileceğinden çift tırnakla yazmak iyi bir uygulamadır.
                        var insertCmd = new NpgsqlCommand("INSERT INTO \"Log\" (\"Sha256\", \"ExceptionMessage\") VALUES (@sha256, @exception)", conn);
                        insertCmd.Parameters.AddWithValue("sha256", userSha256 ?? string.Empty);
                        insertCmd.Parameters.AddWithValue("exception", ex.Message);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Log kaydı sırasında da bir hata oluşursa bu hatayı yutuyoruz. (İsteğe bağlı olarak loglama yapılabilir)
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] User user)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var sql = @"
                        INSERT INTO users (
                            gender, title, first_name, last_name,
                            street_number, street_name, city, state, country, postcode,
                            latitude, longitude, timezone_offset, timezone_description,
                            email, login_uuid, login_username, login_password,
                            login_salt, login_md5, login_sha1, login_sha256,
                            dob_date, dob_age, registered_date, registered_age,
                            phone, cell, id_name, id_value,
                            picture_large, picture_medium, picture_thumbnail, nat
                        ) VALUES (
                            @gender, @title, @firstName, @lastName,
                            @streetNumber, @streetName, @city, @state, @country, @postcode,
                            @latitude, @longitude, @timezoneOffset, @timezoneDescription,
                            @email, @loginUuid, @loginUsername, @loginPassword,
                            @loginSalt, @loginMd5, @loginSha1, @loginSha256,
                            @dobDate, @dobAge, @registeredDate, @registeredAge,
                            @phone, @cell, @idName, @idValue,
                            @pictureLarge, @pictureMedium, @pictureThumbnail, @nat
                        )";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        // Temel bilgiler
                        cmd.Parameters.AddWithValue("gender", user.Gender);
                        cmd.Parameters.AddWithValue("title", user.Name.Title);
                        cmd.Parameters.AddWithValue("firstName", user.Name.First);
                        cmd.Parameters.AddWithValue("lastName", user.Name.Last);

                        // Adres bilgileri
                        cmd.Parameters.AddWithValue("streetNumber", user.Location.Street.Number);
                        cmd.Parameters.AddWithValue("streetName", user.Location.Street.Name);
                        cmd.Parameters.AddWithValue("city", user.Location.City);
                        cmd.Parameters.AddWithValue("state", user.Location.State);
                        cmd.Parameters.AddWithValue("country", user.Location.Country);
                        cmd.Parameters.AddWithValue("postcode", user.Location.Postcode);
                        
                        // Koordinat bilgileri
                        cmd.Parameters.AddWithValue("latitude", user.Location.Coordinates.Latitude);
                        cmd.Parameters.AddWithValue("longitude", user.Location.Coordinates.Longitude);
                        cmd.Parameters.AddWithValue("timezoneOffset", user.Location.Timezone.Offset);
                        cmd.Parameters.AddWithValue("timezoneDescription", user.Location.Timezone.Description);

                        // Kullanıcı bilgileri
                        cmd.Parameters.AddWithValue("email", user.Email);
                        cmd.Parameters.AddWithValue("loginUuid", user.Login.Uuid);
                        cmd.Parameters.AddWithValue("loginUsername", user.Login.Username);
                        cmd.Parameters.AddWithValue("loginPassword", user.Login.Password);
                        cmd.Parameters.AddWithValue("loginSalt", user.Login.Salt);
                        cmd.Parameters.AddWithValue("loginMd5", user.Login.Md5);
                        cmd.Parameters.AddWithValue("loginSha1", user.Login.Sha1);
                        cmd.Parameters.AddWithValue("loginSha256", user.Login.Sha256);

                        // Tarih bilgileri
                        cmd.Parameters.AddWithValue("dobDate", user.Dob.Date);
                        cmd.Parameters.AddWithValue("dobAge", user.Dob.Age);
                        cmd.Parameters.AddWithValue("registeredDate", user.Registered.Date);
                        cmd.Parameters.AddWithValue("registeredAge", user.Registered.Age);

                        // İletişim bilgileri
                        cmd.Parameters.AddWithValue("phone", user.Phone);
                        cmd.Parameters.AddWithValue("cell", user.Cell);
                        cmd.Parameters.AddWithValue("idName", user.Id.Name);
                        cmd.Parameters.AddWithValue("idValue", user.Id.Value);

                        // Resim bilgileri
                        cmd.Parameters.AddWithValue("pictureLarge", user.Picture.Large);
                        cmd.Parameters.AddWithValue("pictureMedium", user.Picture.Medium);
                        cmd.Parameters.AddWithValue("pictureThumbnail", user.Picture.Thumbnail);
                        cmd.Parameters.AddWithValue("nat", user.Nat);

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var insertCmd = new NpgsqlCommand("INSERT INTO \"Log\" (\"Sha256\", \"ExceptionMessage\") VALUES (@sha256, @exception)", conn);
                        insertCmd.Parameters.AddWithValue("sha256", user.Login?.Sha256 ?? string.Empty);
                        insertCmd.Parameters.AddWithValue("exception", ex.Message);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Log hatası durumunda sessizce devam et
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
