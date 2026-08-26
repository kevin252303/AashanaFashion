using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    public enum TransactionType
    {
        Income,
        Expense
    }

    [Table("AccountingTransactions")]
    public class AccountingTransaction
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // e.g. "Sales", "Purchase", "Salary", "Rent", "Utility", "JobWork", "Other"

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Reference { get; set; } // Invoice no, PO no, Lot no, etc.

        public int? VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}
