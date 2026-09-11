using UnityEngine;

namespace DdaIT.Util.Attribute
{
    public class AliasAttribute : PropertyAttribute
    {
        public AliasAttribute(string text)
        {
            Text = text;
        }
        public string Text { get; private set; }
    }
}
