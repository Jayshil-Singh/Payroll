using System;
using System.Security.Cryptography;

namespace FijiPayroll.LicenseGenerator;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--generate-keys")
        {
            using var rsa = RSA.Create(2048);
            string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();

            Console.WriteLine("=== PRIVATE KEY PEM ===");
            Console.WriteLine(privateKeyPem);
            Console.WriteLine("=== PUBLIC KEY PEM ===");
            Console.WriteLine(publicKeyPem);
            return;
        }

        Console.WriteLine("Usage: FijiPayroll.LicenseGenerator --generate-keys");
    }
}
