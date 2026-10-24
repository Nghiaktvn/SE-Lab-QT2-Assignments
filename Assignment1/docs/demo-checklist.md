# Kiosk demonstration
1. Run SQL setup, configure appsettings.json, then launch the WinForms project.
2. Show the 3D welcome banner; pause and resume it.
3. Select a destination. Confirm fare and choose Card, Bank QR or Wallet.
4. Choose Approved; Continue; simulate payment. Show SQL row, opaque ticket QR, service-day expiry and print preview.
5. New journey: select Declined. Show no usable ticket and safe retry.
6. New journey: select Timeout. Show that New journey is blocked until Reconcile.
7. Select Printer fault. Recover the same ticket, or demonstrate a simulated refund.
8. Run all 21 MSTest cases and the 5-check opt-in SQL harness. Show model generation and open engineering/DomainCodeMap.dgml in supporting Visual Studio.
9. Supply actual Visual Studio operation captures and genuine team Git/PR records before submission.
