using Interfaces;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;


namespace Application.Services;

/// <summary>
/// Implements the file storage service for managing car images locally.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    #region Field(s)

    private readonly string _basePath;

    #endregion

    #region Constructor(s)

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorageService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration settings to read the base path for file storage.</param>
    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "storage", "cars");
    }

    #endregion

    #region Method(s)

    /// <summary>
    /// Saves the uploaded car image after converting it to WebP format.
    /// </summary>
    /// <param name="carId">The unique identifier for the car.</param>
    /// <param name="imageStream">The stream for the image being uploaded.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>The stored filename (without path) of the saved image.</returns>

    public async Task<string> SaveCarImageAsync(int carId, Stream imageStream, CancellationToken ct = default)
    {
        var carFolder = Path.Combine(_basePath, carId.ToString());
        Directory.CreateDirectory(carFolder);

        var fileName = $"car_{carId}_{Guid.NewGuid():N}.webp";
        var filePath = Path.Combine(carFolder, fileName);

        using var image = await Image.LoadAsync(imageStream, ct);
        await image.SaveAsync(filePath, new WebpEncoder(), ct);

        return fileName;
    }

    /// <summary>
    /// Retrieves the contents of the specified car image file.
    /// </summary>
    /// <param name="fileName">The name of the image file to retrieve.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>A tuple containing the image raw bytes and its content type.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified image file cannot be found.</exception>
    public async Task<(byte[] Content, string ContentType)> GetCarImageAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = _BuildPath(fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Image not found: {fileName}");

        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return (bytes, "image/webp");
    }

    /// <summary>
    /// Deletes the specified car image file from storage.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    public Task DeleteCarImageAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = _BuildPath(fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the specified car image file exists in storage.
    /// </summary>
    /// <param name="fileName">The name of the file to check for existence.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>

    public bool FileExists(string fileName)
        => File.Exists(_BuildPath(fileName));

    #endregion

    #region Helpers
    /// <summary>
    /// Constructs the physical path from the given filename.
    /// The expected format is: car_{carId}_{guid}.webp.
    /// </summary>
    /// <param name="fileName">The file name to construct the path from.</param>
    /// <returns>The full physical path of the file.</returns>
    /// <exception cref="ArgumentException">Thrown if the filename format is invalid.</exception>

    private string _BuildPath(string fileName)
    {
        // Extract carId from: car_5_abc123.webp → parts[1] = "5"
        var parts = fileName.Split('_');
        if (parts.Length < 3)
            throw new ArgumentException($"Invalid car image filename format: {fileName}");

        var carId = parts[1];
        return Path.Combine(_basePath, carId, fileName);
    }
    #endregion

}