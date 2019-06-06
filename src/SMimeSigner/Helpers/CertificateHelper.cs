﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SMimeSigner.Helpers
{
    /// <summary>
    /// Helper class that helps with the computers current registers X509 certificate store.
    /// </summary>
    internal static class CertificateHelper
    {
        /// <summary>
        /// Gets the OIN where to store the signature stamp.
        /// </summary>
        public static Oid SignatureTimeStampOin { get; } = new Oid("1.2.840.113549.1.9.16.2.14");

        public static X509Certificate2 FindUserCertificate(string localUser)
        {
            bool isIdToken = !localUser.Contains("@", StringComparison.InvariantCulture);

            Func<X509Certificate2, bool> isMatchFunc;
            if (!isIdToken)
            {
                isMatchFunc = cert => cert.GetNameInfo(X509NameType.EmailName, false)?.Equals(localUser, StringComparison.InvariantCultureIgnoreCase) ?? false;
            }
            else
            {
                isMatchFunc = cert => cert.Thumbprint?.Equals(localUser, StringComparison.InvariantCultureIgnoreCase) ?? false;
            }

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                try
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                    foreach (var certificate in store.Certificates)
                    {
                        if (isMatchFunc(certificate))
                        {
                            return certificate;
                        }
                    }
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine("Could not open key store: " + ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Based on http://www.iana.org/assignments/pgp-parameters/pgp-parameters.xhtml#pgp-parameters-12
        /// it will retrieve the appropriate code.
        /// </summary>
        /// <param name="certificate">The certificate to get the code for.</param>
        /// <returns>The PGP code.</returns>
        public static (int algorithmCode, int hashCode) ToPgpPublicKeyAlgorithmCode(X509Certificate2 certificate)
        {
            const int ecdsaAlgorithm = 19, rsaAlgorithm = 1;
            const int sha1 = 2, sha256 = 8, sha384 = 9, sha512 = 10;
            switch (certificate.SignatureAlgorithm.Value)
            {
                case "1.2.840.10045.4.1": // SHA1ECDSA
                    return (ecdsaAlgorithm, sha1);
                case "1.2.840.113549.1.1.5": // SHA1RSA
                    return (rsaAlgorithm, sha1);

                case "1.2.840.10045.4.3.2": // SHA256ECDSA
                    return (ecdsaAlgorithm, sha256);
                case "1.2.840.113549.1.1.11": // SHA256RSA
                    return (rsaAlgorithm, sha256);

                case "1.2.840.10045.4.3.3": // SHA384ECDSA
                    return (ecdsaAlgorithm, sha384);
                case "1.2.840.113549.1.1.12": // SHA384RSA
                    return (rsaAlgorithm, sha384);

                case "1.2.840.10045.4.3.4": // SHA512ECDSA
                    return (ecdsaAlgorithm, sha512);
                case "1.2.840.113549.1.1.13": // SHA512RSA
                    return (rsaAlgorithm, sha512);

                default:
                    throw new Exception("Certificate has unknown signature algorithm: " + certificate.SignatureAlgorithm.FriendlyName);
            }
        }
    }
}