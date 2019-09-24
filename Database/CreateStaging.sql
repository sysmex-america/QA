if exists (select * from sysobjects where name = 'CRMIntegrationExecutionLog')
	drop table CRMIntegrationExecutionLog;
go

create table CRMIntegrationExecutionLog (
	CRMIntegrationExecutionLogID int identity(1,1) not null
		constraint PK_CRMIntegrationExecutionLog primary key clustered,
	IntegrationName nvarchar(100) not null,
	StartDate datetime null,
	EndDate datetime null,
	StatusCode nvarchar(20) null,
	StatusMessage nvarchar(2000) null)
go

alter table CRMIntegrationExecutionLog
	add FileNameProcessed nvarchar(100) null
go

if exists (select * from sysobjects where name = 'CRMIntegrationErrorLog')
	drop table CRMIntegrationErrorLog;
go

create table CRMIntegrationErrorLog (
	IntegrationName nvarchar(100) null,
	CRMIntegrationExecutionLogID int null
		constraint FK_CRMIntegrationExecutionLog foreign key references CRMIntegrationExecutionLog(CRMIntegrationExecutionLogID),
	ObjectID nvarchar(100) null,
	ErrorMessage nvarchar(2048) null,
	ErrorDate datetime not null
		constraint DF_ErrorDate default getdate())
go

if exists (select * from sysobjects where name = 'CRMIntegrationErrorLog')
	drop table CRMIntegrationSuccessLog;
go

create table CRMIntegrationSuccessLog (
	IntegrationName nvarchar(100) null,
	CRMIntegrationExecutionLogID int null
		constraint FK_Success_CRMIntegrationExecutionLog foreign key references CRMIntegrationExecutionLog(CRMIntegrationExecutionLogID),
	ObjectID nvarchar(100) null,
	CRMID uniqueidentifier null,
	RecordAction nchar(1) null,
	ProcessDate datetime not null
		constraint DF_Success_ErrorDate default getdate())
go

if exists (select * from sysobjects where name = 'CRMLabInfo')
	drop table CRMLabInfo;
go

--drop table CRMLabInfo
create table CRMLabInfo (
	smx_labid uniqueidentifier not null
		constraint PK_CRMLabInfo primary key clustered,
	smx_name nvarchar(100) null,
	smx_account uniqueidentifier null,
	smx_sapid nvarchar(100) null,
	ownerid uniqueidentifier not null,
	statecode int not null,
	statuscode int not null)
go

if exists (select * from sysobjects where name = 'stageProduct')
	drop table stageProduct;
go

create table stageProduct (
	ID int identity(1,1) not null primary key clustered,
	ExternalID nvarchar(100) null,
	ProductCode nvarchar(255) null,
	[Name] nvarchar(255) null,
	[Type] nvarchar(50) null,
	ConfigurationType nvarchar(50) null,
	[Description] nvarchar(max) null,
	ext_DescriptionFrench nvarchar(max) null,
	Family nvarchar(50) null,
	ext_productgroup nvarchar(50) null,
	ext_OpexModel nvarchar(50) null,
	ext_SOW nvarchar(max) null,
	ext_AdditionalInfo nvarchar(max) null,
	ext_TrainingSeats nvarchar(50) null,
	ext_TrainingSeatCost nvarchar(50) null,
	ext_training nvarchar(255) null,
	ext_weightage nvarchar(255) null,
	ext_SkipWarranty nvarchar(50) null,
	ext_leaseresidual nvarchar(255) null,
	ext_CompassEligibleInstruments nvarchar(50) null,
	ext_US nvarchar(50) null,
	ext_Canada nvarchar(50) null,
	ext_Latam nvarchar(50) null,
	ext_MinTestNum nvarchar(50) null,
	ext_MaxTestNum nvarchar(50) null,
	CRMIntegrationExecutionLogID int null)
go

if exists (select * from sysobjects where name = 'stagePriceListHeader')
	drop table stagePriceListHeader;
go

create table stagePriceListHeader (
	ID int identity(1,1) not null primary key clustered,
	ExternalId nvarchar(100) null,
	ProductCode nvarchar(255) null,
	[Name] nvarchar(255) null,
	CurrencyId nvarchar(255) null,
	[Description] nvarchar(max) null,
	EffectiveDate datetime null,
	ExpirationDate datetime null,
	CRMIntegrationExecutionLogID int null)
go

if exists (select * from sysobjects where name = 'stagePriceList')
	drop table stagePriceList;
go

create table stagePriceList (
	ID int identity(1,1) not null primary key clustered,
	ExternalId nvarchar(100) null,
	[Name] nvarchar(255) null,
	[Active] bit null,
	Cost money null,
	CurrencyId nvarchar(255) null,
	[Description] nvarchar(max) null,
	EffectiveDate datetime null,
	ExpirationDate datetime null,
	ListPrice money null,
	MaxPrice money null,
	MinPrice money null,
	MinUsageQuantity decimal(19,5) null,
	PriceListId nvarchar(100) null,
	PriceMethod nvarchar(255) null,
	ProductActive bit null,
	ProductCode nvarchar(255) null,
	ProductDescription nvarchar(max) null,
	ProductFamily nvarchar(255) null,
	ProductId nvarchar(100) null,
	ProductName nvarchar(255) null,
	SalesOrganization nvarchar(255) null,
	CRMIntegrationExecutionLogID int null)
