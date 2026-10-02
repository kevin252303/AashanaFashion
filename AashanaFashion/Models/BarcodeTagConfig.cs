using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AashanaFashion.Models;

public class BarcodeTagConfig
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ConfigName { get; set; } = "Default";

    public bool IsDefault { get; set; } = true;

    // Dimensions & Sizing
    public int TagWidthMm { get; set; } = 50;
    public int TagHeightMm { get; set; } = 30;
    public string FontSize { get; set; } = "normal"; // small, normal, large

    // Header & Brand
    public bool ShowBrandName { get; set; } = true;
    [MaxLength(100)]
    public string BrandName { get; set; } = "Aashana Fashion";

    public bool ShowSubtitle { get; set; } = false;
    [MaxLength(100)]
    public string? Subtitle { get; set; } = "Designer Ethnic Wear";

    // Core Garment Fields (Current details)
    public bool ShowDesignNumber { get; set; } = true;
    [MaxLength(50)]
    public string DesignNumberLabel { get; set; } = "Design";

    public bool ShowLotNo { get; set; } = true;
    [MaxLength(50)]
    public string LotNoLabel { get; set; } = "Lot";

    public bool ShowSerialNumber { get; set; } = true;
    [MaxLength(50)]
    public string SerialNumberLabel { get; set; } = "#";

    public bool ShowComponent { get; set; } = true;
    [MaxLength(50)]
    public string ComponentLabel { get; set; } = "Part";

    public bool ShowColour { get; set; } = true;
    [MaxLength(50)]
    public string ColourLabel { get; set; } = "Colour";

    public bool ShowSize { get; set; } = true;
    [MaxLength(50)]
    public string SizeLabel { get; set; } = "Size";

    // Codes
    public bool ShowBarcode { get; set; } = true;
    public bool ShowBarcodeText { get; set; } = true;
    public bool ShowQrCode { get; set; } = true;
    public int BarcodeHeight { get; set; } = 34; // px

    // Commercial / Pricing
    public bool ShowPrice { get; set; } = false;
    [MaxLength(50)]
    public string PriceLabel { get; set; } = "M.R.P. ₹";

    // Product Category & HSN
    public bool ShowCategory { get; set; } = false;
    [MaxLength(50)]
    public string CategoryLabel { get; set; } = "Category";

    public bool ShowHsnCode { get; set; } = false;
    [MaxLength(50)]
    public string HsnLabel { get; set; } = "HSN";

    // Dates
    public bool ShowMfgDate { get; set; } = false;
    [MaxLength(50)]
    public string MfgDateLabel { get; set; } = "Mfg Date";

    // Standard Additional Details
    public bool ShowCustomField1 { get; set; } = false;
    [MaxLength(100)]
    public string? CustomField1Label { get; set; } = "Fabric";
    [MaxLength(200)]
    public string? CustomField1Value { get; set; } = "";

    public bool ShowCustomField2 { get; set; } = false;
    [MaxLength(100)]
    public string? CustomField2Label { get; set; } = "Wash Care";
    [MaxLength(200)]
    public string? CustomField2Value { get; set; } = "Dry Clean Only";

    public bool ShowCustomField3 { get; set; } = false;
    [MaxLength(100)]
    public string? CustomField3Label { get; set; } = "Origin";
    [MaxLength(200)]
    public string? CustomField3Value { get; set; } = "Made in India";

    // Footer Note
    public bool ShowFooterText { get; set; } = false;
    [MaxLength(200)]
    public string? FooterText { get; set; } = "(Incl. of all taxes)";

    /// <summary>
    /// JSON array of dynamic extra details: [{"id":"...","label":"...","value":"...","enabled":true}]
    /// </summary>
    public string? DynamicCustomFieldsJson { get; set; }

    public DateTime UpdatedDate { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }

    public List<DynamicCustomFieldItem> GetDynamicCustomFields()
    {
        if (string.IsNullOrWhiteSpace(DynamicCustomFieldsJson))
            return new List<DynamicCustomFieldItem>();

        try
        {
            return JsonSerializer.Deserialize<List<DynamicCustomFieldItem>>(DynamicCustomFieldsJson) 
                   ?? new List<DynamicCustomFieldItem>();
        }
        catch
        {
            return new List<DynamicCustomFieldItem>();
        }
    }

    public void SetDynamicCustomFields(List<DynamicCustomFieldItem> fields)
    {
        DynamicCustomFieldsJson = JsonSerializer.Serialize(fields ?? new List<DynamicCustomFieldItem>());
    }
}

public class DynamicCustomFieldItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
