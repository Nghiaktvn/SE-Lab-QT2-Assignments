SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF DB_ID(N'WholesaleManagement') IS NULL CREATE DATABASE WholesaleManagement;
GO
ALTER DATABASE WholesaleManagement SET AUTO_CLOSE OFF;
GO
USE WholesaleManagement;
GO
IF OBJECT_ID('dbo.Agent','U') IS NULL
CREATE TABLE dbo.Agent(AgentID INT IDENTITY PRIMARY KEY, AgentName NVARCHAR(120) NOT NULL, Address NVARCHAR(250) NOT NULL, Phone NVARCHAR(25) NOT NULL UNIQUE, IsActive BIT NOT NULL DEFAULT 1);
IF OBJECT_ID('dbo.Item','U') IS NULL
CREATE TABLE dbo.Item(ItemID INT IDENTITY PRIMARY KEY, ItemName NVARCHAR(120) NOT NULL, Size NVARCHAR(30) NOT NULL, Price DECIMAL(12,2) NOT NULL CHECK(Price>0), StockQuantity INT NOT NULL CHECK(StockQuantity>=0), IsActive BIT NOT NULL DEFAULT 1, RowVersion ROWVERSION);
IF OBJECT_ID('dbo.Order','U') IS NULL
CREATE TABLE dbo.[Order](OrderID INT IDENTITY PRIMARY KEY, OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), AgentID INT NOT NULL, OrderStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK(OrderStatus IN ('Pending','Processing','Shipped','Cancelled')), PaymentStatus NVARCHAR(16) NOT NULL CONSTRAINT DF_Order_PaymentStatus DEFAULT 'Unpaid', PaidAtUtc DATETIME2 NULL, PaymentReference NVARCHAR(80) NULL, CONSTRAINT FK_Order_Agent FOREIGN KEY(AgentID) REFERENCES dbo.Agent(AgentID));
IF OBJECT_ID('dbo.OrderDetail','U') IS NULL
CREATE TABLE dbo.OrderDetail(ID INT IDENTITY PRIMARY KEY, OrderID INT NOT NULL, ItemID INT NOT NULL, Quantity INT NOT NULL CHECK(Quantity>0), UnitAmount DECIMAL(12,2) NOT NULL CHECK(UnitAmount>0), CONSTRAINT UQ_OrderDetail UNIQUE(OrderID,ItemID), CONSTRAINT FK_Detail_Order FOREIGN KEY(OrderID) REFERENCES dbo.[Order](OrderID) ON DELETE CASCADE, CONSTRAINT FK_Detail_Item FOREIGN KEY(ItemID) REFERENCES dbo.Item(ItemID));
IF OBJECT_ID('dbo.Users','U') IS NULL
CREATE TABLE dbo.Users(UserID INT IDENTITY PRIMARY KEY, UserName NVARCHAR(80) NOT NULL UNIQUE, Email NVARCHAR(254) NOT NULL UNIQUE, PasswordHash NVARCHAR(255) NOT NULL, IsLocked BIT NOT NULL DEFAULT 0, Role NVARCHAR(20) NOT NULL CHECK(Role IN ('Admin','Agent')), AgentID INT NULL, FailedLoginCount INT NOT NULL DEFAULT 0, CONSTRAINT FK_User_Agent FOREIGN KEY(AgentID) REFERENCES dbo.Agent(AgentID), CONSTRAINT CK_User_RoleAgent CHECK((Role='Agent' AND AgentID IS NOT NULL) OR (Role='Admin' AND AgentID IS NULL)));
GO
-- Additive upgrade: preserves existing demo orders and inventory.
IF COL_LENGTH('dbo.Order','RequestKey') IS NULL ALTER TABLE dbo.[Order] ADD RequestKey UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.Order','PaymentStatus') IS NULL ALTER TABLE dbo.[Order] ADD PaymentStatus NVARCHAR(16) NOT NULL CONSTRAINT DF_Order_PaymentStatus DEFAULT 'Unpaid' WITH VALUES;
IF COL_LENGTH('dbo.Order','PaidAtUtc') IS NULL ALTER TABLE dbo.[Order] ADD PaidAtUtc DATETIME2 NULL;
IF COL_LENGTH('dbo.Order','PaymentReference') IS NULL ALTER TABLE dbo.[Order] ADD PaymentReference NVARCHAR(80) NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Order_RequestKey' AND object_id=OBJECT_ID('dbo.Order'))
 CREATE UNIQUE INDEX UX_Order_RequestKey ON dbo.[Order](RequestKey) WHERE RequestKey IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Order_PaymentReference' AND object_id=OBJECT_ID('dbo.Order'))
 CREATE UNIQUE INDEX UX_Order_PaymentReference ON dbo.[Order](PaymentReference) WHERE PaymentReference IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Order_PaymentStatus' AND parent_object_id=OBJECT_ID('dbo.Order'))
 ALTER TABLE dbo.[Order] ADD CONSTRAINT CK_Order_PaymentStatus CHECK(PaymentStatus IN ('Unpaid','Paid','Refunded'));
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Order_PaymentAudit' AND parent_object_id=OBJECT_ID('dbo.Order'))
 ALTER TABLE dbo.[Order] ADD CONSTRAINT CK_Order_PaymentAudit CHECK((PaymentStatus='Unpaid' AND PaidAtUtc IS NULL AND PaymentReference IS NULL) OR (PaymentStatus IN ('Paid','Refunded') AND PaidAtUtc IS NOT NULL AND PaymentReference IS NOT NULL));
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Agent)
BEGIN
 INSERT dbo.Agent(AgentName,Address,Phone) VALUES
 (N'An Phat Retail',N'12 Nguyen Hue, District 1, HCMC','0901000001'),(N'Minh Chau Trading',N'28 Le Loi, District 1, HCMC','0901000002'),(N'Saigon Mart',N'45 Vo Van Tan, District 3, HCMC','0901000003'),(N'Green Basket',N'8 Phan Xich Long, Phu Nhuan','0901000004'),(N'Nam Viet Shop',N'11 Tran Hung Dao, District 5','0901000005'),(N'Lotus Wholesale',N'77 Xa Lo Ha Noi, Thu Duc','0901000006'),(N'Binh Minh Store',N'19 Cach Mang Thang 8, District 10','0901000007'),(N'Happy Home',N'3 Nguyen Van Linh, District 7','0901000008'),(N'Co.op Partner A',N'16 Dien Bien Phu, Binh Thanh','0901000009'),(N'Viet Choice',N'92 Quang Trung, Go Vap','0901000010'),(N'Phu Quy Distributor',N'51 Kinh Duong Vuong, Binh Tan','0901000011'),(N'An Khang Supply',N'6 Hoang Dieu, District 4','0901000012'),(N'Thien Loc Market',N'20 Le Van Viet, Thu Duc','0901000013'),(N'Song Xanh Goods',N'32 Nguyen Oanh, Go Vap','0901000014'),(N'Gia Bao Retail',N'14 Truong Chinh, Tan Binh','0901000015');
 INSERT dbo.Item(ItemName,Size,Price,StockQuantity) VALUES
 (N'Arabica Coffee Beans','1 kg',245000,120),(N'Robusta Coffee Beans','1 kg',185000,90),(N'Green Tea Pack','500 g',98000,65),(N'Black Tea Pack','500 g',92000,75),(N'Rice Paper','500 g',32000,200),(N'Fish Sauce Premium','750 ml',68000,110),(N'Coconut Water','12 x 330 ml',156000,80),(N'Dried Mango','500 g',125000,45),(N'Cashew Nuts','1 kg',290000,8),(N'Chili Sauce','500 ml',42000,140),(N'Instant Noodles','30 pack',135000,180),(N'Jasmine Rice','5 kg',145000,95),(N'Cooking Oil','2 l',98000,70),(N'Sea Salt','1 kg',18000,240),(N'Brown Sugar','1 kg',35000,100);
 INSERT dbo.Users(UserName,Email,PasswordHash,Role,AgentID) VALUES
 ('admin',N'admin@wholesale.local',N'pbkdf2$120000$akZ46vbn7snMFdfXxDTDmQ==$8hjESQNmoMX9oLYEzshfd3enHSqIEkYhhK5Jaiif5x0=','Admin',NULL),('agent01',N'agent01@wholesale.local',N'pbkdf2$120000$12/bt0OrR2aKpddPQLKYlQ==$eF4MonNuggLSxqWgEVRkmLsVqdQlNdC06ONJMV/tj2U=','Agent',1),('agent02',N'agent02@wholesale.local',N'pbkdf2$120000$C9+Sfp76Alj+4dHXXGyHMA==$hG92odiGxy147uXm+0Vr0w0arbu7Ku9NJRgVAPruWwY=','Agent',2),('agent03',N'agent03@wholesale.local',N'pbkdf2$120000$kG9oJJ+pCuKvq8w3zN7smg==$dWqRiRXpb/2i0zEkXVyMAxlNNTYT0qiR7P1drkWikw0=','Agent',3),('agent04',N'agent04@wholesale.local',N'pbkdf2$120000$DVd4idhAxnMyzW7hR+rhtA==$iEHefa4dCOBMnQs5psHIgaUWwIbTqVSm0dF05P0jRdw=','Agent',4),('agent05',N'agent05@wholesale.local',N'pbkdf2$120000$K3G/q4AJpYllzD0554c1Zg==$kCM8ae5kLDRfD8TtQItWb+hVz37SZv9RD+nDfL/00dg=','Agent',5),('agent06',N'agent06@wholesale.local',N'pbkdf2$120000$qDT+AHkvh58HSGDfyPCcGg==$w/fryNcC3RBbozMkaKVuVvY9SgUK0pkIJGXnGfl6qy0=','Agent',6),('agent07',N'agent07@wholesale.local',N'pbkdf2$120000$FfIoqkASDlQXd2YT4MVHdg==$eGn24dGTV3y71tle1Z5R459EUCPT95HuwYINbKF3N34=','Agent',7),('agent08',N'agent08@wholesale.local',N'pbkdf2$120000$xb+8YmL7zZ3EyAqG0DT6cg==$S9cRLfNja0nfF45Vw9OF9WDAL90ctshWB5p9qaQTCkk=','Agent',8),('agent09',N'agent09@wholesale.local',N'pbkdf2$120000$4iuL19tCPKzpb+hBeYubrg==$u0Op1ebbk3bT035gcZCnxAG7PY8Fh/6bK7hOEoxex98=','Agent',9),('agent10',N'agent10@wholesale.local',N'pbkdf2$120000$BMxgRRddYyTK2lVH2ikm0g==$0155N4sheRoWiwyK3ARnygP7UB7gL6oQpFX5vncYRFk=','Agent',10),('agent11',N'agent11@wholesale.local',N'pbkdf2$120000$y2PdnAuKaX1QQaFwJTClCw==$hMvl6TPZW6Dq3EwGwRmuyl8elNfYF6bhw1t7KaTwnSg=','Agent',11),('agent12',N'agent12@wholesale.local',N'pbkdf2$120000$f/7svDM0QV72oXRIyMMMpg==$FgOdM4GGXljz33fKxajdFlMRks1WjF70S6jI5/Tujdw=','Agent',12),('agent13',N'agent13@wholesale.local',N'pbkdf2$120000$QqvvhUGRVvI3NDRYjnp31A==$qclAIM4er+oLpzFKZdCy9OcqcMJ3PW8MuVlhb0iPpfk=','Agent',13),('agent14',N'agent14@wholesale.local',N'pbkdf2$120000$zhPyWWoqLBTy7OxU2rwaUg==$KL3vqOHp2uoi/KsBv5ZMTMQccEebepred3Q277avvog=','Agent',14),('agent15',N'agent15@wholesale.local',N'pbkdf2$120000$jcsCUONHwanScFCoPkhelA==$jHi+Lh1aiX9Nyj8NUK1spEruFiKg0FhK+1p7McqooWI=','Agent',15);
 DECLARE @i INT=1;
 WHILE @i<=15 BEGIN
  INSERT dbo.[Order](AgentID,OrderStatus,OrderDate) VALUES(((@i-1)%15)+1,CASE WHEN @i%3=0 THEN 'Shipped' WHEN @i%3=1 THEN 'Pending' ELSE 'Processing' END,DATEADD(day,-@i,SYSUTCDATETIME()));
  INSERT dbo.OrderDetail(OrderID,ItemID,Quantity,UnitAmount) VALUES(SCOPE_IDENTITY(),((@i-1)%15)+1,1+(@i%5),(SELECT Price FROM dbo.Item WHERE ItemID=((@i-1)%15)+1));
  SET @i+=1;
 END
END;
GO
