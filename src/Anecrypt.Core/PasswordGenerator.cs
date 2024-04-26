using System.Text;

namespace Anecrypt.Core;

public static class PasswordGenerator
{
    public static string Generate(int length, bool symbols, bool numbers)
    {
        StringBuilder builder = new StringBuilder(length);

        ReadOnlySpan<char> pool = BuildPool(symbols, numbers);

        for (int i = 0; i < length; i++)
        {
            builder.Append(pool[Random.Shared.Next(0, pool.Length)]);
        }

        return builder.ToString();
    }

    private static ReadOnlySpan<char> BuildPool(in bool symbols, in bool numbers)
    {
        string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        if (symbols)
            pool += "\"!#$%&()*+-/:;<=>?@[\\]^_{|}~";

        if (numbers)
            pool += "1234567890";

        return pool.AsSpan();
    }
}
