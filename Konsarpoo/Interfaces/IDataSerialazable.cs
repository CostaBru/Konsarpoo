using JetBrains.Annotations;
using Konsarpoo.Collections.Data.Serialization;

namespace Konsarpoo.Collections;

/// <summary>
/// Defines methods for serializing and deserializing an object using <see cref="IDataSerializationInfo"/>.
/// </summary>
public interface IDataSerializable
{
    /// <summary>
    /// Serializes the current instance to the provided <see cref="IDataSerializationInfo"/> implementation.
    /// </summary>
    /// <param name="info"></param>
    void SerializeTo([NotNull] IDataSerializationInfo info);

    /// <summary>
    /// Deserializes the current instance using <see cref="IDataSerializationInfo"/> implementation.
    /// </summary>
    /// <param name="info"></param>
    void DeserializeFrom([NotNull] IDataSerializationInfo info);
}