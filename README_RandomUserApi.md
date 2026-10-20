# RandomUserApi

.NET 8 ve ASP.NET Core kullanılarak geliştirilen bu proje, kullanıcı verilerini REST API uç noktaları üzerinden yönetmeyi ve PostgreSQL veritabanıyla çalışmayı amaçlar. Veritabanı erişiminde Npgsql kullanılır. Depoda ayrıca RandomUser.me servisinden örnek kullanıcı verileri çekip PostgreSQL'e aktarmak için bir konsol uygulaması bulunur.

## Kullanılan Teknolojiler

- C#
- .NET 8
- ASP.NET Core Web API
- PostgreSQL
- Npgsql
- REST API ve JSON
- Swagger / OpenAPI
- Git ve GitHub

## Özellikler

- Kullanıcı kayıtlarını listeleme
- UUID üzerinden tek kullanıcı getirme
- Kullanıcı ekleme, güncelleme ve silme (CRUD)
- Listeleme isteğinde cinsiyet filtresi ve kayıt limiti kullanabilme
- Parametreli SQL sorgularıyla veritabanı işlemleri
- Geliştirme ortamında Swagger arayüzüyle API uç noktalarını inceleme
- Hata durumlarında günlük tablosuna hata bilgisi kaydetme girişimi
- Konsol uygulamasıyla RandomUser.me API'sinden örnek veri alıp PostgreSQL'e aktarma

## API Uç Noktaları

Temel adres, uygulamayı yerel ortamda çalıştırdığınızda kullandığınız adres olacaktır. Aşağıdaki yollar `api/users` rotasına göre tanımlıdır.

| HTTP yöntemi | Yol | Açıklama |
|---|---|---|
| `GET` | `/api/users` | Kullanıcıları listeler. İsteğe bağlı `gender` ve `limit` sorgu parametreleri alır. |
| `GET` | `/api/users/{uuid}` | UUID değerine göre tek kullanıcı getirir. |
| `POST` | `/api/users/add` | Yeni kullanıcı ekler. JSON gövdesinde kullanıcı bilgileri bekler. |
| `PUT` | `/api/users/{uuid}` | UUID değerine sahip kullanıcıyı günceller. |
| `DELETE` | `/api/users/{uuid}` | UUID değerine sahip kullanıcıyı siler. |

Örnek istekler:

```http
GET /api/users?gender=male&limit=20
GET /api/users/{uuid}
DELETE /api/users/{uuid}
```

Başarılı ve hatalı isteklerde uygun HTTP durum kodları döndürülmeye çalışılır; bulunamayan kayıtlar için `404 Not Found` yanıtı kullanılmaktadır.

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL
- Projede kullanılan tabloları içeren yerel bir veritabanı (`users` ve hata kayıtları için `Log`)

## Kurulum ve Çalıştırma

1. Depoyu bilgisayarınıza klonlayın:

   ```bash
   git clone https://github.com/alperenkzlarslan/RandomUserApi.git
   cd RandomUserApi
   ```

2. PostgreSQL'de proje sorgularıyla uyumlu `users` tablosunu ve hata kaydı için `Log` tablosunu oluşturun.

3. API projesinin veritabanı bağlantı bilgisini güvenli şekilde yapılandırın. Geliştirme ortamında, `RandomUserApi` proje klasöründe şu komutları kullanabilirsiniz:

   ```bash
   cd RandomUserApi
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
   dotnet run
   ```

   `YOUR_DATABASE`, `YOUR_USERNAME` ve `YOUR_PASSWORD` alanlarını kendi yerel PostgreSQL bilgilerinizle değiştirin. Gerçek bağlantı bilgilerini kaynak koduna veya Git'e gönderilen yapılandırma dosyalarına eklemeyin.

4. Uygulama geliştirme ortamında çalışırken Swagger arayüzünü `/swagger` yolundan açarak uç noktaları test edebilirsiniz.

> Not: Konsol veri aktarma uygulamasını çalıştırmadan önce onun veritabanı bağlantısını da güvenli yapılandırmaya taşıyın. Konsol projesindeki bağlantı dizesi kaynak kodunda sabit tutulmamalıdır.

## Güvenlik Notu

Veritabanı parolaları, API anahtarları ve diğer gizli bilgiler Git deposuna eklenmemelidir. Bu depoyu kullanmadan önce uygulama ve konsol projesindeki bağlantı bilgilerini gözden geçirin; daha önce paylaşılmış gerçek bir parola varsa değiştirin. Yalnızca dosyadan silmek, gizli bilginin Git geçmişinden kaldırıldığı anlamına gelmez.

## Geliştirme Hedefi

Bu proje; C#, ASP.NET Core Web API, REST uç noktaları, PostgreSQL bağlantısı, temel CRUD işlemleri ve hata yönetimi konularında uygulamalı çalışma amacıyla geliştirilmiştir.

## Geliştirici

**Alperen Kızılarslan**  
GitHub: [alperenkzlarslan](https://github.com/alperenkzlarslan)
