using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System
{
  /// <summary>
  /// Fournit la classe de base pour les protecteurs de données.
  /// </summary>
  public abstract class DataProtector
  {
    private string m_applicationName;
    private string m_primaryPurpose;
    private IEnumerable<string> m_specificPurposes;
    private volatile byte[] m_hashedPurpose;

    /// <summary>
    /// Obtient le nom de l'application.
    /// </summary>
    /// 
    /// <returns>
    /// Nom de l'application.
    /// </returns>
    protected string ApplicationName
    {
      get
      {
        return this.m_applicationName;
      }
    }

    /// <summary>
    /// Spécifie si le hachage est ajouté au tableau de texte avant le chiffrement.
    /// </summary>
    /// 
    /// <returns>
    /// Toujours true.
    /// </returns>
    protected virtual bool PrependHashedPurposeToPlaintext
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Obtient l'objectif principal des données protégées.
    /// </summary>
    /// 
    /// <returns>
    /// Objectif principal des données protégées.
    /// </returns>
    protected string PrimaryPurpose
    {
      get
      {
        return this.m_primaryPurpose;
      }
    }

    /// <summary>
    /// Obtient les objectifs spécifiques des données protégées.
    /// </summary>
    /// 
    /// <returns>
    /// Collection des objectifs spécifiques pour les données protégées.
    /// </returns>
    protected IEnumerable<string> SpecificPurposes
    {
      get
      {
        return this.m_specificPurposes;
      }
    }

    /// <summary>
    /// Crée une instance de la classe <see cref="T:System.Security.Cryptography.DataProtector"/> en utilisant le nom d'application, l'objectif principal et les objectifs spécifiques fournis.
    /// </summary>
    /// <param name="applicationName">Nom de l'application.</param><param name="primaryPurpose">Objectif principal des données protégées.Consultez la section Remarques pour obtenir des informations importantes supplémentaires.</param><param name="specificPurposes">Objectifs spécifiques des données protégées.Consultez la section Remarques pour obtenir des informations importantes supplémentaires.</param><exception cref="T:System.ArgumentException"><paramref name="applicationName"/> est une chaîne vide ou a la valeur null.ou<paramref name="primaryPurpose"/> est une chaîne vide ou a la valeur null.ou<paramref name="specificPurposes"/> contient une chaîne vide ou null.</exception>
    protected DataProtector(string applicationName, string primaryPurpose, string[] specificPurposes)
    {
      if (string.IsNullOrWhiteSpace(applicationName))
        throw new ArgumentException("Invalid App Name or Purpose", "applicationName");
      if (string.IsNullOrWhiteSpace(primaryPurpose))
        throw new ArgumentException("Invalid App Name or Purpose", "primaryPurpose");
      if (specificPurposes != null)
      {
        foreach (string str in specificPurposes)
        {
          if (string.IsNullOrWhiteSpace(str))
            throw new ArgumentException("Invalid App Name or Purpose", "specificPurposes");
        }
      }
      this.m_applicationName = applicationName;
      this.m_primaryPurpose = primaryPurpose;
      List<string> list = new List<string>();
      if (specificPurposes != null)
        list.AddRange((IEnumerable<string>) specificPurposes);
      this.m_specificPurposes = (IEnumerable<string>) list;
    }

    /// <summary>
    /// Crée un hachage des valeurs de propriétés spécifiées par le constructeur.
    /// </summary>
    /// 
    /// <returns>
    /// Un tableau d'octets qui contiennent le hachage des propriétés de <see cref="P:System.Security.Cryptography.DataProtector.ApplicationName"/>, de <see cref="P:System.Security.Cryptography.DataProtector.PrimaryPurpose"/>, et de <see cref="P:System.Security.Cryptography.DataProtector.SpecificPurposes"/>.
    /// </returns>
    protected virtual byte[] GetHashedPurpose()
    {
      if (this.m_hashedPurpose == null)
      {
        using (HashAlgorithm hashAlgorithm = HashAlgorithm.Create("System.Security.Cryptography.Sha256Cng"))
        {
          using (BinaryWriter binaryWriter = new BinaryWriter((Stream) new CryptoStream((Stream) new MemoryStream(), (ICryptoTransform) hashAlgorithm, CryptoStreamMode.Write), (Encoding) new UTF8Encoding(false, true)))
          {
            binaryWriter.Write(this.ApplicationName);
            binaryWriter.Write(this.PrimaryPurpose);
            foreach (string str in this.SpecificPurposes)
              binaryWriter.Write(str);
          }
          this.m_hashedPurpose = hashAlgorithm.Hash;
        }
      }
      return this.m_hashedPurpose;
    }

    /// <summary>
    /// Détermine si le re-chiffrement est requis pour les données chiffrées spécifiées.
    /// </summary>
    /// 
    /// <returns>
    /// true si les données doivent être rechiffrées ; sinon, false.
    /// </returns>
    /// <param name="encryptedData">Données chiffrées à évaluer.</param>
    public abstract bool IsReprotectRequired(byte[] encryptedData);

    /// <summary>
    /// Crée une instance d'une implémentation de protecteur de données à l'aide du nom de classe spécifié du protecteur de données, du nom de l'application, de l'objectif principal et des objectifs spécifiques.
    /// </summary>
    /// 
    /// <returns>
    /// Objet d'implémentation de protecteur de données.
    /// </returns>
    /// <param name="providerClass">Nom de classe du protecteur de données.</param><param name="applicationName">Nom de l'application.</param><param name="primaryPurpose">Objectif principal des données protégées.</param><param name="specificPurposes">Objectifs spécifiques des données protégées.</param><exception cref="T:System.ArgumentNullException"><paramref name="providerClass"/> a la valeur null.</exception>
    public static DataProtector Create(string providerClass, string applicationName, string primaryPurpose, params string[] specificPurposes)
    {
      if (providerClass == null)
        throw new ArgumentNullException("providerClass");
      return (DataProtector) CryptoConfig.CreateFromName(providerClass, (object) applicationName, (object) primaryPurpose, (object) specificPurposes);
    }

    /// <summary>
    /// Protège les données utilisateur spécifiées.
    /// </summary>
    /// 
    /// <returns>
    /// Tableau d'octets qui contient les données chiffrées.
    /// </returns>
    /// <param name="userData">Données à protéger.</param><exception cref="T:System.ArgumentNullException"><paramref name="userData"/> a la valeur null.</exception>
    public byte[] Protect(byte[] userData)
    {
      if (userData == null)
        throw new ArgumentNullException("userData");
      if (this.PrependHashedPurposeToPlaintext)
      {
        byte[] hashedPurpose = this.GetHashedPurpose();
        byte[] numArray = new byte[userData.Length + hashedPurpose.Length];
        Array.Copy((Array) hashedPurpose, 0, (Array) numArray, 0, hashedPurpose.Length);
        Array.Copy((Array) userData, 0, (Array) numArray, hashedPurpose.Length, userData.Length);
        userData = numArray;
      }
      return this.ProviderProtect(userData);
    }

    /// <summary>
    /// Spécifie la méthode déléguée dans la classe dérivée appelée par la méthode <see cref="M:System.Security.Cryptography.DataProtector.Protect(System.Byte[])"/> dans la classe de base.
    /// </summary>
    /// 
    /// <returns>
    /// Tableau d'octets qui contient les données chiffrées.
    /// </returns>
    /// <param name="userData">Données à chiffrer.</param>
    protected abstract byte[] ProviderProtect(byte[] userData);

    /// <summary>
    /// Spécifie la méthode déléguée dans la classe dérivée appelée par la méthode <see cref="M:System.Security.Cryptography.DataProtector.Unprotect(System.Byte[])"/> dans la classe de base.
    /// </summary>
    /// 
    /// <returns>
    /// Données non chiffrées.
    /// </returns>
    /// <param name="encryptedData">Données à chiffrer.</param>
    protected abstract byte[] ProviderUnprotect(byte[] encryptedData);

    /// <summary>
    /// Ôte la protection des données protégées spécifiées.
    /// </summary>
    /// 
    /// <returns>
    /// Tableau d'octets qui contient les données en texte brut.
    /// </returns>
    /// <param name="encryptedData">Données chiffrées à déprotéger.</param><exception cref="T:System.ArgumentNullException"><paramref name="encryptedData"/> a la valeur null.</exception><exception cref="T:System.Security.Cryptography.CryptographicException"><paramref name="encryptedData"/> contient un objectif non valide.</exception>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public byte[] Unprotect(byte[] encryptedData)
    {
      if (encryptedData == null)
        throw new ArgumentNullException("encryptedData");
      if (!this.PrependHashedPurposeToPlaintext)
        return this.ProviderUnprotect(encryptedData);
      byte[] numArray1 = this.ProviderUnprotect(encryptedData);
      byte[] hashedPurpose = this.GetHashedPurpose();
      bool flag = numArray1.Length >= hashedPurpose.Length;
      for (int index = 0; index < hashedPurpose.Length; ++index)
      {
        if ((int) hashedPurpose[index] != (int) numArray1[index % numArray1.Length])
          flag = false;
      }
      if (!flag)
        throw new CryptographicException("Invalid Purpose");
      byte[] numArray2 = new byte[numArray1.Length - hashedPurpose.Length];
      Array.Copy((Array) numArray1, hashedPurpose.Length, (Array) numArray2, 0, numArray2.Length);
      return numArray2;
    }
  }
}