go

if exists (select * from sysobjects where name = 'stagePricingIHNGPO')
	drop table stagePricingIHNGPO;
go

create table stagePricingIHNGPO (
	ID int identity(1,1) not null primary key clustered,
	ExternalId nvarchar(100) null,
	[Name] nvarchar(255) null,
	ProductName nvarchar(255) null,
	ProductGroupId nvarchar(255) null,
	EffectiveDate datetime null,
	EndDate datetime null,
	Price money null,
	Tier nvarchar(255) null,
	[Type] nvarchar(255) null,
	AffiliationID nvarchar(255) null,
	Currency nvarchar(255) null,
	SalesOrganization nvarchar(255) null,
	IHNGPO nvarchar(255) null,
	CRMIntegrationExecutionLogID int null)
go


if exists (select * from sysobjects where name = 'stageAddress')
	drop table stageAddress;
go

CREATE TABLE [dbo].[stageAddress](
	ID int identity(1,1) not null primary key clustered,
	[ExternalId] [nvarchar](50) NULL,
	[Name] [nvarchar](255) NULL,
	[SAPID] [nvarchar](50) NULL,
	[Name2] [nvarchar](50) NULL,
	[Address] [nvarchar](50) NULL,
	[Address2] [nvarchar](50) NULL,
	[Address3] [nvarchar](50) NULL,
	[Address4] [nvarchar](50) NULL,
	[Address5] [nvarchar](50) NULL,
	[ZipCode] [nvarchar](50) NULL,
	[City] [nvarchar](50) NULL,
	[State] [nvarchar](50) NULL,
	[Country] [nvarchar](50) NULL,
	[Location1] [nvarchar](50) NULL,
	[Location2] [nvarchar](50) NULL,
	[CheckDigit] [nvarchar](50) NULL,
	[HEMTier] [nvarchar](50) NULL,
	[UFTier] [nvarchar](50) NULL,
	[HemeGPO] [nvarchar](50) NULL,
	[HemeGPOID] [nvarchar](50) NULL,
	[UFGPO] [nvarchar](50) NULL,
	[UFGPOID] [nvarchar](50) NULL,
	[IHN] [nvarchar](50) NULL,
	[SecondaryIHN] [nvarchar](50) NULL,
	[SalesOrganization] [nvarchar](50) NULL,
	[DistributionChannel] [nvarchar](50) NULL,
	[Division] [nvarchar](50) NULL,
	[SourceCode] [nvarchar](50) NULL,
	[ElectronicMethod] [nvarchar](50) NULL,
	[CustomerType] [nvarchar](50) NULL,
	[TermsOfPayment] [nvarchar](50) NULL,
	[TermsOfPayment2] [nvarchar](50) NULL,
	[OrderBlock] [nvarchar](50) NULL,
	[DeliveryBlock] [nvarchar](50) NULL,
	[BillingBlock] [nvarchar](50) NULL,
	CRMIntegrationExecutionLogID int null)
GO


if exists (select * from sysobjects where name = 'smx_product')
	drop table smx_product;
go

CREATE TABLE [dbo].[smx_product](
	[smx_productid] [uniqueidentifier] NULL,
	[smx_name] [nvarchar](100) NULL,
	[smx_description] [nvarchar](max) NULL,
	[statecode] [int] NULL,
	[statuscode] [int] NULL,
	[smx_family] [int] NULL,
	[smx_familyname] [nvarchar](255) NULL,
	[statecodename] [nvarchar](255) NULL,
	[statuscodename] [nvarchar](255) NULL,
	[smx_sapid] [nvarchar](100) NULL
) 
GO

create index idx_sapid on smx_product(smx_sapid);
go

if exists (select * from sysobjects where name = 'smx_priceheader')
	drop table smx_priceheader;
go

CREATE TABLE [dbo].[smx_priceheader](
	[createdby] [uniqueidentifier] NULL,
	[createdbyname] [nvarchar](200) NULL,
	[createdon] [datetime] NULL,
	[modifiedby] [uniqueidentifier] NULL,
	[modifiedbyname] [nvarchar](200) NULL,
	[modifiedon] [datetime] NULL,
	[overriddencreatedon] [datetime] NULL,
	[ownerid] [uniqueidentifier] NULL,
	[owneridname] [nvarchar](200) NULL,
	[owneridtype] [nvarchar](64) NULL,
	[owningbusinessunit] [uniqueidentifier] NULL,
	[owningteam] [uniqueidentifier] NULL,
	[owninguser] [uniqueidentifier] NULL,
	[smx_currencyid] [uniqueidentifier] NULL,
	[smx_currencyidname] [nvarchar](100) NULL,
	[smx_description] [nvarchar](max) NULL,
	[smx_effectivedate] [datetime] NULL,
	[smx_expirationdate] [datetime] NULL,
	[smx_name] [nvarchar](100) NULL,
	[smx_pricelistheaderid] [uniqueidentifier] NULL,
	[statecode] [int] NULL,
	[statecodename] [nvarchar](255) NULL,
	[statuscode] [int] NULL,
	[statuscodename] [nvarchar](255) NULL
)
GO

