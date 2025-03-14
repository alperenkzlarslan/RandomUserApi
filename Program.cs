using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// Random User API'sinin ana yapısı
public class RandomUserResponse
{
    public List<Result> results { get; set; }
    public Info info { get; set; }
}

public class Result
{
    public string gender { get; set; }
    public Name name { get; set; }
    // Daha fazla alan ekleyebilirsin
}

public class Name
{
    public string title { get; set; }
    public string first { get; set; }
    public string last { get; set; }
}

public class Info
{
    public string seed { get; set; }
    public int results { get; set; }
    public int page { get; set; }
    public string version { get; set; }
}

class Program
{
    static async Task Main()
    {
        using HttpClient client = new HttpClient();
        string url = "https://randomuser.me/api/?results=5";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();

            // JSON verisini nesneye dönüştürme
            RandomUserResponse data = JsonSerializer.Deserialize<RandomUserResponse>(jsonResponse);

            // Veriye erişim: İlk kullanıcının adını yazdırıyoruz
            
            if (data.results.Count > 0)
            {
                for (int i = 0; i < data.results; i++)
                {
                    var firstUser = data.results[i];
                    Console.WriteLine($"Kullanıcı: {firstUser.name.title} {firstUser.name.first} {firstUser.name.last}");
                }
                        
            }
            
            
        }
        catch (Exception ex)
        {
            Console.WriteLine("Bir hata oluştu: " + ex.Message);
        }
    }
}
