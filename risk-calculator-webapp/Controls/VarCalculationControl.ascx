<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="VarCalculationControl.ascx.cs" Inherits="risk_calculator_webapp.Controls.VarCalculationControl" %>

<div class="var-calculation-control">
    <asp:Panel ID="pnlForm" runat="server" CssClass="form-panel">
        <div class="form-group">
            <label for="txtSymbol">Stock Symbol:</label>
            <asp:TextBox ID="txtSymbol" runat="server" CssClass="form-control"></asp:TextBox>
            <asp:RequiredFieldValidator ID="rfvSymbol" runat="server" 
                ControlToValidate="txtSymbol" 
                ErrorMessage="Stock symbol is required"
                CssClass="error-message" />
        </div>
        
        <div class="form-group">
            <label for="txtAmount">Amount ($):</label>
            <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control"></asp:TextBox>
            <asp:RequiredFieldValidator ID="rfvAmount" runat="server" 
                ControlToValidate="txtAmount" 
                ErrorMessage="Amount is required"
                CssClass="error-message" />
            <asp:RegularExpressionValidator ID="revAmount" runat="server" 
                ControlToValidate="txtAmount" 
                ValidationExpression="^\d+(\.\d{1,2})?$"
                ErrorMessage="Please enter a valid amount"
                CssClass="error-message" />
            <asp:RangeValidator ID="rvAmount" runat="server"
                ControlToValidate="txtAmount"
                Type="Double"
                MinimumValue="0.01"
                MaximumValue="999999999.99"
                ErrorMessage="Amount must be between $0.01 and $999,999,999.99"
                CssClass="error-message" />
        </div>
        
        <div class="form-group">
            <asp:Button ID="btnCalculate" runat="server" Text="Calculate Risk" 
                CssClass="btn btn-primary" OnClick="btnCalculate_Click" />
            <asp:Button ID="btnReset" runat="server" Text="Reset" 
                CssClass="btn btn-secondary" OnClick="btnReset_Click" />
        </div>
    </asp:Panel>
    
    <asp:Panel ID="pnlResults" runat="server" CssClass="results-panel" Visible="false">
        <h3>Risk Analysis for <asp:Label ID="lblSymbol" runat="server"></asp:Label></h3>
        
        <table class="results-table">
            <tr>
                <td>Investment Amount</td>
                <td><asp:Label ID="lblAmount" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>Daily VaR (95%)</td>
                <td><asp:Label ID="lblVaR" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>Credit Risk Score</td>
                <td><asp:Label ID="lblCreditRisk" runat="server"></asp:Label></td>
            </tr>
            <tr>
                <td>Risk Level</td>
                <td><asp:Label ID="lblRiskLevel" runat="server" CssClass="risk-level"></asp:Label></td>
            </tr>
        </table>
        
        <div class="form-group">
            <asp:Button ID="btnNewCalculation" runat="server" Text="New Calculation" 
                CssClass="btn btn-secondary" OnClick="btnNewCalculation_Click" />
        </div>
    </asp:Panel>
    
    <div class="error-panel">
        <asp:Label ID="lblError" runat="server" CssClass="error-message" Visible="false"></asp:Label>
    </div>
    
    <asp:HiddenField ID="hfLastCalculation" runat="server" />
    <asp:HiddenField ID="hfCalculationCount" runat="server" Value="0" />
</div>