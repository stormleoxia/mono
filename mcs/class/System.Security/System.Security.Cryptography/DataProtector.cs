using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography
{
	public abstract class DataProtector
	{
		private readonly string _applicationName;

		private readonly string _primaryPurpose;

		private readonly IEnumerable<string> _specificPurposes;

		private volatile byte[] _hashedPurpose;

		protected string ApplicationName
		{
			get
			{
				return _applicationName;
			}
		}

		protected virtual bool PrependHashedPurposeToPlaintext
		{
			get
			{
				return true;
			}
		}

		protected string PrimaryPurpose
		{
			get
			{
				return _primaryPurpose;
			}
		}

		protected IEnumerable<string> SpecificPurposes
		{
			get
			{
				return _specificPurposes;
			}
		}

		protected DataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
		{
			if (string.IsNullOrWhiteSpace(applicationName))
			{
				throw new ArgumentException(Locale.GetText("Invalid Application Name"), "applicationName");
			}
			if (string.IsNullOrWhiteSpace(primaryPurpose))
			{
				throw new ArgumentException(Locale.GetText("Invalid Application Primary Purpose"), "primaryPurpose");
			}
			if (specificPurposes != null)
			{
				for (int i = 0; i < specificPurposes.Length; i++)
				{
					if (string.IsNullOrWhiteSpace(specificPurposes[i]))
					{
						throw new ArgumentException(Locale.GetText("Invalid Application Sub Purpose"), "specificPurposes");
					}
				}
			}
			_applicationName = applicationName;
			_primaryPurpose = primaryPurpose;
			List<string> list = new List<string>();
			if (specificPurposes != null)
			{
				list.AddRange(specificPurposes);
			}
			_specificPurposes = list;
		}

		protected virtual byte[] GetHashedPurpose()
		{
			if (_hashedPurpose == null)
			{
				using (HashAlgorithm hashAlgorithm = HashAlgorithm.Create("System.Security.Cryptography.Sha256Cng"))
				{
					using (BinaryWriter binaryWriter = new BinaryWriter(new CryptoStream(new MemoryStream(), hashAlgorithm, CryptoStreamMode.Write), new UTF8Encoding(false, true)))
					{
						binaryWriter.Write(ApplicationName);
						binaryWriter.Write(PrimaryPurpose);
						foreach (string current in SpecificPurposes)
						{
							binaryWriter.Write(current);
						}
					}
					_hashedPurpose = hashAlgorithm.Hash;
				}
			}
			return _hashedPurpose;
		}

		public abstract bool IsReprotectRequired(byte[] encryptedData);

		public static DataProtector Create(string providerClass, string applicationName, string primaryPurpose, params string[] specificPurposes)
		{
			if (providerClass == null)
			{
				throw new ArgumentNullException("providerClass");
			}
			return (DataProtector)CryptoConfig.CreateFromName(providerClass, applicationName, primaryPurpose, specificPurposes);
		}

		public byte[] Protect(byte[] userData)
		{
			if (userData == null)
			{
				throw new ArgumentNullException("userData");
			}
			if (PrependHashedPurposeToPlaintext)
			{
				byte[] hashedPurpose = GetHashedPurpose();
				byte[] array = new byte[userData.Length + hashedPurpose.Length];
				Array.Copy(hashedPurpose, 0, array, 0, hashedPurpose.Length);
				Array.Copy(userData, 0, array, hashedPurpose.Length, userData.Length);
				userData = array;
			}
			return ProviderProtect(userData);
		}

		protected abstract byte[] ProviderProtect(byte[] userData);

		protected abstract byte[] ProviderUnprotect(byte[] encryptedData);

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public byte[] Unprotect(byte[] encryptedData)
		{
			if (encryptedData == null)
			{
				throw new ArgumentNullException("encryptedData");
			}
			if (!PrependHashedPurposeToPlaintext)
			{
				return ProviderUnprotect(encryptedData);
			}
			byte[] array = ProviderUnprotect(encryptedData);
			byte[] hashedPurpose = GetHashedPurpose();
			bool flag = array.Length >= hashedPurpose.Length;
			for (int i = 0; i < hashedPurpose.Length; i++)
			{
				if (hashedPurpose[i] != array[i % array.Length])
				{
					flag = false;
				}
			}
			if (!flag)
			{
				throw new CryptographicException(Locale.GetText("Invalid Application Purpose"));
			}
			byte[] array2 = new byte[array.Length - hashedPurpose.Length];
			Array.Copy(array, hashedPurpose.Length, array2, 0, array2.Length);
			return array2;
		}
	}
}
