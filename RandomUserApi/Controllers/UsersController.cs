using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;
using RandomUserApi.Model;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
                var users = new List<Result>();
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

                                var user = new Result
                                {
                                    gender = reader["gender"].ToString(),
                                    name = new Name
                                    {
                                        title = reader["title"].ToString(),
                                        first = reader["first_name"].ToString(),
                                        last = reader["last_name"].ToString()
                                    },
                                    location = new Location
                                    {
                                        street = new Street
                                        {
                                            number = Convert.ToInt32(reader["street_number"]),
                                            name = reader["street_name"].ToString()
                                        },
                                        city = reader["city"].ToString(),
                                        state = reader["state"].ToString(),
                                        country = reader["country"].ToString(),
                                        postcode = reader["postcode"].ToString(),
                                        coordinates = new Coordinates
                                        {
                                            latitude = reader["latitude"].ToString(),
                                            longitude = reader["longitude"].ToString()
                                        },
                                        timezone = new Timezone
                                        {
                                            offset = reader["timezone_offset"].ToString(),
                                            description = reader["timezone_description"].ToString()
                                        }
                                    },
                                    email = reader["email"].ToString(),
                                    login = new Login
                                    {
                                        uuid = reader["login_uuid"].ToString(),
                                        username = reader["login_username"].ToString(),
                                        password = reader["login_password"].ToString(),
                                        salt = reader["login_salt"].ToString(),
                                        md5 = reader["login_md5"].ToString(),
                                        sha1 = reader["login_sha1"].ToString(),
                                        sha256 = reader["login_sha256"].ToString()
                                    },
                                    dob = new Dob
                                    {
                                        date = Convert.ToDateTime(reader["dob_date"]),
                                        age = Convert.ToInt32(reader["dob_age"])
                                    },
                                    registered = new Registered
                                    {
                                        date = Convert.ToDateTime(reader["registered_date"]),
                                        age = Convert.ToInt32(reader["registered_age"])
                                    },
                                    phone = reader["phone"].ToString(),
                                    cell = reader["cell"].ToString(),
                                    id = new Id
                                    {
                                        name = reader["id_name"].ToString(),
                                        value = reader["id_value"].ToString()
                                    },
                                    picture = new Picture
                                    {
                                        large = reader["picture_large"].ToString(),
                                        medium = reader["picture_medium"].ToString(),
                                        thumbnail = reader["picture_thumbnail"].ToString()
                                    },
                                    nat = reader["nat"].ToString(),
                                };
                                users.Add(user); //Her bir kullanıcı nesnesi, users listesine ekleniyor.
                            }
                        }
                    }
                }
                return Ok(new RandomUserResponse { results = users }); //Tüm kullanıcıları içeren results nesnesini döndürüyoruz.
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
        public IActionResult CreateUser([FromBody] Result user)
        {
            if (user == null)
            {
                return BadRequest(new { error = "User data is required" });
            }

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
                        cmd.Parameters.AddWithValue("gender", user.gender);
                        cmd.Parameters.AddWithValue("title", user.name?.title);
                        cmd.Parameters.AddWithValue("firstName", user.name?.first);
                        cmd.Parameters.AddWithValue("lastName", user.name?.last);

                        // Adres bilgileri
                        cmd.Parameters.AddWithValue("streetNumber", user.location?.street?.number);
                        cmd.Parameters.AddWithValue("streetName", user.location?.street?.name);
                        cmd.Parameters.AddWithValue("city", user.location?.city);
                        cmd.Parameters.AddWithValue("state", user.location?.state);
                        cmd.Parameters.AddWithValue("country", user.location?.country);
                        cmd.Parameters.AddWithValue("postcode", user.location?.postcode);
                        
                        // Koordinat bilgileri
                        cmd.Parameters.AddWithValue("latitude", user.location?.coordinates?.latitude);
                        cmd.Parameters.AddWithValue("longitude", user.location?.coordinates?.longitude);
                        cmd.Parameters.AddWithValue("timezoneOffset", user.location?.timezone?.offset);
                        cmd.Parameters.AddWithValue("timezoneDescription", user.location?.timezone?.description);

                        // Kullanıcı bilgileri
                        cmd.Parameters.AddWithValue("email", user.email);
                        cmd.Parameters.AddWithValue("loginUuid", user.login?.uuid);
                        cmd.Parameters.AddWithValue("loginUsername", user.login?.username);
                        cmd.Parameters.AddWithValue("loginPassword", user.login?.password);
                        cmd.Parameters.AddWithValue("loginSalt", user.login?.salt);
                        cmd.Parameters.AddWithValue("loginMd5", user.login?.md5);
                        cmd.Parameters.AddWithValue("loginSha1", user.login?.sha1);
                        cmd.Parameters.AddWithValue("loginSha256", user.login?.sha256);

                        // Tarih bilgileri
                        cmd.Parameters.AddWithValue("dobDate", user.dob?.date);
                        cmd.Parameters.AddWithValue("dobAge", user.dob?.age);
                        cmd.Parameters.AddWithValue("registeredDate", user.registered?.date);
                        cmd.Parameters.AddWithValue("registeredAge", user.registered?.age);

                        // İletişim bilgileri
                        cmd.Parameters.AddWithValue("phone", user.phone);
                        cmd.Parameters.AddWithValue("cell", user.cell);
                        cmd.Parameters.AddWithValue("idName", user.id?.name);
                        cmd.Parameters.AddWithValue("idValue", user.id?.value);

                        // Resim bilgileri
                        cmd.Parameters.AddWithValue("pictureLarge", user.picture?.large);
                        cmd.Parameters.AddWithValue("pictureMedium", user.picture?.medium);
                        cmd.Parameters.AddWithValue("pictureThumbnail", user.picture?.thumbnail);
                        cmd.Parameters.AddWithValue("nat", user.nat);

                        cmd.ExecuteNonQuery();
                    }
                }

                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var insertCmd = new NpgsqlCommand("INSERT INTO \"Log\" (\"Sha256\", \"ExceptionMessage\") VALUES (@sha256, @message)", conn);
                        insertCmd.Parameters.AddWithValue("sha256", user.login?.sha256 ?? string.Empty);
                        insertCmd.Parameters.AddWithValue("message", "User created successfully");
                        insertCmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Log hatası durumunda sessizce devam et
                }

                return StatusCode(StatusCodes.Status201Created, new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var insertCmd = new NpgsqlCommand("INSERT INTO \"Log\" (\"Sha256\", \"ExceptionMessage\") VALUES (@sha256, @exception)", conn);
                        insertCmd.Parameters.AddWithValue("sha256", user.login?.sha256 ?? string.Empty);
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

        [HttpDelete("{uuid}")]
        public IActionResult DeleteUser(string uuid)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var sql = "DELETE FROM users WHERE login_uuid = @uuid";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("uuid", uuid);
                        int affectedRows = cmd.ExecuteNonQuery();
                        
                        if (affectedRows == 0)
                        {
                            return NotFound(new { error = "Kullanıcı bulunamadı" });
                        }
                    }
                }
                return Ok(new { message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPut("{uuid}")]
        public async Task<IActionResult> UpdateUser(string uuid, [FromBody] UpdateUserDto user)
        {
            if (user == null ||
                string.IsNullOrEmpty(user.gender) ||
                user.name == null ||
                string.IsNullOrEmpty(user.username) ||
                string.IsNullOrEmpty(user.name.first) ||
                string.IsNullOrEmpty(user.name.last) ||
                string.IsNullOrEmpty(user.email) ||
                string.IsNullOrEmpty(user.phone))
            {
                return BadRequest(new { error = "Tüm alanlar zorunludur!" });
            }

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var sql = @"
                        UPDATE users SET 
                            gender = @gender,
                            login_username = @loginUsername,
                            first_name = @firstName,
                            last_name = @lastName,
                            email = @email,
                            phone = @phone
                        WHERE login_uuid = @uuid";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("uuid", uuid);
                        cmd.Parameters.AddWithValue("gender", user.gender);
                        cmd.Parameters.AddWithValue("loginUsername", user.username);
                        cmd.Parameters.AddWithValue("firstName", user.name?.first);
                        cmd.Parameters.AddWithValue("lastName", user.name?.last);
                        cmd.Parameters.AddWithValue("email", user.email);
                        cmd.Parameters.AddWithValue("phone", user.phone);

                        int affectedRows = cmd.ExecuteNonQuery();
                        
                        if (affectedRows == 0)
                        {
                            return NotFound(new { error = "Kullanıcı bulunamadı" });
                        }
                    }
                }
                return Ok(new { message = "Kullanıcı başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet("{uuid}")]
        public IActionResult GetUser(string uuid)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var sql = "SELECT * FROM users WHERE login_uuid = @uuid";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("uuid", uuid);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new Result
                                {
                                    gender = reader["gender"].ToString(),
                                    name = new Name
                                    {
                                        first = reader["first_name"].ToString(),
                                        last = reader["last_name"].ToString()
                                    },
                                    email = reader["email"].ToString(),
                                    phone = reader["phone"].ToString(),
                                    login = new Login
                                    {
                                        username = reader["login_username"].ToString(),
                                        uuid = reader["login_uuid"].ToString()
                                    }
                                };
                                return Ok(user);
                            }
                            return NotFound(new { error = "Kullanıcı bulunamadı" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
