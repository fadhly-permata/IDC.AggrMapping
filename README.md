# IDC.AggrMapping (.NET 8 Web API Modular Template)

## Overview

IDC.AggrMapping adalah template proyek Web API berbasis .NET 8 yang dirancang modular, scalable, dan mudah dikustomisasi. Template ini mengadopsi dependency injection, konfigurasi berbasis file JSON, serta pemisahan logika melalui partial class dan folder feature.

## Fitur Utama

- **Modular Dependency Injection**:Semua konfigurasi dependency injection terpusat di file `Program.DI.cs` melalui metode `SetupDI()`.

  > [!NOTE]
  > Setiap service, middleware, dan handler didaftarkan secara scoped atau singleton sesuai kebutuhan modul.
  >
- **Konfigurasi Dinamis**:Menggunakan dua file konfigurasi utama:

  - `appconfigs.jsonc` untuk runtime settings, mendukung komentar dan perubahan otomatis.
  - `appsettings.json` untuk environment settings standar ASP.NET.

  > [!TIP]
  > Akses konfigurasi menggunakan dot notation, contoh:
  > `config.Get(path: "Security.Cors.Enabled")`
  >
- **Partial Program Classes**:Setup aplikasi dipisah ke beberapa file partial seperti `Program.cs`, `Program.DI.cs`, `Program.Middlewares.cs`, dan `Program.Services.cs`.

  > [!IMPORTANT]
  > Pemisahan ini memudahkan pengelolaan logika startup dan penambahan fitur baru.
  >
- **Middleware Dinamis**: Berikut adalah daftar middleware yang digunakan pada aplikasi ini:

  1. **Request Logging**: Mencatat setiap permintaan HTTP yang masuk, termasuk path, metode, dan status respons.

     > [!NOTE]
     > Logging dapat dikonfigurasi untuk menulis ke file, atau sistem operasi.
     >
  2. **Rate Limiting**: Membatasi jumlah permintaan dari satu client dalam periode waktu tertentu untuk mencegah abuse.

     > [!TIP]
     > Konfigurasi threshold dan window time dapat diatur melalui `appconfigs.jsonc`.
     >
  3. **Response Compression**: Mengompresi respons HTTP (gzip, brotli) untuk menghemat bandwidth dan mempercepat pengiriman data.

     > [!IMPORTANT]
     > Compression otomatis aktif untuk konten yang mendukung dan dapat dinonaktifkan via konfigurasi.
     >
  4. **Security Headers**: Menambahkan header keamanan seperti `X-Frame-Options`, `X-XSS-Protection`, dan `Content-Security-Policy`.

     > [!WARNING]
     > Header dapat disesuaikan untuk memenuhi standar keamanan aplikasi Anda.
     >
  5. **API Key Authentication**: Melindungi endpoint dengan validasi API Key pada setiap permintaan, kecuali path yang dikecualikan.

     > [!NOTE]
     > Path seperti Swagger UI, CSS, JS, themes, dan images otomatis dikecualikan dari autentikasi.
     >
  6. **Exception Handling**: Menangani error secara global dan mengembalikan respons terstruktur dengan kode dan pesan error.

     > [!TIP]
     > Error log dapat diintegrasikan dengan sistem monitoring eksternal.
     >
  7. **Swagger UI**: Menyediakan dokumentasi interaktif API, mendukung theme switching dan grouping endpoint.

     > [!IMPORTANT]
     > Swagger UI hanya tersedia pada environment tertentu dan dapat dikustomisasi.
     >
  8. **Static File Serving**: Melayani file statis seperti konfigurasi, tema, gambar, dan log dari folder `wwwroot/`.

     > [!NOTE]
     > Path dan akses file statis dapat diatur sesuai kebutuhan aplikasi.
     >

  ---


  > [!NOTE]
  >
  > Semua middleware dapat diaktifkan atau dinonaktifkan melalui konfigurasi di `appconfigs.jsonc`.
  >

  ---
- **Swagger UI**: Mendukung theme switching secara runtime, memungkinkan pengguna memilih tampilan sesuai preferensi. Mendukung grouping endpoint menggunakan atribut `[ApiExplorerSettings(GroupName = "...")]` untuk memudahkan navigasi API. Menyediakan dokumentasi interaktif dengan fitur pencarian, filter, dan try-out langsung pada endpoint. Mendukung custom header dan autentikasi API Key secara otomatis pada permintaan yang relevan. Swagger UI otomatis mengecualikan path seperti CSS, JS, themes, dan images dari API Key Auth.

  > [!TIP]
  > Swagger UI dapat dikustomisasi melalui konfigurasi, termasuk pengaturan tema, logo, dan aksesibilitas endpoint.
  >

