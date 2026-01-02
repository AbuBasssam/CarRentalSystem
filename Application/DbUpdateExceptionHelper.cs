using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Application;

/// <summary>
/// Helper for handling DbUpdateException with SQL Server specifics
/// </summary>
public static class DbUpdateExceptionHelper
{
    // SQL Server Error Numbers
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    /// <summary>
    /// Checks if exception is a unique constraint violation
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlEx &&
               (sqlEx.Number == UniqueConstraintViolation ||
                sqlEx.Number == UniqueIndexViolation);
    }

}