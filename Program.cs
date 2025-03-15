using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RandomUserAPI.Model;
using Npgsql;

using HttpClient client = new HttpClient();
string url = "https://randomuser.me/api/?results=5";
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
            string query = "INSERT INTO users (title, first_name, last_name, gender) VALUES (@title, @first, @last, @gender)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("title", user.name.title);
            cmd.Parameters.AddWithValue("first", user.name.first);
            cmd.Parameters.AddWithValue("last", user.name.last);
            cmd.Parameters.AddWithValue("gender", user.gender);

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
