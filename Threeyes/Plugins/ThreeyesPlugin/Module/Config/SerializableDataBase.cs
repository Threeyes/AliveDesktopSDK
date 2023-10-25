#if USE_JsonDotNet
using Newtonsoft.Json;
#endif

namespace Threeyes.Config
{
    /// <summary>
    /// Base class for all data that are Serializable via Json
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]//Only Serialize Fields, child class will inheric this Attribute
    public abstract class SerializableDataBase
    {
    }
}