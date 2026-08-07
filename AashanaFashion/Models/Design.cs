namespace AashanaFashion.Models;

public class Design
{
    public int Id { get; set; }

    // ——— General Information ———
    public string DesignNumber { get; set; } = string.Empty;
    public string ProductType { get; set; } = "Goods";
    public string? InvoicingPolicy { get; set; }
    public bool TrackInventory { get; set; }
    public decimal? QuantityOnHand { get; set; }
    public bool Discontinued { get; set; }
    public decimal SalesPrice { get; set; }
    public string? CommonDNo { get; set; }
    public string? SalesTaxes { get; set; }
    public string? PurchaseTaxes { get; set; }
    public string? Category { get; set; }
    public string? HsnSacCode { get; set; }
    public string? Company { get; set; }
    public string? Property1 { get; set; }
    public string? InternalNotes { get; set; }

    // ——— Sales Tab ———
    public string? VisibilityOfProducts { get; set; }
    public string? Website { get; set; }
    public string? Tags { get; set; }
    public bool IsPublished { get; set; }
    public bool SellWhenOutOfStock { get; set; }
    public string? Ribbon { get; set; }
    public bool ShowAvailableQty { get; set; }
    public string? OutOfStockMessage { get; set; }
    public string? EcommerceDescription { get; set; }
    public string? WarningOnSalesOrders { get; set; }
    public string? QuotationDescription { get; set; }
    public string? ReInvoiceCosts { get; set; }

    // ——— Inventory Tab ———
    public bool RouteBuy { get; set; }
    public bool RouteManufacture { get; set; }
    public bool RouteResupplySubcontractor { get; set; }
    public bool RouteResupplySubcontractorOnOrder { get; set; }
    public string? Responsible { get; set; }
    public int? CustomerLeadTime { get; set; }
    public decimal? SafetyFactor { get; set; }
    public string? DescriptionForReceipts { get; set; }
    public string? DescriptionForInternalTransfers { get; set; }
    public string? DescriptionForDeliveryOrders { get; set; }

    // ——— Purchase Tab ———
    public string? PurchaseDescription { get; set; }
    public string? WarningOnPurchaseOrders { get; set; }
    public string? ControlPolicy { get; set; }

    // ——— Existing legacy fields ———
    public string? PhotoPath { get; set; }
    public string Colours { get; set; } = string.Empty;
    public string Sizes { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CreationFlow { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // ——— Navigation ———
    public List<ProductAttributeLine> AttributeLines { get; set; } = new();
    public List<ProductPricelist> Pricelists { get; set; } = new();
    public List<ProductVendor> ProductVendors { get; set; } = new();
    public List<ProductPackaging> Packagings { get; set; } = new();

    public List<string> GetCreationSteps() =>
        CreationFlow.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
}

public class ProductAttributeLine
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public string Attribute { get; set; } = string.Empty;
    public string Values { get; set; } = string.Empty;
    public bool ColourCheck { get; set; }
}

public class ProductPricelist
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public string Pricelist { get; set; } = string.Empty;
    public string AppliedOn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal MinQuantity { get; set; }
}

public class ProductVendor
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Piece";
    public decimal UnitPrice { get; set; }
    public int LeadTime { get; set; }
}

public class ProductPackaging
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public string PackagingName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
