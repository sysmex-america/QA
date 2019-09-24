using Microsoft.Xrm.Sdk;
using System.Linq;

namespace Sysmex.Crm.Plugins.Extensions
{
    public static class EntityExtensions
    {
        public static T GetAliasedAttributeValue<T>(this Entity entity, string aliasedAttributeName)
        {
            T result = default(T);
            if (entity.Contains(aliasedAttributeName))
            {
                result = (T)entity.GetAttributeValue<AliasedValue>(aliasedAttributeName).Value;
            }

            return result;
        }

        public static void MergePreImage(this Entity target, Entity preImage)
        {
            if (target != null && preImage != null)
            {
                preImage.Attributes.Keys
                    .ToList()
                    .ForEach((key) =>
                    {
                        if (!target.Contains(key))
                        {
                            target[key] = preImage[key];
                        }
                    });
            }
        }
    }
}
