<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PortfolioGridControl.ascx.cs" Inherits="risk_calculator_webapp.Controls.PortfolioGridControl" %>

<div class="portfolio-grid-control">
    <div class="portfolio-header">
        <h3>Current Portfolio Holdings</h3>
        <asp:Button ID="btnRefresh" runat="server" Text="Refresh Data" 
            CssClass="btn btn-primary" OnClick="btnRefresh_Click" />
    </div>
    
    <table id="tblPortfolio" class="display" style="width:100%">
        <thead>
            <tr>
                <th>Symbol</th>
                <th>Value ($)</th>
                <th>Daily VaR</th>
                <th>Risk %</th>
                <th>Risk Level</th>
            </tr>
        </thead>
        <tbody id="tblPortfolioBody" runat="server">
        </tbody>
    </table>
    <script>
        $(document).ready(function () {
            $('#tblPortfolio').DataTable({
                paging: true,
                searching: true,
                ordering: true
            });
        });
    </script>
    
    <div class="portfolio-summary">
        <h4>Portfolio Summary</h4>
        <table class="summary-table">
            <tr>
                <td>Total Portfolio Value:</td>
                <td><asp:Label ID="lblTotalValue" runat="server" CssClass="summary-value"></asp:Label></td>
            </tr>
            <tr>
                <td>Total Daily VaR:</td>
                <td><asp:Label ID="lblTotalVaR" runat="server" CssClass="summary-value"></asp:Label></td>
            </tr>
            <tr>
                <td>Portfolio Risk Ratio:</td>
                <td><asp:Label ID="lblRiskRatio" runat="server" CssClass="summary-value"></asp:Label></td>
            </tr>
            <tr>
                <td>Last Updated:</td>
                <td><asp:Label ID="lblLastUpdated" runat="server" CssClass="summary-value"></asp:Label></td>
            </tr>
        </table>
    </div>
    
    <asp:HiddenField ID="hfPortfolioData" runat="server" />
    <asp:HiddenField ID="hfLastRefreshTime" runat="server" />
</div>
