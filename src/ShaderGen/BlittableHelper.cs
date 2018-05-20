using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

/// <summary>
/// 
/// </summary>
namespace ShaderGen
{
    /// <summary>
    /// Allows detection of whether a type is blittable.
    /// </summary>
    /// <remarks>From https://aakinshin.net/blog/post/blittable/</remarks>
    public static class BlittableHelper
    {
        /// <summary>
        /// Determines whether the <typeparamref name="T">specified type</typeparamref> is blittable.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>
        ///   <c>true</c> if this type is blittable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBlittable<T>() => IsBlittableCache<T>.Value;

        /// <summary>
        /// Determines whether the specified <paramref name="type"/> is blittable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is blittable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBlittable(this Type type)
        {
            while (type.IsArray)
                type = type.GetElementType();

            try
            {
                GCHandle.Alloc(FormatterServices.GetUninitializedObject(type), GCHandleType.Pinned).Free();
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Determines whether the specified full type name is blittable.
        /// </summary>
        /// <param name="fullTypeName">Full name of the type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is blittable and <c>false</c> if it isn't; otherwise, <c>null</c> if the type was not found.
        /// </returns>
        public static bool? IsBlittable(string fullTypeName) => Type.GetType(fullTypeName)?.IsBlittable();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static class IsBlittableCache<T>
        {
            public static readonly bool Value = IsBlittable(typeof(T));
        }
    }
}