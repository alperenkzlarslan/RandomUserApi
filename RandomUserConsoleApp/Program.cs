using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RandomUserAPI.Model;
using Npgsql;

using HttpClient client = new HttpClient();
string url = "https://randomuser.me/api/?results=500";
string connectionString = "Host=localhost;Username=postgres;Password=alperen4423;Database=postgres";

try
{
    HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    string jsonResponse = await response.Content.ReadAsStringAsync();


    // JSON verisini nesneye dönüştürme
    RandomUserResponse data = JsonSerializer.Deserialize<RandomUserResponse>(jsonResponse);


    // Veriye erişim: kullanıcının adını yazdırıyoruz
    if (data.results.Count > 0)
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (var user in data.results)
        {
            string query = @"INSERT INTO users (
                        gender, email, phone, cell, nat,
                        title, first_name, last_name,
                        street_number, street_name, city, state, country, postcode, latitude, longitude, timezone_offset, timezone_description,
                        login_uuid, login_username, login_password, login_salt, login_md5, login_sha1, login_sha256,
                        dob_date, dob_age,
                        registered_date, registered_age,
                        id_name, id_value,
                        picture_large, picture_medium, picture_thumbnail
                    ) VALUES (
                        @gender, @email, @phone, @cell, @nat,
                        @title, @first, @last,
                        @street_number, @street_name, @city, @state, @country, @postcode, @latitude, @longitude, @timezone_offset, @timezone_description,
                        @login_uuid, @login_username, @login_password, @login_salt, @login_md5, @login_sha1, @login_sha256,
                        @dob_date, @dob_age,
                        @registered_date, @registered_age,
                        @id_name, @id_value,
                        @picture_large, @picture_medium, @picture_thumbnail
                    )";
            using var cmd = new NpgsqlCommand(query, conn);

            // Basit alanlar
            cmd.Parameters.AddWithValue("gender", user.gender);
            cmd.Parameters.AddWithValue("email", user.email);
            cmd.Parameters.AddWithValue("phone", user.phone);
            cmd.Parameters.AddWithValue("cell", user.cell);
            cmd.Parameters.AddWithValue("nat", user.nat);

            // Name alanları
            cmd.Parameters.AddWithValue("title", user.name.title);
            cmd.Parameters.AddWithValue("first", user.name.first);
            cmd.Parameters.AddWithValue("last", user.name.last);

            // Location alanları (örnek olarak)
            cmd.Parameters.AddWithValue("street_number", user.location.street.number);
            cmd.Parameters.AddWithValue("street_name", user.location.street.name);
            cmd.Parameters.AddWithValue("city", user.location.city);
            cmd.Parameters.AddWithValue("state", user.location.state);
            cmd.Parameters.AddWithValue("country", user.location.country);
            cmd.Parameters.AddWithValue("postcode", user.location.postcode.ToString());  // Postcode hem sayı hem de string olabilir
            cmd.Parameters.AddWithValue("latitude", user.location.coordinates.latitude);
            cmd.Parameters.AddWithValue("longitude", user.location.coordinates.longitude);
            cmd.Parameters.AddWithValue("timezone_offset", user.location.timezone.offset);
            cmd.Parameters.AddWithValue("timezone_description", user.location.timezone.description);

            // Login alanları
            cmd.Parameters.AddWithValue("login_uuid", user.login.uuid);
            cmd.Parameters.AddWithValue("login_username", user.login.username);
            cmd.Parameters.AddWithValue("login_password", user.login.password);
            cmd.Parameters.AddWithValue("login_salt", user.login.salt);
            cmd.Parameters.AddWithValue("login_md5", user.login.md5);
            cmd.Parameters.AddWithValue("login_sha1", user.login.sha1);
            cmd.Parameters.AddWithValue("login_sha256", user.login.sha256);

            // Dob alanları
            cmd.Parameters.AddWithValue("dob_date", DateTime.Parse(user.dob.date));
            cmd.Parameters.AddWithValue("dob_age", user.dob.age);

            // Registered alanları
            cmd.Parameters.AddWithValue("registered_date", DateTime.Parse(user.registered.date));
            cmd.Parameters.AddWithValue("registered_age", user.registered.age);

            // ID alanları
            cmd.Parameters.AddWithValue("id_name", user.id.name);
            cmd.Parameters.AddWithValue("id_value", user.id.value ?? (object)DBNull.Value);

            // Picture alanları
            cmd.Parameters.AddWithValue("picture_large", user.picture.large);
            cmd.Parameters.AddWithValue("picture_medium", user.picture.medium);
            cmd.Parameters.AddWithValue("picture_thumbnail", user.picture.thumbnail);

            //SQL komutunu veritabanında çalıştırır, etkilenen satır sayısını döndürür. Asenkron olarak çalışır.
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Veriler başarıyla PostgreSQL'e aktarıldı!");
    }
    else
    {
        Console.WriteLine("API'den veri alınamadı.");
    }

}
catch (Exception ex)
{
    Console.WriteLine("Bir hata oluştu: " + ex.Message);
}
Console.ReadKey();

