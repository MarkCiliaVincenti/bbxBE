﻿using bbxBE.Common.Enums;
using bbxBE.Common.NAV;
using bbxBE.Domain.Entities;
using FluentMigrator;
using static bbxBE.Common.NAV.NAV_enums;

//https://code-maze.com/dapper-migrations-fluentmigrator-aspnetcore/

namespace bbxBE.Infrastructure.Persistence.Migrations
{
    [Migration(00010, "v00.00.01-Invoice")]
    public class InitialTables_00010 : Migration
    {
        public override void Down()
        {
            Delete.Index("INX_InvoiceNumber")
                   .OnTable("Invoice");
            Delete.Index("INX_InvoiceIssueDate")
                        .OnTable("Invoice");
            Delete.Index("INX_InvoiceDeliveryDate")
                        .OnTable("Invoice");
            Delete.Table("Invoice");


            Delete.Table("AdditionalInvoiceData");

            Delete.Table("SummaryByVatRate");

            Delete.Table("InvoiceLine");

            Delete.Table("AdditionalInvoiceLineData");

            /*
                delete VersionInfo where Version = 10
                drop table AdditionalInvoiceData
                drop table AdditionalInvoiceLineData
                drop table Invoice
                drop table InvoiceLine
                drop table SummaryByVatRate

             */
        }
        public override void Up()
        {

            Create.Table("Invoice")
                    .WithColumn("ID").AsInt64().NotNullable().PrimaryKey().Identity()
                    .WithColumn("CreateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("UpdateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("Deleted").AsBoolean().WithDefaultValue(false)

                    .WithColumn("Incoming").AsBoolean().WithDefaultValue(false)
                    .WithColumn("InvoiceType").AsString().NotNullable()
                    .WithColumn("WarehouseID").AsInt64().ForeignKey()
                    //InvoiceData 
                    .WithColumn("InvoiceNumber").AsString().NotNullable()
                    .WithColumn("InvoiceIssueDate").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("CompletenessIndicator").AsBoolean().NotNullable().WithDefaultValue(false)

                    //invoiceHead
                    .WithColumn("SupplierID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("CustomerID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("InvoiceCategory").AsString().NotNullable().WithDefaultValue(InvoiceCategoryType.NORMAL.ToString())
                    .WithColumn("InvoiceDeliveryDate").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("PaymentDate").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("PaymentMethod").AsString().NotNullable().WithDefaultValue(PaymentMethodType.OTHER.ToString())
                    .WithColumn("CurrencyCode").AsString().NotNullable().WithDefaultValue(enCurrencyCodes.HUF.ToString())
                    .WithColumn("ExchangeRate").AsDecimal().NotNullable().WithDefaultValue(1)
                    .WithColumn("UtilitySettlementIndicator").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("InvoiceAppearance").AsString().NotNullable().WithDefaultValue(InvoiceCategoryType.NORMAL.ToString())
                    .WithColumn("Copies").AsInt16().Nullable()

                    //Incoming (bevételezés)
                    .WithColumn("IncomingInvReference").AsString().Nullable()

                    //invoiceReference (javítószámla)
                    .WithColumn("OriginalInvoiceID").AsInt64().Nullable().ForeignKey()
                    .WithColumn("OriginalInvoiceNumber").AsString().Nullable()
                    .WithColumn("ModifyWithoutMaster").AsBoolean().Nullable().WithDefaultValue(false)
                    .WithColumn("ModificationIndex").AsInt16().Nullable().WithDefaultValue(0)


                    //InvoiceOrdernumber
                    .WithColumn("OrderNumber").AsString().Nullable()


                    //invoiceSummary
                    .WithColumn("InvoiceNetAmount").AsCurrency().NotNullable()
                    .WithColumn("InvoiceNetAmountHUF").AsCurrency().NotNullable()
                    .WithColumn("InvoiceVatAmount").AsCurrency().NotNullable()
                    .WithColumn("InvoiceVatAmountHUF").AsCurrency().NotNullable()
                    .WithColumn("InvoiceGrossAmount").AsCurrency().NotNullable()
                    .WithColumn("invoiceGrossAmountHUF").AsCurrency().NotNullable();


            Create.Index("INX_InvoiceNumber")
                         .OnTable("Invoice")
                         .OnColumn("InvoiceNumber").Ascending()
                         .WithOptions().NonClustered();

            Create.Index("INX_InvoiceIssueDate")
                         .OnTable("Invoice")
                         .OnColumn("Incoming").Ascending()
                         .OnColumn("InvoiceIssueDate").Ascending()
                         .OnColumn("WarehouseID").Ascending()
                         .WithOptions().NonClustered();

            Create.Index("INX_InvoiceDeliveryDate")
                         .OnTable("Invoice")
                         .OnColumn("Incoming").Ascending()
                         .OnColumn("InvoiceDeliveryDate").Ascending()
                         .OnColumn("WarehouseID").Ascending()
                         .WithOptions().NonClustered();

            Create.Table("AdditionalInvoiceData")
                    .WithColumn("ID").AsInt64().NotNullable().PrimaryKey().Identity()
                    .WithColumn("CreateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("UpdateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("Deleted").AsBoolean().WithDefaultValue(false)
                    .WithColumn("InvoiceID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("DataName").AsString().NotNullable()
                    .WithColumn("DataDescription").AsString(int.MaxValue).Nullable()
                    .WithColumn("DataValue").AsString().Nullable();

            Create.Table("SummaryByVatRate")
                    .WithColumn("ID").AsInt64().NotNullable().PrimaryKey().Identity()
                    .WithColumn("CreateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("UpdateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("Deleted").AsBoolean().WithDefaultValue(false)
                    .WithColumn("InvoiceID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("VatRateID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("VatNetAmount").AsCurrency().NotNullable()
                    .WithColumn("VatNetAmountHUF").AsCurrency().NotNullable()
                    .WithColumn("VatRateNetAmount").AsCurrency().NotNullable()
                    .WithColumn("VatRateNetAmountHUF").AsCurrency().NotNullable();


            Create.Table("InvoiceLine")
                     .WithColumn("ID").AsInt64().NotNullable().PrimaryKey().Identity()
                     .WithColumn("CreateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                     .WithColumn("UpdateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                     .WithColumn("Deleted").AsBoolean().WithDefaultValue(false)
                     .WithColumn("InvoiceID").AsInt64().NotNullable().ForeignKey()
                     .WithColumn("ProductID").AsInt64().Nullable().ForeignKey()                         //Opcionális
                     .WithColumn("ProductCode").AsString().Nullable()                                   //Opcionális!
                     .WithColumn("VTSZ").AsString().Nullable()                                          //Opcionális!
                     .WithColumn("VatRateID").AsInt64().NotNullable().ForeignKey()
                     .WithColumn("VatPercentage").AsDecimal().Nullable()
                     .WithColumn("LineNumber").AsInt16().NotNullable()
                     .WithColumn("LineExpressionIndicator").AsBoolean().WithDefaultValue(true)           // Ha a tag értéke true, akkor adott számlasorban kötelező megadni:
                                                                                                         // a.a termék vagy szolgáltatás nevét
                                                                                                         // b.mennyiségét
                                                                                                         // c.mennyiségi egységét
                                                                                                         // d.egységárát

                    .WithColumn("LineNatureIndicator").AsString().NotNullable()
                    .WithColumn("LineDescription").AsString().NotNullable()
                    .WithColumn("Quantity").AsDecimal().NotNullable().WithDefaultValue(0)
                    .WithColumn("UnitOfMeasure").AsString().NotNullable().WithDefaultValue("")
                    .WithColumn("UnitPrice").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("UnitPriceHUF").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("LineNetAmount").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("LineNetAmountHUF").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("lineVatAmount").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("lineVatAmountHUF").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("lineGrossAmountNormal").AsCurrency().NotNullable().WithDefaultValue(0)
                    .WithColumn("lineGrossAmountNormalHUF").AsCurrency().NotNullable().WithDefaultValue(0)

                    //lineModificationReference, javítószámla
                    .WithColumn("LineNumberReference").AsInt16().Nullable()
                    .WithColumn("LineOperation").AsString().Nullable()

                    // aggregateInvoiceLineData, gyűjtőszámla
                    .WithColumn("LineExchangeRate").AsDecimal().Nullable()
                    .WithColumn("LineDeliveryDate").AsDateTime2().Nullable()
                    .WithColumn("DeliveryNote").AsString().Nullable()                   //Szállítólevél-szám
                    .WithColumn("DeliveryNoteInvoiceID").AsInt64().Nullable()           //Szállítólevél ID
                    .WithColumn("DeliveryNoteLineNumber").AsInt16().Nullable()       //Kapcsolódó szállítólevél sor

                    //Termékdíj - A környezetvédelmi termékdíj-kötelezettség átvállalása
                    .WithColumn("TakeoverReason").AsString().Nullable()
                    .WithColumn("TakeoverAmount").AsDecimal().Nullable()
                    .WithColumn("TakeoverProductCodeCategory").AsString().Nullable()
                    .WithColumn("TakeoverProductCodeValue").AsString().Nullable()

                    //Termékdíj
                    .WithColumn("ProductFeeProductCodeCategory").AsString().Nullable()
                    .WithColumn("ProductFeeProductCodeValue").AsString().Nullable()
                    .WithColumn("ProductFeeQuantity").AsDecimal().Nullable()
                    .WithColumn("ProductFeeMeasuringUnit").AsString().Nullable()
                    .WithColumn("ProductFeeRate").AsDecimal().Nullable()
                    .WithColumn("ProductFeeAmount").AsDecimal().Nullable();

            Create.Index("INX_InvoiceLineProduct")
                         .OnTable("InvoiceLine")
                         .OnColumn("ProductID").Ascending()
                         .WithOptions().NonClustered();

            Create.Table("AdditionalInvoiceLineData")
                    .WithColumn("ID").AsInt64().NotNullable().PrimaryKey().Identity()
                    .WithColumn("CreateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("UpdateTime").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("Deleted").AsBoolean().WithDefaultValue(false)
                    .WithColumn("InvoiceLineID").AsInt64().NotNullable().ForeignKey()
                    .WithColumn("DataName").AsString().NotNullable()
                    .WithColumn("DataDescription").AsString(int.MaxValue).Nullable()
                    .WithColumn("DataValue").AsString().Nullable();
        }
    }
}
