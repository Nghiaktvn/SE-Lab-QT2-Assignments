IF DB_ID(N'SmartTicketing') IS NULL CREATE DATABASE SmartTicketing;
GO
ALTER DATABASE SmartTicketing SET AUTO_CLOSE OFF;
GO
USE SmartTicketing;
GO
IF OBJECT_ID('dbo.TicketTransaction','U') IS NULL
CREATE TABLE dbo.TicketTransaction (
 TransactionId BIGINT IDENTITY PRIMARY KEY,
 DestinationCode NVARCHAR(20) NOT NULL,
 DestinationName NVARCHAR(100) NOT NULL,
 Fare DECIMAL(12,2) NOT NULL CHECK (Fare > 0),
 PaymentMethod NVARCHAR(20) NOT NULL CHECK (PaymentMethod IN ('Card','Wallet')),
 PaymentReference NVARCHAR(80) NOT NULL UNIQUE,
 Barcode NVARCHAR(80) NOT NULL UNIQUE,
 Status NVARCHAR(20) NOT NULL CHECK (Status IN ('Paid','Refunded')),
 CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Destination','U') IS NULL
CREATE TABLE dbo.Destination (
 DestinationCode NVARCHAR(20) PRIMARY KEY,
 DestinationName NVARCHAR(100) NOT NULL,
 Fare DECIMAL(12,2) NOT NULL CHECK (Fare > 0),
 IsActive BIT NOT NULL DEFAULT 1
);
IF NOT EXISTS (SELECT 1 FROM dbo.Destination)
INSERT dbo.Destination (DestinationCode,DestinationName,Fare) VALUES
('D01',N'Ben Thanh',7000),('D02',N'Tan Cang',12000),('D03',N'Suoi Tien',20000),
('D04',N'University Village',17000),('D05',N'Tan Son Nhat',25000),('D06',N'Cho Lon',15000),
('D07',N'Binh Thanh',10000),('D08',N'Thu Duc',18000),('D09',N'District 7',16000),('D10',N'Central Station',9000),
('D11',N'An Phu',14000),('D12',N'Phu Nhuan',11000),('D13',N'Go Vap',13000),('D14',N'Binh Tan',19000),('D15',N'City Hall',8000);
GO
-- Additive upgrade for the complete demo. Existing tickets are retained.
IF COL_LENGTH('dbo.TicketTransaction','PrintStatus') IS NULL
 ALTER TABLE dbo.TicketTransaction ADD PrintStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK(PrintStatus IN ('Pending','Printed','Failed'));
IF COL_LENGTH('dbo.TicketTransaction','ValidUntilUtc') IS NULL
 ALTER TABLE dbo.TicketTransaction ADD ValidUntilUtc DATETIME2 NULL;
DECLARE @paymentCheck SYSNAME;
SELECT @paymentCheck=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.TicketTransaction') AND definition LIKE '%PaymentMethod%';
IF @paymentCheck IS NOT NULL EXEC('ALTER TABLE dbo.TicketTransaction DROP CONSTRAINT '+@paymentCheck);
ALTER TABLE dbo.TicketTransaction ADD CONSTRAINT CK_Ticket_PaymentMethod CHECK (PaymentMethod IN ('Card','BankQr','Wallet'));
GO
