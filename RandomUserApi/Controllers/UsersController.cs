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
                    userSha256 = reader["login_sha256"].ToString();



                    var user = new Result
                    {
                        gender = reader["gender"].ToString(),

                        name = new Name
                        {
                            title = reader["title"] != DBNull.Value ? reader["title"].ToString() : "",
                            first = reader["first_name"].ToString(),
                            last = reader["last_name"].ToString()
                        },
                        location = new Location
                        {
                            street = new Street
                            {
                                number = reader["street_number"] != DBNull.Value ? Convert.ToInt32(reader["street_number"]) : 0,
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
                        gender, login_username, first_name, last_name,
                        email, phone, login_uuid
                    ) VALUES (
                        @gender, @loginUsername, @firstName, @lastName,
                        @email, @phone, @loginUuid
                    )";

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("gender", userDto.gender);
                cmd.Parameters.AddWithValue("loginUsername", userDto.username);
                cmd.Parameters.AddWithValue("firstName", userDto.name?.first);
                cmd.Parameters.AddWithValue("lastName", userDto.name?.last);
                cmd.Parameters.AddWithValue("email", userDto.email);
                cmd.Parameters.AddWithValue("phone", userDto.phone);
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
    }
}
