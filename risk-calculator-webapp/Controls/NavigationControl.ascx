<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="NavigationControl.ascx.cs" Inherits="risk_calculator_webapp.Controls.NavigationControl" %>

<div class="navigation">
    <table class="nav-table">
        <tr>
            <td>
                <asp:HyperLink ID="lnkCalculate" runat="server" NavigateUrl="~/CalculateVar.aspx" CssClass="nav-link">
                    Calculate VaR
                </asp:HyperLink>
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkPortfolio" runat="server" NavigateUrl="~/Portfolio.aspx" CssClass="nav-link">
                    View Portfolio
                </asp:HyperLink>
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkMonteCarlo" runat="server" NavigateUrl="~/MonteCarlo.aspx" CssClass="nav-link">
                    Monte Carlo Simulation
                </asp:HyperLink>
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkAlerts" runat="server" NavigateUrl="~/RiskAlerts.aspx" CssClass="nav-link">
                    Risk Alerts
                </asp:HyperLink>
            </td>
        </tr>
    </table>
</div>