using ICS.Portal.Data.Custom;

namespace ICS.Mobile.Helpers
{
    public class IdGenerators
    {
        public static long AttributeValueIdAsLong(int attributeId, int contextId, int keyId, int workOderDispatchId)
        {
            return (long)AttributeValueIdAsInt(attributeId, contextId, keyId, workOderDispatchId);
        }

        public static int AttributeValueIdAsInt(int attributeId, int contextId, int keyId, int workOderDispatchId)
        {
            // return ICS.Portal.Data.Custom.SQLHashFunctions.GetDeterministicHashCodeSQL($"{attributeId}{contextId}{keyId}{workOderDispatchId}");
            return ($"{attributeId}{contextId}{keyId}{workOderDispatchId}").HashAsInt();
        }
    }
}
