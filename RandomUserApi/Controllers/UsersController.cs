using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;
using RandomUserApi.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace RandomUserApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString;

        public UsersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult GetUsers([FromQuery] string? gender, [FromQuery] int? limit)
        {
            string? userSha256 = null;

            try
            {
                var users = new List<Result>();
                var sql = "SELECT * FROM users";
                var whereClauses = new List<string>();

                if (!string.IsNullOrEmpty(gender))
                    whereClauses.Add("gender = @gender");

                if (whereClauses.Count > 0)
                    sql += " WHERE " + string.Join(" AND ", whereClauses);

                if (limit.HasValue && limit.Value > 0)
                    sql += " LIMIT @limit";

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(gender))
                    cmd.Parameters.AddWithValue("gender", gender);
                if (limit.HasValue && limit.Value > 0)
                    cmd.Parameters.AddWithValue("limit", limit.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    userSha256 = SafeGetString(reader, "login_sha256");

                    var user = new Result
                    {
                        gender = SafeGetString(reader, "gender"),
                        name = new Name
                        {
                            title = SafeGetString(reader, "title"),
                            first = SafeGetString(reader, "first_name"),
                            last = SafeGetString(reader, "last_name")
                        },
                        location = new Location
                        {
                            street = new Street
                            {
                                number = SafeGetInt(reader, "street_number"),
                                name = SafeGetString(reader, "street_name")
                            },
                            city = SafeGetString(reader, "city"),
                            state = SafeGetString(reader, "state"),
                            country = SafeGetString(reader, "country"),
                            postcode = SafeGetString(reader, "postcode"),
                            coordinates = new Coordinates
                            {
                                latitude = SafeGetString(reader, "latitude"),
                                longitude = SafeGetString(reader, "longitude")
                            },
                            timezone = new Timezone
                            {
                                offset = SafeGetString(reader, "timezone_offset"),
                                description = SafeGetString(reader, "timezone_description")
                            }
                        },
                        email = SafeGetString(reader, "email"),
                        login = new Login
                        {
                            uuid = SafeGetString(reader, "login_uuid"),
                            username = SafeGetString(reader, "login_username"),
                            password = SafeGetString(reader, "login_password"),
                            salt = SafeGetString(reader, "login_salt"),
                            md5 = SafeGetString(reader, "login_md5"),
                            sha1 = SafeGetString(reader, "login_sha1"),
                            sha256 = SafeGetString(reader, "login_sha256")
                        },
                        dob = new Dob
                        {
                            date = SafeGetDate(reader, "dob_date"),
                            age = SafeGetInt(reader, "dob_age")
                        },
                        registered = new Registered
                        {
                            date = SafeGetDate(reader, "registered_date"),
                            age = SafeGetInt(reader, "registered_age")
                        },
                        phone = SafeGetString(reader, "phone"),
                        cell = SafeGetString(reader, "cell"),
                        id = new Id
                        {
                            name = SafeGetString(reader, "id_name"),
                            value = SafeGetString(reader, "id_value")
                        },
                        picture = new Picture
                        {
                            large = SafeGetString(reader, "picture_large"),
                            medium = SafeGetString(reader, "picture_medium"),
                            thumbnail = SafeGetString(reader, "picture_thumbnail")
                        },
                        nat = SafeGetString(reader, "nat")
                    };

                    users.Add(user);
                }

                return Ok(new RandomUserResponse { results = users });
            }
            catch (Exception ex)
            {
                try
                {
                    using var conn = new NpgsqlConnection(_connectionString);
                    conn.Open();
                    using var insertCmd = new NpgsqlCommand("INSERT INTO \"Log\" (\"Sha256\", \"ExceptionMessage\") VALUES (@sha256, @exception)", conn);
                    insertCmd.Parameters.AddWithValue("sha256", userSha256 ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("exception", ex.Message);
                    insertCmd.ExecuteNonQuery();
                }
                catch { }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet("{uuid}")]
        public IActionResult GetUser(string uuid)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT * FROM users WHERE login_uuid = @uuid", conn);
                cmd.Parameters.AddWithValue("uuid", uuid);

                using var reader = cmd.ExecuteReader();
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult AddUser([FromBody] UpdateUserDto userDto)
        {
            try
            {
                var sql = @"
                    INSERT INTO users (
                        gender, login_username, title, first_name, last_name,
                        email, phone, login_uuid
                    ) VALUES (
                        @gender, @loginUsername, @title, @firstName, @lastName,
                        @email, @phone, @loginUuid
                    )";

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand(sql, conn);

                // Null veya bos kontrolü ile varsayılan atamalar
                cmd.Parameters.AddWithValue("gender", string.IsNullOrWhiteSpace(userDto.gender) ? "bilinmiyor" : userDto.gender);
                cmd.Parameters.AddWithValue("loginUsername", string.IsNullOrWhiteSpace(userDto.username) ? "anonymous" : userDto.username);
                cmd.Parameters.AddWithValue("title", string.IsNullOrWhiteSpace(userDto.name?.title) ? "" : userDto.name.title);
                cmd.Parameters.AddWithValue("firstName", string.IsNullOrWhiteSpace(userDto.name?.first) ? "" : userDto.name.first);
                cmd.Parameters.AddWithValue("lastName", string.IsNullOrWhiteSpace(userDto.name?.last) ? "" : userDto.name.last);
                cmd.Parameters.AddWithValue("email", string.IsNullOrWhiteSpace(userDto.email) ? "-" : userDto.email);
                cmd.Parameters.AddWithValue("phone", string.IsNullOrWhiteSpace(userDto.phone) ? "-" : userDto.phone);
                cmd.Parameters.AddWithValue("loginUuid", Guid.NewGuid().ToString());

                cmd.ExecuteNonQuery();
                return Ok(new { message = "Kullanıcı başarıyla eklendi" });
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
                using var conn = new NpgsqlConnection(_connectionString);
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

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uuid", uuid);
                cmd.Parameters.AddWithValue("gender", user.gender);
                cmd.Parameters.AddWithValue("loginUsername", user.username);
                cmd.Parameters.AddWithValue("firstName", user.name?.first);
                cmd.Parameters.AddWithValue("lastName", user.name?.last);
                cmd.Parameters.AddWithValue("email", user.email);
                cmd.Parameters.AddWithValue("phone", user.phone);

                int affectedRows = cmd.ExecuteNonQuery();
                if (affectedRows == 0)
                    return NotFound(new { error = "Kullanıcı bulunamadı" });

                return Ok(new { message = "Kullanıcı başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpDelete("{uuid}")]
        public IActionResult DeleteUser(string uuid)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM users WHERE login_uuid = @uuid", conn);
                cmd.Parameters.AddWithValue("uuid", uuid);

                int affectedRows = cmd.ExecuteNonQuery();
                if (affectedRows == 0)
                    return NotFound(new { error = "Kullanıcı bulunamadı" });

                return Ok(new { message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
        //Yardımcı Methodlar 
        private string SafeGetString(NpgsqlDataReader reader, string columnName)
        {
            return reader[columnName] != DBNull.Value ? reader[columnName].ToString() : "";
        }

        private int SafeGetInt(NpgsqlDataReader reader, string columnName)
        {
            return reader[columnName] != DBNull.Value ? Convert.ToInt32(reader[columnName]) : 0;
        }

        private DateTime SafeGetDate(NpgsqlDataReader reader, string columnName)
        {
            return reader[columnName] != DBNull.Value ? Convert.ToDateTime(reader[columnName]) : DateTime.MinValue;
        }

    }
}


