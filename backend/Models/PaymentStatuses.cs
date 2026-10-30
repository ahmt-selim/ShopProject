namespace Backend.Models;

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";

    public static readonly string[] All = [Pending, Paid, Failed];
}