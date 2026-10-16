namespace Backend.Services.Reviews;

// Service katmanı iş kuralı ihlallerini exception ile bildirir,
// ReviewsController bunları ProblemDetails'e çevirir.
public sealed class ReviewNotFoundException(string message) : Exception(message);

public sealed class ReviewForbiddenException(string message) : Exception(message);

public sealed class ReviewConflictException(string message) : Exception(message);

public sealed class ReviewValidationException(string message) : Exception(message);
