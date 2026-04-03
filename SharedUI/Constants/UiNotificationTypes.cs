namespace FoodStreet.Client.Constants;

public static class UiNotificationTypes
{
    public static bool IsMenuNotification(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return type.StartsWith("Menu_", StringComparison.OrdinalIgnoreCase)
            || type.StartsWith("Food_", StringComparison.OrdinalIgnoreCase);
    }
}
