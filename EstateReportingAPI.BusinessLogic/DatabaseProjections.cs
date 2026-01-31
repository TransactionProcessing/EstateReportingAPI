using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;
using ContractProduct = TransactionProcessor.Database.Entities.ContractProduct;
using Merchant = TransactionProcessor.Database.Entities.Merchant;
using Operator = TransactionProcessor.Database.Entities.Operator;

namespace EstateReportingAPI.BusinessLogic
{

    public class DatabaseProjections {
        public class SettlementGroupProjection
        {
            public decimal SettledValue { get; set; }
            public int SettledCount { get; set; }

            public decimal UnSettledValue { get; set; }
            public int UnSettledCount { get; set; }
        }

        


        public class FeeTransactionProjection
        {
            public MerchantSettlementFee Fee { get; set; }
            public Transaction Txn { get; set; }
        }

        public class TodaySettlementTransactionProjection
        {
            public MerchantSettlementFee Fee { get; set; }
            public TodayTransaction Txn { get; set; }
        }

        public class ComparisonSettlementTransactionProjection
        {
            public MerchantSettlementFee Fee { get; set; }
            public TransactionHistory Txn { get; set; }
        }

        public class TransactionSearchProjection {
            public Transaction Transaction { get; set; }
            public Operator Operator { get; set; }
            public Merchant Merchant { get; set; }
            public ContractProduct Product { get; set; }
        }

        public class TopBottomData
        {
            public String DimensionName { get; set; }
            public Decimal SalesValue { get; set; }
        }
    }
}
