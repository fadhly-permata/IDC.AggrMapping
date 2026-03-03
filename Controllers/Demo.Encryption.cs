using IDC.Utilities;
using IDC.Utilities.IO;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing demo operations
/// </summary>
/// <remarks>
/// Provides endpoints for system logging and other demo functionalities
/// </remarks>
/// <example>
/// <code>
/// var controller = new DemoController(new SystemLogging());
/// controller.LogInfo(message: "Test message");
/// </code>
/// </example>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoEncryption(SystemLogging systemLogging) : ControllerBase
{
    private const string CON_ENC_KEY = "1234567890123456";
    private const string CON_ENC_VECTOR = "2345678901234567";
    private const string CON_CONTENT_TYPE_APP_OCTET_STREAM = "application/octet-stream";
    private const string CON_ENC_KEY_2 = "12345678";
    private readonly FileEncryption _fileEncryption = new(systemLogging: systemLogging);

    private static (string outputFilename, string outputFullPath) EncodingPathBuilder(
        string fileName,
        bool isEncrypt = true
    )
    {
        var encryptedDir = Path.Combine(
            path1: Directory.GetCurrentDirectory(),
            path2: "wwwroot",
            path3: "encoded",
            path4: isEncrypt ? "encrypted" : "decrypted"
        );
        Directory.CreateDirectory(path: encryptedDir);

        var outputFilename =
            $"{Path.GetFileNameWithoutExtension(path: fileName)}.{(isEncrypt ? "encrypted" : "decrypted")}{Path.GetExtension(path: fileName)}";
        return (outputFilename, Path.Combine(path1: encryptedDir, path2: outputFilename));
    }

    /// <summary>
    /// Processes file upload for encryption or decryption operations
    /// </summary>
    /// <remarks>
    /// Handles the upload of files to temporary storage and prepares paths for encryption/decryption operations.
    /// The method creates necessary directories and generates appropriate file names.
    ///
    /// > [!IMPORTANT]
    /// > Ensure sufficient disk space in the wwwroot/encoded directory
    ///
    /// > [!NOTE]
    /// > Files are temporarily stored in wwwroot/encoded/raw before processing
    ///
    /// Example usage:
    /// <code>
    /// var file = Request.Form.Files[0];
    /// var (filePath, encryptedFileName, encryptedFilePath) = await UploadProcess(
    ///     file: file,
    ///     isEncrypt: true,
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    /// </remarks>
    /// <param name="file">The uploaded file from the request</param>
    /// <param name="isEncrypt">Determines if the operation is encryption (true) or decryption (false)</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>
    /// A tuple containing:
    /// - filePath: The path where the original file is stored
    /// - encryptedFileName: The name of the processed file
    /// - outputFilePath: The full path where the processed file will be stored
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to directory is denied</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iformfile">IFormFile Interface</seealso>
    private static async Task<(
        string sourceFilePath,
        string outputFilename,
        string outputFullPath
    )> UploadProcess(
        IFormFile file,
        bool isEncrypt = true,
        CancellationToken cancellationToken = default
    )
    {
        var uploadDir = Path.Combine(
            path1: Directory.GetCurrentDirectory(),
            path2: "wwwroot",
            path3: "encoded",
            path4: "raw"
        );
        Directory.CreateDirectory(path: uploadDir);

        var fileName = Path.GetFileName(path: file.FileName);
        var filePath = Path.Combine(path1: uploadDir, path2: fileName);

        using var stream = new FileStream(
            path: filePath,
            mode: FileMode.Create,
            access: FileAccess.Write,
            share: FileShare.None
        );

        await file.CopyToAsync(target: stream, cancellationToken: cancellationToken);

        var (outputFilename, outputFullPath) = EncodingPathBuilder(
            fileName: fileName,
            isEncrypt: isEncrypt
        );

        var encryptedDir = Path.GetDirectoryName(path: outputFullPath)!;
        Directory.CreateDirectory(path: encryptedDir);

        return (
            filePath,
            outputFilename,
            outputFullPath: Path.Combine(encryptedDir, outputFilename)
        );
    }

    /// <summary>
    /// Encrypts a file using AES (Advanced Encryption Standard) encryption algorithm
    /// </summary>
    /// <remarks>
    /// This endpoint handles file encryption using AES-128 encryption with a fixed key and IV.
    /// The process includes:
    /// 1. Uploading the file to a temporary location
    /// 2. Encrypting the file using AES encryption
    /// 3. Returning the encrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > The key and IV are hardcoded for demo purposes. In production, use secure key management.
    ///
    /// > [!NOTE]
    /// > The encrypted file will be saved with '.encrypted' appended to its name
    ///
    /// Example usage:
    /// <code>
    /// using var fileStream = File.OpenRead("myfile.txt");
    /// var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", "myfile.txt");
    /// var result = await AESEncryptFile(file: formFile, cancellationToken: CancellationToken.None);
    /// </code>
    /// </remarks>
    /// <param name="file">The file to be encrypted. Must be a valid file with read permissions.</param>
    /// <param name="cancellationToken">Token to cancel the encryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the encrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if encryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when file is invalid or empty</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes">AES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "AES/Files/Encrypt")]
    public async Task<IActionResult> AESEncryptFile(
        IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            (string sourceFile, string encryptedFileName, string encryptedFilePath) =
                await UploadProcess(
                    file: file,
                    isEncrypt: true,
                    cancellationToken: cancellationToken
                );

            await _fileEncryption.AESFileEncryption(
                sourceFileLocation: sourceFile,
                destFileLocation: encryptedFilePath,
                key: CON_ENC_KEY,
                iv: CON_ENC_VECTOR,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: encryptedFilePath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: encryptedFileName
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Decrypts a file that was previously encrypted using AES (Advanced Encryption Standard) algorithm
    /// </summary>
    /// <remarks>
    /// This endpoint handles file decryption using AES-128 decryption with a fixed key and IV.
    /// The process includes:
    /// 1. Uploading the encrypted file to a temporary location
    /// 2. Decrypting the file using AES decryption
    /// 3. Returning the decrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > The key and IV must match those used for encryption. In production, use secure key management.
    ///
    /// > [!CAUTION]
    /// > Attempting to decrypt a file that wasn't encrypted with AES will result in corruption
    ///
    /// > [!NOTE]
    /// > The decrypted file will be saved with '.decrypted' appended to its name
    ///
    /// Example usage:
    /// <code>
    /// using var fileStream = File.OpenRead("encrypted_file.txt.encrypted");
    /// var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", "encrypted_file.txt.encrypted");
    /// var result = await AESDecryptFile(
    ///     file: formFile,
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    /// </remarks>
    /// <param name="file">The encrypted file to be decrypted. Must be a valid AES-encrypted file.</param>
    /// <param name="cancellationToken">Token to cancel the decryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the decrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if decryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when file is invalid or empty</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes">AES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "AES/Files/Decrypt")]
    public async Task<IActionResult> AESDecryptFile(
        IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            (string filePath, string encryptedFileName, string encryptedFilePath) =
                await UploadProcess(
                    file: file,
                    isEncrypt: false,
                    cancellationToken: cancellationToken
                );

            await _fileEncryption.AESFileDecryption(
                sourceFileLocation: filePath,
                destFileLocation: encryptedFilePath,
                key: CON_ENC_KEY,
                iv: CON_ENC_VECTOR,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: encryptedFilePath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: encryptedFileName
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Encrypts a string input and saves it to a file using AES (Advanced Encryption Standard) encryption
    /// </summary>
    /// <remarks>
    /// This endpoint converts a plain text string into an encrypted file using AES-128 encryption with a fixed key and IV.
    /// The process includes:
    /// 1. Converting the input string to bytes
    /// 2. Encrypting the bytes using AES encryption
    /// 3. Saving the encrypted content to a file
    /// 4. Returning the encrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > The key and IV are hardcoded for demo purposes. In production, use secure key management.
    ///
    /// > [!NOTE]
    /// > The encrypted file will be saved as 'encrypted.txt' in the wwwroot/encoded/encrypted directory
    ///
    /// > [!CAUTION]
    /// > The maximum string length that can be encrypted depends on available memory
    ///
    /// Example usage:
    /// <code>
    /// var result = await AESEncryptStringToFile(
    ///     plainText: "Hello, World!",
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    ///
    /// Example request body:
    /// <code>
    /// {
    ///     "plainText": "This is a secret message"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="plainText">The string to be encrypted. Must not be null or empty.</param>
    /// <param name="cancellationToken">Token to cancel the encryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the encrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if encryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when input string is null or empty</exception>
    /// <exception cref="OutOfMemoryException">Thrown when input string is too large to process</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes">AES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "AES/StringToFile/Encrypt")]
    public async Task<IActionResult> AESEncryptStringToFile(
        string plainText,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (_, outputFullPath) = EncodingPathBuilder(fileName: "encoded.txt", isEncrypt: true);

            await _fileEncryption.AESStringToFileEncryption(
                plainText: plainText,
                destFileLocation: outputFullPath,
                key: CON_ENC_KEY,
                iv: CON_ENC_VECTOR,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: outputFullPath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: "encrypted.txt"
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted string input and saves it to a file using AES (Advanced Encryption Standard) decryption
    /// </summary>
    /// <remarks>
    /// This endpoint converts an encrypted string back to its original form using AES-128 decryption with a fixed key and IV.
    /// The process includes:
    /// 1. Converting the encrypted input string to bytes
    /// 2. Decrypting the bytes using AES decryption
    /// 3. Saving the decrypted content to a file
    /// 4. Returning the decrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > The key and IV must match those used for encryption. In production, use secure key management.
    ///
    /// > [!CAUTION]
    /// > Attempting to decrypt a string that wasn't encrypted with AES will result in corruption
    ///
    /// > [!NOTE]
    /// > The decrypted file will be saved as 'decrypted.txt' in the wwwroot/encoded/decrypted directory
    ///
    /// Example usage:
    /// <code>
    /// var result = await AESDecryptStringToFile(
    ///     encryptedText: "AQIDBAUGBwgJCgsMDQ4PEA==",
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    ///
    /// Example request body:
    /// <code>
    /// {
    ///     "encryptedText": "AQIDBAUGBwgJCgsMDQ4PEA=="
    /// }
    /// </code>
    /// </remarks>
    /// <param name="encryptedText">The encrypted string to be decrypted. Must be a valid base64 encoded string.</param>
    /// <param name="cancellationToken">Token to cancel the decryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the decrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if decryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when input string is not a valid base64 string</exception>
    /// <exception cref="FormatException">Thrown when the encrypted text is not in the correct format</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes">AES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "AES/StringToFile/Decrypt")]
    public async Task<IActionResult> AESDecryptStringToFile(
        string encryptedText,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (_, outputFullPath) = EncodingPathBuilder(
                fileName: "decoded.txt",
                isEncrypt: false
            );

            await _fileEncryption.AESStringToFileDecryption(
                encryptedText: encryptedText,
                destFileLocation: outputFullPath,
                key: CON_ENC_KEY,
                iv: CON_ENC_VECTOR,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: outputFullPath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: "decrypted.txt"
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Encrypts a file using DES (Data Encryption Standard) encryption algorithm
    /// </summary>
    /// <remarks>
    /// This endpoint handles file encryption using DES encryption with a fixed 8-byte key and IV.
    /// The process includes:
    /// 1. Uploading the file to a temporary location
    /// 2. Encrypting the file using DES encryption
    /// 3. Returning the encrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > DES is considered cryptographically weak and should not be used in production environments.
    /// > The key and IV are hardcoded for demo purposes. In production, use secure key management.
    ///
    /// > [!WARNING]
    /// > DES has been deprecated due to its vulnerability to brute force attacks.
    /// > Consider using AES for secure encryption.
    ///
    /// > [!NOTE]
    /// > The encrypted file will be saved with '.encrypted' appended to its name
    ///
    /// Example usage:
    /// <code>
    /// using var fileStream = File.OpenRead("myfile.txt");
    /// var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", "myfile.txt");
    /// var result = await DESEncryptFile(file: formFile, cancellationToken: CancellationToken.None);
    /// </code>
    /// </remarks>
    /// <param name="file">The file to be encrypted. Must be a valid file with read permissions.</param>
    /// <param name="cancellationToken">Token to cancel the encryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the encrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if encryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when file is invalid or empty</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.des">DES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "DES/Files/Encrypt")]
    public async Task<IActionResult> DESEncryptFile(
        IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            (string sourceFile, string encryptedFileName, string encryptedFilePath) =
                await UploadProcess(
                    file: file,
                    isEncrypt: true,
                    cancellationToken: cancellationToken
                );

            await _fileEncryption.DESFileEncryption(
                sourceFileLocation: sourceFile,
                destFileLocation: encryptedFilePath,
                key: CON_ENC_KEY_2,
                iv: "23456789",
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: encryptedFilePath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: encryptedFileName
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Decrypts a file that was previously encrypted using DES (Data Encryption Standard) algorithm
    /// </summary>
    /// <remarks>
    /// This endpoint handles file decryption using DES encryption with a fixed 8-byte key and IV.
    /// The process includes:
    /// 1. Uploading the encrypted file to a temporary location
    /// 2. Decrypting the file using DES decryption
    /// 3. Returning the decrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > DES is considered cryptographically weak and should not be used in production environments.
    /// > The key and IV must match those used for encryption. In production, use secure key management.
    ///
    /// > [!WARNING]
    /// > DES has been deprecated due to its vulnerability to brute force attacks.
    /// > Consider using AES for secure decryption.
    ///
    /// > [!CAUTION]
    /// > Attempting to decrypt a file that wasn't encrypted with DES will result in corruption
    ///
    /// > [!NOTE]
    /// > The decrypted file will be saved with '.decrypted' appended to its name
    ///
    /// Example usage:
    /// <code>
    /// using var fileStream = File.OpenRead("encrypted_file.txt.encrypted");
    /// var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", "encrypted_file.txt.encrypted");
    /// var result = await DESDecryptFile(
    ///     file: formFile,
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    /// </remarks>
    /// <param name="file">The encrypted file to be decrypted. Must be a valid DES-encrypted file.</param>
    /// <param name="cancellationToken">Token to cancel the decryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the decrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if decryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when file is invalid or empty</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.des">DES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "DES/Files/Decrypt")]
    public async Task<IActionResult> DESDecryptFile(
        IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            (string filePath, string encryptedFileName, string encryptedFilePath) =
                await UploadProcess(
                    file: file,
                    isEncrypt: false,
                    cancellationToken: cancellationToken
                );

            await _fileEncryption.DESFileDecryption(
                sourceFileLocation: filePath,
                destFileLocation: encryptedFilePath,
                key: CON_ENC_KEY_2,
                iv: CON_ENC_KEY_2,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: encryptedFilePath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: encryptedFileName
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Encrypts a string input and saves it to a file using DES (Data Encryption Standard) encryption
    /// </summary>
    /// <remarks>
    /// This endpoint converts a plain text string into an encrypted file using DES encryption with a fixed 8-byte key and IV.
    /// The process includes:
    /// 1. Converting the input string to bytes
    /// 2. Encrypting the bytes using DES encryption
    /// 3. Saving the encrypted content to a file
    /// 4. Returning the encrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > DES is considered cryptographically weak and should not be used in production environments.
    /// > The key and IV are hardcoded for demo purposes. In production, use secure key management.
    ///
    /// > [!WARNING]
    /// > DES has been deprecated due to its vulnerability to brute force attacks.
    /// > Consider using AES for secure encryption.
    ///
    /// > [!NOTE]
    /// > The encrypted file will be saved as 'encrypted.txt' in the wwwroot/encoded/encrypted directory
    ///
    /// > [!CAUTION]
    /// > The maximum string length that can be encrypted depends on available memory
    ///
    /// Example usage:
    /// <code>
    /// var result = await DESEncryptStringToFile(
    ///     plainText: "Hello, World!",
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    ///
    /// Example request body:
    /// <code>
    /// {
    ///     "plainText": "This is a secret message"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="plainText">The string to be encrypted. Must not be null or empty.</param>
    /// <param name="cancellationToken">Token to cancel the encryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the encrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if encryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when input string is null or empty</exception>
    /// <exception cref="OutOfMemoryException">Thrown when input string is too large to process</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.des">DES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "DES/StringToFile/Encrypt")]
    public async Task<IActionResult> DESEncryptStringToFile(
        string plainText,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (_, outputFullPath) = EncodingPathBuilder(fileName: "encoded.txt", isEncrypt: true);

            await _fileEncryption.DESStringToFileEncryption(
                plainText: plainText,
                destFileLocation: outputFullPath,
                key: CON_ENC_KEY_2,
                iv: CON_ENC_KEY_2,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: outputFullPath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: "encrypted.txt"
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted string input and saves it to a file using DES (Data Encryption Standard) decryption
    /// </summary>
    /// <remarks>
    /// This endpoint converts an encrypted string back to its original form using DES decryption with a fixed 8-byte key and IV.
    /// The process includes:
    /// 1. Converting the encrypted input string to bytes
    /// 2. Decrypting the bytes using DES decryption
    /// 3. Saving the decrypted content to a file
    /// 4. Returning the decrypted file as a downloadable response
    ///
    /// > [!IMPORTANT]
    /// > DES is considered cryptographically weak and should not be used in production environments.
    /// > The key and IV must match those used for encryption. In production, use secure key management.
    ///
    /// > [!WARNING]
    /// > DES has been deprecated due to its vulnerability to brute force attacks.
    /// > Consider using AES for secure decryption.
    ///
    /// > [!CAUTION]
    /// > Attempting to decrypt a string that wasn't encrypted with DES will result in corruption
    ///
    /// > [!NOTE]
    /// > The decrypted file will be saved as 'decrypted.txt' in the wwwroot/encoded/decrypted directory
    ///
    /// Example usage:
    /// <code>
    /// var result = await DESDecryptStringToFile(
    ///     encryptedText: "AQIDBAUGBwgJCgsMDQ4PEA==",
    ///     cancellationToken: CancellationToken.None
    /// );
    /// </code>
    ///
    /// Example request body:
    /// <code>
    /// {
    ///     "encryptedText": "AQIDBAUGBwgJCgsMDQ4PEA=="
    /// }
    /// </code>
    /// </remarks>
    /// <param name="encryptedText">The encrypted string to be decrypted. Must be a valid base64 encoded string.</param>
    /// <param name="cancellationToken">Token to cancel the decryption operation</param>
    /// <returns>
    /// <see cref="FileResult"/> containing the decrypted file for download, or
    /// <see cref="BadRequestObjectResult"/> if decryption fails
    /// </returns>
    /// <exception cref="IOException">Thrown when file operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied</exception>
    /// <exception cref="ArgumentException">Thrown when input string is not a valid base64 string</exception>
    /// <exception cref="FormatException">Thrown when the encrypted text is not in the correct format</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.des">DES Class</seealso>
    [Tags(tags: "Encryption And Decryption"), HttpPost(template: "DES/StringToFile/Decrypt")]
    public async Task<IActionResult> DESDecryptStringToFile(
        string encryptedText,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (_, outputFullPath) = EncodingPathBuilder(
                fileName: "decoded.txt",
                isEncrypt: false
            );

            await _fileEncryption.DESStringToFileDecryption(
                plainText: encryptedText,
                destFileLocation: outputFullPath,
                key: CON_ENC_KEY_2,
                iv: CON_ENC_KEY_2,
                cancellationToken: cancellationToken
            );

            return File(
                fileContents: await System.IO.File.ReadAllBytesAsync(
                    path: outputFullPath,
                    cancellationToken: cancellationToken
                ),
                contentType: CON_CONTENT_TYPE_APP_OCTET_STREAM,
                fileDownloadName: "decrypted.txt"
            );
        }
        catch (Exception ex)
        {
            systemLogging.LogError(exception: ex);
            return BadRequest(error: ex);
        }
    }
}
