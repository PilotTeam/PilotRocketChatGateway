using Ascon.Pilot.DataClasses;
using System.Text;

namespace PilotRocketChatGateway.Utils
{
    public static class DObjectExtensions
    {
        public static string GetTiltle(this INObject obj, INType type)
        {
            if (obj == null || type == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var displayableAttr in type.Attributes.Where(d => d.ShowInTree).OrderBy(d => d.DisplaySortOrder))
            {
                var attributeText = GetAttributeText(obj, obj.Attributes, displayableAttr);
                if (!String.IsNullOrEmpty(attributeText))
                    attributeText = attributeText.Replace(Environment.NewLine, " ");
                if (sb.Length != 0 && !String.IsNullOrEmpty(attributeText))
                {
                    sb.Append(Constants.PROJECT_TITLE_ATTRIBUTES_DELIMITER);
                }

                sb.Append(attributeText);
            }

            return sb.ToString();
        }
        private static string GetAttributeText(INObject obj, IReadOnlyDictionary<string, DValue> attributes, INAttribute attr)
        {
            if (!attributes.TryGetValue(attr.Name, out var value))
                return String.Empty;

            return value.Value?.ToString();
        }
    }
}
