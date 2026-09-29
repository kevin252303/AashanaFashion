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
    public int? CategoryId { get; set; }
    public ProductCategory? ProductCategory { get; set; }
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

    // ——— Assigned Workers / Karigars ———
    public int? HandworkWorkerId { get; set; }
    public Vendor? HandworkWorker { get; set; }

    public int? StitchingWorkerId { get; set; }
    public Vendor? StitchingWorker { get; set; }

    // ——— Handwork Garment Parts ———
    public bool HandworkCholi { get; set; } = true;
    public bool HandworkChaniya { get; set; } = true;
    public bool HandworkDupatta { get; set; } = false;

    public string HandworkComponentsSummary
    {
        get
        {
            var parts = new List<string>();
            if (HandworkCholi) parts.Add("Choli");
            if (HandworkChaniya) parts.Add("Chaniya");
            if (HandworkDupatta) parts.Add("Dupatta");
            return parts.Any() ? string.Join(", ", parts) : "None";
        }
    }

    // ——— Navigation ———
    public List<ProductAttributeLine> AttributeLines { get; set; } = new();
    public List<ProductPricelist> Pricelists { get; set; } = new();
    public List<ProductVendor> ProductVendors { get; set; } = new();
    public List<ProductPackaging> Packagings { get; set; } = new();
    public List<ProductExtraCharge> ExtraCharges { get; set; } = new();
    public List<DesignBomItem> BomItems { get; set; } = new();
    public List<DesignOperationCost> OperationCosts { get; set; } = new();

    // ——— Costing & Profit Margin Helpers ———
    public decimal TotalMaterialCost => BomItems.Sum(b => b.EffectiveQuantity * (b.RawMaterial?.Rate ?? 0));
    public decimal TotalLaborCost => OperationCosts.Sum(o => o.EstimatedCost);
    public decimal TotalProductionCost => TotalMaterialCost + TotalLaborCost;
    public decimal GrossProfitMargin => SalesPrice - TotalProductionCost;
    public decimal GrossProfitMarginPercent => SalesPrice > 0 ? (GrossProfitMargin / SalesPrice) * 100m : 0;

    public List<string> GetCreationSteps()
    {
        if (!string.IsNullOrWhiteSpace(CreationFlow))
        {
            var steps = CreationFlow
                .Split(new[] { ',', '→', '>', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            if (steps.Count > 0) return steps;
        }

        if (OperationCosts != null && OperationCosts.Any())
        {
            return OperationCosts.Select(o => o.OperationName).ToList();
        }

        return new List<string> { "Dying", "Handwork", "Stitching" };
    }
}

public class ProductAttributeLine
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public string? Attribute { get; set; }
    public string? Values { get; set; }
    public bool ColourCheck { get; set; }
}

public class ProductPricelist
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }
    public string? Pricelist { get; set; }
    public string? AppliedOn { get; set; }
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
    public string? PackagingName { get; set; }
    public decimal Quantity { get; set; }
}
