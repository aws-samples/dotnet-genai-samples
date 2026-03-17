# Example Transformations - VB6 to C# Mapping

## Example 1: VB6 Form Load → Blazor OnInitializedAsync

### Before (VB6 - frmCustomers.frm)

Private Sub Form_Load()
     Dim rs As ADODB.Recordset
     Set rs = New ADODB.Recordset
     rs.Open "SELECT * FROM Customers", cn, adOpenStatic
     Set MSFlexGrid1.DataSource = rs
End Sub

### After (C# - Customers.razor)

@code {
     private List<Customer> customers = [];

    protected override async Task OnInitializedAsync()
     {
         customers = await DbContext.Customers.ToListAsync();
     }
}

## Example 2: VB6 Module Function → C# Extension Method

### Before (VB6 - modFunctions.bas)

Public Function FormatCurrency(ByVal amount As Double) As String
     FormatCurrency = Format(amount, "$#,##0.00")
End Function

### After (C# - Extensions/CurrencyExtensions.cs)

namespace SKS.Extensions;

public static class CurrencyExtensions
{
     public static string FormatAsCurrency(this decimal amount)
         => amount.ToString("C2");
}

## Example 3: VB6 Error Handling → C# Structured Exception

### Before (VB6)

On Error GoTo ErrorHandler
     cn.Execute "UPDATE Products SET Price = " & txtPrice.Text
     Exit Sub
ErrorHandler:
     MsgBox "Error: " & Err.Description

### After (C#)

try
{
     await _dbContext.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
     _logger.LogError(ex, "Failed to update product price. CorrelationId: {CorrelationId}", correlationId);
     throw;
}