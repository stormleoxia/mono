using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.DirectoryServices.ActiveDirectory
{
  /// <summary>
  /// Class exception <see cref="T:System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException"/> is raised when request object is not found in the underlying directory store.
  /// </summary>
  [Serializable]
  public class ActiveDirectoryObjectNotFoundException : Exception, ISerializable
  {
    private readonly Type _objectType;
    private readonly string _name;

    public Type Type
    {
      get
      {
        return _objectType;
      }
    }


    public string Name
    {
      get
      {
        return _name;
      }
    }

    public ActiveDirectoryObjectNotFoundException(string message, Type type, string name)
      : base(message)
    {
      this._objectType = type;
      this._name = name;
    }

    public ActiveDirectoryObjectNotFoundException(string message, Exception inner)
      : base(message, inner)
    {
    }

    public ActiveDirectoryObjectNotFoundException(string message)
      : base(message)
    {
    }

    public ActiveDirectoryObjectNotFoundException()
    {
    }

    protected ActiveDirectoryObjectNotFoundException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
    public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
      base.GetObjectData(serializationInfo, streamingContext);
    }
  }
}
