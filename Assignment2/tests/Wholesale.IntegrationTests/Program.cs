using System.Diagnostics;
using System.Text.Json;
using AgentPortal;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Wholesale.AdminWinForms;
using Wholesale.Domain;

var root=Path.GetFullPath(args.FirstOrDefault()??Path.Combine(AppContext.BaseDirectory,"../../../../.."));
var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(root,"src/AgentPortal/appsettings.json")).AddEnvironmentVariables().Build();
var cs=config.GetConnectionString("Wholesale")??throw new InvalidOperationException("Missing Wholesale configuration.");
var portal=new WholesaleStore(config);var admin=new SqlAdminStore(cs);
var prefix="QA"+Guid.NewGuid().ToString("N")[..12];
var trigger=prefix+"Fault";
var checks=new List<object>();var log=new List<string>();var passed=0;var triggerCreated=false;
var baseline=await Counts();
void Check(bool condition,string title){if(!condition)throw new InvalidOperationException("FAIL: "+title);passed++;log.Add("PASS: "+title);Console.WriteLine(log[^1]);checks.Add(new{title,status="PASS"});}
async Task<int> Scalar(string sql,params (string,object)[] values){await using var c=new SqlConnection(cs);await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);foreach(var (name,value) in values)cmd.Parameters.AddWithValue(name,value);return Convert.ToInt32(await cmd.ExecuteScalarAsync());}
async Task Execute(string sql,params (string,object)[] values){await using var c=new SqlConnection(cs);await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);foreach(var (name,value) in values)cmd.Parameters.AddWithValue(name,value);await cmd.ExecuteNonQueryAsync();}
async Task<string> Counts(){await using var c=new SqlConnection(cs);await c.OpenAsync();await using var cmd=new SqlCommand("SELECT (SELECT COUNT(*) FROM dbo.Agent),(SELECT COUNT(*) FROM dbo.Item),(SELECT COUNT(*) FROM dbo.[Order]),(SELECT COUNT(*) FROM dbo.OrderDetail),(SELECT COUNT(*) FROM dbo.Users)",c);await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();return string.Join("/",Enumerable.Range(0,5).Select(i=>r.GetInt32(i)));}
async Task ExpectFailure(Func<Task> action,string title){try{await action();}catch(Exception e)when(e is OrderException or InvalidOperationException or SqlException or ArgumentException){Check(true,title);return;}throw new InvalidOperationException("Expected failure: "+title);}
try
{
 admin.RegisterAgent(new(0,prefix+" Partner A","12 Demo Street",prefix+"01"),prefix+"a",prefix+"a@example.test","Demo123!");
 admin.RegisterAgent(new(0,prefix+" Partner B","24 Demo Street",prefix+"02"),prefix+"b",prefix+"b@example.test","Demo123!");
 var agent=admin.GetAgents().Single(x=>x.Name==prefix+" Partner A");var other=admin.GetAgents().Single(x=>x.Name==prefix+" Partner B");
 Check((await portal.AuthenticateAgentAsync(prefix+"a@example.test","Demo123!"))?.AgentId==agent.Id,"Newly registered agent can sign in to portal");
 Check(!admin.AuthenticateAdmin(prefix+"a","Demo123!"),"Agent role cannot access desktop Admin");
 var agentCount=await Scalar("SELECT COUNT(*) FROM dbo.Agent");
 await ExpectFailure(()=>Task.Run(()=>admin.RegisterAgent(new(0,prefix+" Failed","rollback",prefix+"03"),prefix+"collision",prefix+"a@example.test","Demo123!")),"Duplicate account rolls back agent registration");
 Check(await Scalar("SELECT COUNT(*) FROM dbo.Agent")==agentCount,"Failed registration leaves no orphan company");
 admin.SaveItem(new(0,prefix+" Product A","1 kg",100,10));admin.SaveItem(new(0,prefix+" Product B","500 g",75,10));admin.SaveItem(new(0,prefix+" Fault","1 box",25,3));
 var a=admin.GetItems().Single(x=>x.Name==prefix+" Product A");var b=admin.GetItems().Single(x=>x.Name==prefix+" Product B");var fault=admin.GetItems().Single(x=>x.Name==prefix+" Fault");
 var key=Guid.NewGuid();var order=await portal.PlaceOrderAsync(agent.Id,[(a.Id,2),(b.Id,3)],key);
 Check((await portal.GetInvoiceAsync(order,agent.Id))?.Total==425,"Master-detail invoice uses server prices and totals");
 Check((await portal.GetInvoiceAsync(order,agent.Id)) is { AgentAddress:"12 Demo Street", AgentPhone:var phone }&&phone==prefix+"01","Invoice carries the registered delivery contact for print");
 Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==8&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",b.Id))==7,"Master-detail order deducts all stocks");
 Check(await portal.GetInvoiceAsync(order,other.Id) is null,"Foreign-agent invoice is inaccessible");
 Check((await portal.GetOrdersAsync(other.Id)).Count==0,"My Orders filters by authenticated agent");
 Check(await portal.PlaceOrderAsync(agent.Id,[(a.Id,2),(b.Id,3)],key)==order,"Replayed request key returns existing order");
 Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==8,"Replayed request does not deduct stock twice");
 var singleProductOrder=await portal.PlaceOrderAsync(agent.Id,[(a.Id,1)]);
 var unpaidInvoice=await portal.GetInvoiceAsync(singleProductOrder,agent.Id);
 Check(unpaidInvoice is { PaymentStatus:"Unpaid", Lines.Count:1, Total:100 }&&unpaidInvoice.Lines[0] is { Quantity:1, ItemName:var singleName }&&singleName==a.Name,"One-product cart creates an unpaid invoice with the server price");
 await ExpectFailure(()=>portal.ConfirmDemoPaymentAsync(singleProductOrder,other.Id,Guid.NewGuid()),"Another agent cannot confirm payment for this invoice");
 Check((await portal.GetInvoiceAsync(singleProductOrder,agent.Id))?.PaymentStatus=="Unpaid","Rejected foreign payment leaves invoice unpaid");
 var paymentKey=Guid.NewGuid();var paymentReference=await portal.ConfirmDemoPaymentAsync(singleProductOrder,agent.Id,paymentKey);
 Check(paymentReference.StartsWith("DEMO-",StringComparison.Ordinal)&&paymentReference.Length==37,"Demo payment returns a unique classroom reference");
 var paidInvoice=await portal.GetInvoiceAsync(singleProductOrder,agent.Id);
 Check(paidInvoice is { PaymentStatus:"Paid", PaymentReference: not null, PaidAtUtc: not null, Lines.Count:1, Total:100 },"Confirmed one-product invoice is persisted as paid with timestamp and reference");
 Check(await portal.ConfirmDemoPaymentAsync(singleProductOrder,agent.Id,Guid.NewGuid())==paymentReference,"Retrying payment confirmation returns the original reference without duplicate payment");
 Check((await portal.GetOrdersAsync(agent.Id)).Single(x=>x.Id==singleProductOrder).PaymentStatus=="Paid","My Orders displays the confirmed payment state");
 await ExpectFailure(()=>Task.Run(()=>admin.SaveItem(a with {Name=prefix+" stale"})),"Stale product editor cannot overwrite stock after web order");
 var before=await Counts();
 await ExpectFailure(()=>portal.PlaceOrderAsync(agent.Id,[(a.Id,1),(b.Id,999)]),"Insufficient stock rejects multi-line order");
 Check(before==await Counts()&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==7,"Insufficient-stock rejection leaves no order or stock change");
 await Execute($"CREATE TRIGGER dbo.[{trigger}] ON dbo.OrderDetail AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted WHERE ItemID={fault.Id}) THROW 51001,'Controlled integration-test failure',1; END");
 triggerCreated=true;
 before=await Counts();
 await ExpectFailure(()=>portal.PlaceOrderAsync(agent.Id,[(a.Id,1),(fault.Id,1)]),"Controlled second-detail failure exercises database rollback");
 Check(before==await Counts()&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==7&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",fault.Id))==3,"Mid-transaction failure rolls back header, first detail, and stock deduction");
 await Execute($"DROP TRIGGER dbo.[{trigger}]");triggerCreated=false;
 admin.AdvanceOrder(singleProductOrder,"Pending","Cancelled");
 Check((await portal.GetInvoiceAsync(singleProductOrder,agent.Id))?.PaymentStatus=="Refunded","Cancelling a paid classroom order records a refund on its invoice");
 Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==8,"Cancelling a paid order restores its reserved stock exactly once");
 admin.AdvanceOrder(order,"Pending","Cancelled");
 Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==10&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",b.Id))==10,"Cancellation atomically restores every reserved quantity");
 await ExpectFailure(()=>Task.Run(()=>admin.AdvanceOrder(order,"Pending","Cancelled")),"Repeated cancellation cannot restore stock twice");
 Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==10,"Repeated cancellation leaves restored stock unchanged");
 var shipping=await portal.PlaceOrderAsync(agent.Id,[(b.Id,1)]);
 admin.AdvanceOrder(shipping,"Pending","Processing");admin.AdvanceOrder(shipping,"Processing","Shipped");
 await ExpectFailure(()=>Task.Run(()=>admin.AdvanceOrder(shipping,"Shipped","Cancelled")),"Shipped orders cannot be cancelled");
 var html=admin.ManifestHtml(shipping);
 Check(html.Contains("12 Demo Street")&&html.Contains("75 VND")&&html.Contains("@media print"),"Exported shipping manifest contains address, totals, and print stylesheet");
 Directory.CreateDirectory(Path.Combine(root,"docs/evidence"));await File.WriteAllTextAsync(Path.Combine(root,"docs/evidence/sample-shipping-manifest.html"),html);
 var report=admin.ReportFiltered(agent.Id,DateTime.Today.AddDays(-1),DateTime.Today.AddDays(1));
 Check(report.Contains(prefix+" Partner A")&&!report.Contains(prefix+" Partner B")&&report.Contains("75 VND"),"Date and agent filters report shipped revenue only");
 // Exact-stock competition uses a separate item; only one checkout can consume it.
 admin.SaveItem(new(0,prefix+" Last Unit","1 box",50,1));var last=admin.GetItems().Single(x=>x.Name==prefix+" Last Unit");
 async Task<bool> Compete(){try{await portal.PlaceOrderAsync(agent.Id,[(last.Id,1)],Guid.NewGuid());return true;}catch(OrderException){return false;}}
 var competitors=await Task.WhenAll(Compete(),Compete());
 Check(competitors.Count(x=>x)==1&&await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",last.Id))==0,"Concurrent checkouts for last unit allow exactly one order");
 for(var i=0;i<5;i++)await portal.AuthenticateAgentAsync(prefix+"b@example.test","incorrect");
 Check(await portal.AuthenticateAgentAsync(prefix+"b@example.test","Demo123!") is null,"Agent account locks on fifth incorrect password");
 admin.UnlockUser(prefix+"b");Check(await portal.AuthenticateAgentAsync(prefix+"b@example.test","Demo123!") is not null,"Admin unlock restores Agent access");
 admin.DeactivateAgent(other.Id);Check(!await portal.IsAgentActiveAsync(other.Id),"Deactivated agent invalidates existing portal session");
 await ExpectFailure(()=>portal.PlaceOrderAsync(other.Id,[(a.Id,1)]),"Deactivated agent cannot place an order");
 // Dedicated Admin fixture: never lock or alter the seeded administrator.
 await Execute("INSERT dbo.Users(UserName,Email,PasswordHash,Role) VALUES(@u,@e,@h,'Admin')",("@u",prefix+"admin"),("@e",prefix+"admin@example.test"),("@h",PasswordHash.Create("Demo123!")));
 Check(admin.AuthenticateAdmin(prefix+"admin","Demo123!"),"Admin role can sign in with PBKDF2 password");
 Check(await portal.AuthenticateAgentAsync(prefix+"admin@example.test","Demo123!") is null,"Admin role cannot sign in to Agent portal");
 for(var i=0;i<5;i++)admin.AuthenticateAdmin(prefix+"admin","incorrect");
 Check(!admin.AuthenticateAdmin(prefix+"admin","Demo123!"),"Desktop Admin locks on fifth incorrect password");
 admin.ChangeStock(a.Id,5);Check(await Scalar("SELECT StockQuantity FROM dbo.Item WHERE ItemID=@id",("@id",a.Id))==15,"Inventory adjustment adds requested quantity");
 await ExpectFailure(()=>Task.Run(()=>admin.ChangeStock(a.Id,-999)),"Inventory adjustment cannot make stock negative");
 var updated=admin.GetItems().Single(x=>x.Id==a.Id);admin.SaveItem(updated with {Price=150});
 Check(admin.GetItems().Single(x=>x.Id==a.Id).Price==150,"Current-version product edit persists successfully");
 if(args.Contains("--browser",StringComparer.OrdinalIgnoreCase))
 {
  var browserScript=Path.Combine(root,"tools/verify-portal-ui.cjs");
  var browserCommand=new ProcessStartInfo(Environment.GetEnvironmentVariable("NODE_EXE")??"node") { UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=root };
  browserCommand.ArgumentList.Add(browserScript);
  browserCommand.Environment["DEMO_AGENT_EMAIL"]=prefix+"a@example.test";
  browserCommand.Environment["DEMO_AGENT_PASSWORD"]="Demo123!";
  browserCommand.Environment["SMOKE_PRODUCT_IDS"]=$"{a.Id},{b.Id}";
  using var browser=Process.Start(browserCommand)??throw new InvalidOperationException("Could not start the browser smoke check.");
  var browserOutput=browser.StandardOutput.ReadToEndAsync();var browserError=browser.StandardError.ReadToEndAsync();
  using var browserLimit=new CancellationTokenSource(TimeSpan.FromMinutes(2));
  await browser.WaitForExitAsync(browserLimit.Token);
  if(browser.ExitCode!=0)throw new InvalidOperationException("Browser checkout failed: "+await browserError+await browserOutput);
  Check(true,"Chrome completes two-product checkout, demo payment, My Orders, printable invoice, and mobile layout");
 }
 admin.SaveAgent(agent with {Address=prefix+" Updated Address"});
 Check(admin.GetAgents().Any(x=>x.Id==agent.Id&&x.Address==prefix+" Updated Address"),"Company edit persists through Admin store");
 admin.SaveItem(new(0,prefix+" Deactivation Fixture","1 pack",25,2));
 var deactivationItem=admin.GetItems().Single(x=>x.Name==prefix+" Deactivation Fixture");
 admin.DeactivateItem(deactivationItem.Id);
 Check(await Scalar("SELECT CONVERT(INT,IsActive) FROM dbo.Item WHERE ItemID=@id",("@id",deactivationItem.Id))==0&&!admin.GetItems().Any(x=>x.Id==deactivationItem.Id),"Product deactivation hides the item without deleting its row");
}
finally
{
 if(triggerCreated)await Execute($"DROP TRIGGER dbo.[{trigger}]");
 await Execute("DELETE FROM dbo.[Order] WHERE AgentID IN(SELECT AgentID FROM dbo.Agent WHERE AgentName LIKE @p); DELETE FROM dbo.Users WHERE UserName LIKE @p; DELETE FROM dbo.Item WHERE ItemName LIKE @p; DELETE FROM dbo.Agent WHERE AgentName LIKE @p;",("@p",prefix+"%"));
 var final=await Counts();Check(final==baseline,"Fixture cleanup restores all five table row counts");
 var output=Path.Combine(root,"docs/evidence");Directory.CreateDirectory(output);
 await File.WriteAllLinesAsync(Path.Combine(output,"integration-results.txt"),log.Append($"TOTAL: {passed} passed. Baseline/final Agent/Item/Order/Detail/Users = {baseline} / {final}"));
 await File.WriteAllTextAsync(Path.Combine(output,"integration-results.json"),JsonSerializer.Serialize(new{date=DateTimeOffset.Now,passed,baseline,final,checks},new JsonSerializerOptions{WriteIndented=true}));
}
Console.WriteLine($"Completed {passed} SQL integration checks.");
