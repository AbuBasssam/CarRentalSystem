namespace Application.Features.Cars;

/// <summary>Binary image payload returned by image-serving endpoints.</summary>
public class CarImageFileDto
{
    public byte[] Content { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string FileName { get; set; } = null!;
}