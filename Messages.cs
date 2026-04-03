using MatrixRegistrationTokenBot.Matrix;

namespace MatrixRegistrationTokenBot;

internal static class Messages
{
    internal static readonly Message TokenHelpMessage = new(
        "Для создания токена регистрации, отправьте сообщение, начинающееся на !token");
}