- **Plugin System**: Mendukung penambahan, eksekusi, update, dan reload plugin secara dinamis tanpa restart aplikasi. Plugin diimplementasikan sebagai class yang mengimplementasi interface `IPlugin` dan dikelola oleh `PluginManager`.

  > [!NOTE]
  > Plugin dapat ditambahkan dengan mengirimkan source code C# melalui API, dan langsung dikompilasi serta didaftarkan ke sistem.
  >

  - **Plugin API Endpoints (Demo)**: Tersedia endpoint untuk eksekusi plugin (`/api/demo/plugins/HelloWorld`, `/api/demo/plugins/CallOther`), penambahan plugin baru, update source code plugin, reload plugin, dan listing plugin aktif.
  - **Primary Constructor Pattern**: Controller plugin menggunakan primary constructor injection untuk dependency seperti `Language`, `SystemLogging`, dan `PluginManager`.
  - **PluginManager**: Menyediakan method untuk register, add, reload, update, dan instantiate plugin secara dinamis. Mendukung chaining dan null safety.
  - **Plugin Interface (`IPlugin`)**: Setiap plugin wajib mengimplementasikan property `Id`, `Name`, `Version`, serta method `Initialize` dan `Execute`.
  - **Plugin Chaining**: Plugin dapat saling memanggil, contoh pada plugin `CallOtherPlugin` yang mengeksekusi plugin lain (`HelloWorldPlugin`) secara dinamis.
  - **XML Documentation & DocFX Alerts**: Semua method plugin dan manager terdokumentasi lengkap dengan contoh kode, alert, dan penjelasan formal.
  - **Error Handling**: Setiap eksekusi plugin dilengkapi error handling dan logging terintegrasi dengan sistem utama.
  - **Extensible Dependencies**: Plugin dapat menerima dependency seperti logging, language, cache, database, dan HTTP client melalui method `Initialize`.
  - **Auto Persist Plugin Source**: Source code plugin tersimpan otomatis di folder `wwwroot/plugins/source` dan dapat diupdate/reload via API.

  > [!TIP]
  > Plugin dapat digunakan untuk menambah fitur baru tanpa perlu rebuild aplikasi, cocok untuk integrasi eksternal dan prototyping.
  >

- **Dynamic Endpoint Generator**: Mendukung penambahan endpoint API secara dinamis melalui file `endpoint_generator.jsonc` tanpa perlu modifikasi kode.Endpoint baru dapat diatur path, method, response, dan autentikasinya langsung dari konfigurasi.

  > [!NOTE]
  > Fitur ini memudahkan integrasi API baru dan prototyping cepat.
  >
- **Primary Constructor Controllers**:Semua controller menggunakan primary constructor untuk dependency injection yang lebih ringkas dan aman.Controller dipisah per fitur menggunakan partial class, memudahkan pengelolaan dan pengembangan.

  > [!IMPORTANT]
  > Pattern ini meningkatkan testabilitas dan maintainability kode.
  >
- **Auto Persist Config**: Perubahan konfigurasi runtime otomatis tersimpan ke `appconfigs.jsonc` tanpa restart aplikasi. Mendukung rollback dan audit perubahan konfigurasi.

  > [!TIP]
  > Konfigurasi dapat diubah melalui API atau UI admin jika tersedia.
  >
- **Extensible Utility Library**: Menggunakan `IDC.Utilities.dll` sebagai library eksternal untuk helper, extension, dan model umum.Library ini dapat diperluas sesuai kebutuhan proyek dan diintegrasikan secara modular.

  > [!NOTE]
  > Referensi lokal memastikan kompatibilitas dan kontrol versi internal.
  >
- **Comprehensive Error Handling**: Global exception handler mengembalikan respons terstruktur dengan kode dan pesan error yang jelas.Mendukung integrasi dengan sistem monitoring eksternal dan logging detail.

  > [!WARNING]
  > Error log dapat dikonfigurasi untuk dikirim ke file, terminal, dan sistem operasi.
  >
- **Scoped Middleware Registration**: Semua middleware didaftarkan secara scoped untuk efisiensi dan keamanan dependency injection.

  > [!IMPORTANT]
  > Pattern ini mencegah memory leak dan memastikan lifecycle yang tepat.
  >
- **Advanced Configuration Access**: Mendukung akses konfigurasi nested dengan dot notation dan default value.

  > [!TIP]
  > Contoh: `config.Get(path: "Security.Cors.Enabled")`
  >
- **Async Method Variants**: Setiap method penting memiliki versi async dengan callback dan cancellation token.

  > [!NOTE]
  > Tidak mengubah method sync yang sudah ada, menjaga backward compatibility.
  >
