<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RiskAlerts.aspx.cs" Inherits="risk_calculator_webapp.RiskAlerts" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Risk Alerts - Risk Calculator</title>
    <link href="Content/Site.css" rel="stylesheet" type="text/css" />
    <!--[if IE 6]>
        <style type="text/css">
            .app-layout { width: 998px !important; }
            .png { behavior: url(iepngfix.htc); }
        </style>
    <![endif]-->
    <!--[if IE 7]>
        <style type="text/css">
            .app-layout { width: 998px !important; }
        </style>
    <![endif]-->
    <!--[if IE 8]>
        <style type="text/css">
            .app-layout { border-collapse: separate; }
        </style>
    <![endif]-->
</head>
<body>
    <form id="form1" runat="server">
        <!-- Classic Table-Based Layout for 2008-2012 Era -->
        <table class="app-layout clearfix" cellpadding="0" cellspacing="0">
            <colgroup>
                <col width="160">
                <col width="838">
            </colgroup>
            <tr>
                <td colspan="2" class="app-header">
                    <h1 style="margin: 0; font-size: 16px; font-weight: bold;">Risk Monitoring & Alerts</h1>
                    <p style="margin: 5px 0 0 0; font-size: 11px;">Get automatic notifications when your investments become too risky</p>
                </td>
            </tr>
            <tr>
                <td class="app-sidebar" width="160" valign="top">
                    <ul class="sidebar-nav">
                        <li class="sidebar-nav-item">
                            <a href="Default.aspx" class="sidebar-nav-link">
                                Dashboard
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="CalculateVar.aspx" class="sidebar-nav-link">
                                Investment Risk Assessment
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="Portfolio.aspx" class="sidebar-nav-link">
                                Portfolio Risk Analysis
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="MonteCarlo.aspx" class="sidebar-nav-link">
                                Stress Testing
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="RiskAlerts.aspx" class="sidebar-nav-link active">
                                Risk Monitoring
                            </a>
                        </li>
                    </ul>
                </td>
                <td class="app-main" width="838" valign="top">
                    <!-- Alert Management Card -->
                    <div class="card">
                        <div class="card-header">
                            <h2 class="card-title">Alert Management</h2>
                            <p class="card-subtitle">Control and review active risk alerts</p>
                        </div>
                        <div class="card-body">
                            <div class="stat-card">
                                <div class="stat-value"><asp:Label ID="lblAlertCount" runat="server" Text="0"></asp:Label></div>
                                <div class="stat-label">Active Alerts</div>
                            </div>

                            <div class="alerts-controls" style="margin-top: 15px; text-align: center;">
                                <asp:Button ID="btnClearAlerts" runat="server" Text="Clear All Alerts"
                                    CssClass="btn btn-danger" OnClick="btnClearAlerts_Click" />
                            </div>
                        </div>
                    </div>

                    <!-- Active Alerts - Full Width -->
                    <div class="card">
                        <div class="card-header">
                            <h3 class="card-title">Active Risk Alerts</h3>
                            <p class="card-subtitle">Current system alerts and notifications</p>
                        </div>
                        <div class="card-body">
                            <asp:Panel ID="pnlNoAlerts" runat="server" CssClass="no-alerts-panel" Visible="false">
                                <div class="alert alert-info" style="text-align: center; padding: 30px;">
                                    <h4 style="margin-bottom: 12px; color: #0c5460;"><strong>No Risk Alerts</strong></h4>
                                    <p style="margin: 0; color: #0c5460;">No risk alerts at this time. The system will automatically generate alerts when risk thresholds are exceeded.</p>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlAlertsList" runat="server" CssClass="alerts-list-panel" Visible="false">
                                <asp:Repeater ID="rptAlerts" runat="server">
                                    <ItemTemplate>
                                        <div class="card" style="margin-bottom: 12px; border-left: 4px solid #f39c12;">
                                            <div class="card-body" style="padding: 15px;">
                                                <table width="100%" cellpadding="0" cellspacing="0">
                                                    <tr>
                                                        <td style="font-weight: bold; color: #000000;"><%# Eval("Message") %></td>
                                                        <td align="right" style="color: #666666; font-size: 10px;"><%# Eval("Time") %></td>
                                                    </tr>
                                                </table>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </asp:Panel>
                        </div>
                        <div class="card-footer">
                            <strong>Last Updated:</strong> <asp:Label ID="lblLastUpdated" runat="server" Text="N/A"></asp:Label>
                        </div>
                    </div>

                    <!-- Alert Information -->
                    <div class="card">
                        <div class="card-header">
                            <h3 class="card-title">Alert Information</h3>
                        </div>
                        <div class="card-body">
                            <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                The Risk Alert System monitors portfolio metrics and generates notifications when predefined thresholds are exceeded.
                            </p>

                            <ul style="margin: 0; padding-left: 15px; line-height: 1.6;">
                                <li style="margin-bottom: 6px;"><strong>Threshold Monitoring:</strong> Continuous risk level assessment</li>
                                <li style="margin-bottom: 6px;"><strong>Real-time Alerts:</strong> Instant notifications on risk events</li>
                                <li style="margin-bottom: 6px;"><strong>Historical Tracking:</strong> Alert history and patterns</li>
                                <li style="margin-bottom: 6px;"><strong>Custom Thresholds:</strong> Configurable alert parameters</li>
                            </ul>

                            <div class="btn-group clearfix" style="margin-top: 15px;">
                                <asp:HyperLink ID="lnkPortfolio" runat="server" NavigateUrl="~/Portfolio.aspx" CssClass="btn btn-primary">
                                    View Portfolio Status
                                </asp:HyperLink>
                                <asp:HyperLink ID="lnkCalculateVar" runat="server" NavigateUrl="~/CalculateVar.aspx" CssClass="btn btn-secondary">
                                    Calculate Current VaR
                                </asp:HyperLink>
                                <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/Default.aspx" CssClass="btn btn-secondary">
                                    ← Back to Dashboard
                                </asp:HyperLink>
                            </div>
                        </div>
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="2" class="app-footer">
                    <p>Risk Alert System - Financial Risk Management System v2.1</p>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
