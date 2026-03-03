namespace Interfaces;

public interface IFileStorageService : IScopedService
{
    /// <summary>
    /// Converts upload to WebP and saves to /storage/cars/{carId}/car_{carId}_{guid}.webp
    /// Returns the stored filename (not path).
    /// </summary>
    /// <param name="carId">The unique identifier for the car.</param>
    /// <param name="imageStream">The stream of the image to convert and save.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>The stored filename (not the full path) of the saved image.</returns>
    Task<string> SaveCarImageAsync(int carId, Stream imageStream, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw bytes and content type of the specified car image.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>A tuple containing the image raw bytes and its content type for serving.</returns>
    Task<(byte[] Content, string ContentType)> GetCarImageAsync(string fileName, CancellationToken ct = default);

    /// <summary>
    /// Deletes the physical file corresponding to the specified image name.
    /// This method is typically called by a cleanup job in a Windows Service.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    Task DeleteCarImageAsync(string fileName, CancellationToken ct = default);

    /// <summary>
    /// Checks if the specified file exists in the storage.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    bool FileExists(string fileName);
}
