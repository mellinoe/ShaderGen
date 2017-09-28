using System;

namespace ShaderGen
{
    internal class TypeAndMethodName : IEquatable<TypeAndMethodName>
    {
        public string TypeName;
        public string MethodName;

        public string FullName => TypeName + "." + MethodName;

        public static bool Get(string fullName, out TypeAndMethodName typeAndMethodName)
        {
            string[] parts = fullName.Split(new[] { '.' });
            if (parts.Length < 2)
            {
                typeAndMethodName = default(TypeAndMethodName);
                return false;
            }
            string typeName = parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                typeName += "." + parts[i];
            }

            typeAndMethodName = new TypeAndMethodName { TypeName = typeName, MethodName = parts[parts.Length - 1] };
            return true;
        }

        public bool Equals(TypeAndMethodName other)
        {
            return TypeName == other.TypeName && MethodName == other.MethodName;
        }

        public override string ToString() => FullName;
    }
}
