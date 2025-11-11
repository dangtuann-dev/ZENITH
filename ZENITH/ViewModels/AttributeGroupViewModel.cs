namespace ZENITH.ViewModels
{
    public class AttributeValueOptionViewModel
    {
        public int ValueId { get; set; }
        public string ValueName { get; set; } = string.Empty;
        public string? ColorCode { get; set; }
        public bool IsSelected { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class AttributeGroupViewModel
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty; // internal name
        public string DisplayName { get; set; } = string.Empty; // show in UI
        public string InputType { get; set; } = "select"; // select, color, text
        public List<AttributeValueOptionViewModel> Options { get; set; } = new List<AttributeValueOptionViewModel>();
    }
}
