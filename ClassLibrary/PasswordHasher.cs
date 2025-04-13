using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class PasswordHasher
{
    private const int SaltSize = 16;

    private const int Iterations = 10000;

    private const int HashSize = 32;

    /// <summary>
    /// Хеширует пароль с использованием PBKDF2.
    /// </summary>
    /// <param name="password">Пароль для хеширования.</param>
    /// <returns>Строка, содержащая соль и хеш, разделенные символом ':'. Это нужно для проверки пароля.</returns>
    public string HashPassword(string password)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(HashSize);

            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Проверяет, совпадает ли пароль с хешем.
    /// </summary>
    /// <param name="password">Пароль для проверки.</param>
    /// <param name="hashedPassword">Хешированный пароль (соль + хеш).</param>
    /// <returns>True, если пароль совпадает, иначе False.</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);

        byte[] salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