- **XML Documentation & DocFX Alerts**: Semua method terdokumentasi dengan XML DocFX style, lengkap dengan alert, contoh kode, dan penjelasan formal.

  > [!TIP]
  > Dokumentasi dapat di-generate otomatis untuk kebutuhan internal dan eksternal.
  >
- **Internal Licensing**: Proyek menggunakan lisensi internal IDC, dengan opsi penggunaan eksternal melalui persetujuan tim pengembang.

  > [!IMPORTANT]
  > Hubungi tim pengembang untuk informasi lisensi dan kontribusi.
  >
- **Auto Persist Config**: Setiap perubahan konfigurasi melalui aplikasi akan otomatis tersimpan ke file `appconfigs.jsonc` tanpa perlu restart.

  > [!NOTE]
  > Fitur ini memastikan konsistensi konfigurasi runtime.
  >
- **Extensible Utility**: Menggunakan library eksternal `IDC.Utilities.dll` yang direferensikan secara lokal dari[`Repository IDC.Utilities`](https://scm.idecision.ai/idecision_source_net8/idc.utility)

  > [!TIP]
  > Library ini menyediakan berbagai helper, extension, dan model yang dapat digunakan di seluruh proyek.
  >

## Installasi

### Langkah Instalasi

1. **Clone Repository**
   Jalankan perintah berikut untuk meng-clone repo:

   ```bash
   git clone https://scm.idecision.ai/idecision_source_net8/IDC.AggrMapping
   ```
2. **Jalankan Installer**
   Pilih installer sesuai OS Anda:

   - Windows:
     ```powershell
     .\installer.ps1
     ```
   - Linux/macOS:
     ```bash
     ./installer.sh
     ```
3. **Ikuti Instruksi Instalasi**
   Ikuti instruksi pada terminal hingga proses instalasi selesai.

> [!IMPORTANT]
> Pastikan semua dependensi dan konfigurasi sudah terpasang sebelum menjalankan aplikasi.

## Directory Structures

```
├── Controllers/               # Partial controllers per feature
├── Utilities/                 # Helpers, Extensions, Models, etc.
├── wwwroot/                   # Static files, configs, themes, logs
├──── appconfigs.jsonc         # Runtime config (with comments)
├── Program.*.cs               # Partial program setup files
├── appsettings.json           # Standard ASP.NET settings
└── endpoint_generator.jsonc   # Dynamic endpoint definitions
```

## Coding Pattern

### C# Codes Pattern

- Selalu gunakan nama argumen pada pemanggilan method:
  `config.Get(path: "app.name", defaultValue: "default")`
- Implementasi null safety & nullable.
- Inisialisasi koleksi secara ringkas.
- Fungsi return class type untuk chaining.
- Controller wajib pakai primary constructor:
  `public class DemoController(SystemLogging systemLogging, Language language)`
- Satu baris perintah tanpa kurung kurawal `{}`.
- Tidak perlu deklarasi variabel jika hanya digunakan sekali.

## Pattern Dokumentasi

- Semua method (termasuk private/internal) wajib XML DocFX style.
- Bahasa Inggris formal, max 100 karakter per baris.
- Sertakan `<summary>`, `<remarks>`, `<example>`, `<code>`, `<returns>`, `<exception>`.
- Gunakan DocFX alert:
  `> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, dll.
- Contoh kode wajib pada `<remarks>`.

## Contoh Penggunaan Konfigurasi

```csharp
// Nested config access dengan dot notation
var isEnabled = _appConfigs.Get<bool>(path: "Security.Cors.Enabled");
var maxItems = _appConfigs.Get(path: "app.settings.maxItems", defaultValue: 100);
```

## Menambah Method Async

- Tambahkan versi async dengan callback & cancellation token.
- Tidak mengubah method sync yang sudah ada.

## Komunikasi

- Penjelasan teknis dalam Bahasa Indonesia jika diperlukan.
- Kode harus jelas, minim penjelasan tambahan.
- Jangan ada kode yang di comment. Ingat, kita sudah menggunakan git sebagai history.

## Referensi Eksternal

- `IDC.Utilities.dll`: [`Repository IDC.Utilities`](https://scm.idecision.ai/idecision_source_net8/idc.utility) adalah referensi mandatory.

## Lisensi

Proyek ini menggunakan lisensi internal IDX Partners. Untuk penggunaan eksternal, silakan hubungi tim pengembang.

---

> [!TIP]
>
> Untuk detail arsitektur, lihat file partial di root dan folder `Controllers/`, serta dokumentasi XML pada setiap method.
