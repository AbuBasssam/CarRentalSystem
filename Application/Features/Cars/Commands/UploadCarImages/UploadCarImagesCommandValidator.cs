using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Cars;

public class UploadCarImagesCommandValidator : AbstractValidator<UploadCarImagesCommand>
{
    private static readonly HashSet<string> _allowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long _maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadCarImagesCommandValidator()
    {
        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("At least one image file is required.")
            .Must(files => files.Count <= 10).WithMessage("Cannot upload more than 10 images at once.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.Length)
                .LessThanOrEqualTo(_maxFileSizeBytes)
                .WithMessage("Each file must not exceed 5 MB.");

            file.RuleFor(f => f.ContentType)
                .Must(ct => _allowedMimeTypes.Contains(ct))
                .WithMessage("Only JPEG, PNG, or WebP images are allowed.");

            file.RuleFor(f => f)
                .Must(_HasValidMagicBytes)
                .WithMessage("File content does not match the declared image type.");
        });
    }

    /// <summary>
    /// Validates file signature (magic bytes) to prevent spoofed MIME types.
    /// Resets stream position after reading.
    /// </summary>
    private static bool _HasValidMagicBytes(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = stream.Read(header, 0, header.Length);

            if (read < 3) return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (read >= 8
                && header[0] == 0x89 && header[1] == 0x50
                && header[2] == 0x4E && header[3] == 0x47)
                return true;

            // WebP: RIFF????WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
            if (read >= 12
                && header[0] == 0x52 && header[1] == 0x49
                && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45
                && header[10] == 0x42 && header[11] == 0x50)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}