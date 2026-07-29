<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="risk_calculator_webapp.Default" %>
<%@ Register Src="~/Controls/NavigationControl.ascx" TagName="NavigationControl" TagPrefix="rc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Risk Calculator - Financial Risk Management System</title>
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
                    <h1 style="margin: 0; font-size: 16px; font-weight: bold;">Financial Risk Management System v2.1</h1>
                    <p style="margin: 5px 0 0 0; font-size: 11px;">Helping investment professionals and portfolio managers make smarter, data-driven risk decisions</p>
                </td>
            </tr>
            <tr>
                <td class="app-sidebar" width="160" valign="top">
                    <ul class="sidebar-nav">
                        <li class="sidebar-nav-item">
                            <a href="Default.aspx" class="sidebar-nav-link active">
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
                            <a href="RiskAlerts.aspx" class="sidebar-nav-link">
                                Risk Monitoring
                            </a>
                        </li>
                    </ul>
                </td>
                <td class="app-main" width="838" valign="top">
                    <div class="card">
                        <div class="card-header">
                            <h2 class="card-title">Welcome to Your Risk Management Dashboard</h2>
                            <p class="card-subtitle">Analyze investment risk, protect your portfolio, and make confident investment decisions</p>
                        </div>
                        <div class="card-body">
                            <p><strong>Perfect for:</strong> Portfolio managers, financial advisors, risk analysts, and serious investors who need to understand and manage investment risk.</p>
                            <p><strong>What you can do:</strong> Calculate potential losses, analyze portfolio risk, run stress-test scenarios, and set up automated risk monitoring alerts.</p>

                            <!-- Replace CSS Grid with Table Layout -->
                            <table class="dashboard-table" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td valign="top">
                                        <div class="card">
                                            <div class="card-header">
                                                <h3 class="card-title">What You Can Accomplish</h3>
                                            </div>
                                            <div class="card-body">
                                                <ul style="margin: 0; padding-left: 15px;">
                                                    <li style="margin-bottom: 8px;"><strong>Investment Risk Assessment</strong> - Calculate potential losses on individual stocks and bonds</li>
                                                    <li style="margin-bottom: 8px;"><strong>Portfolio Risk Analysis</strong> - See total portfolio risk and identify your riskiest holdings</li>
                                                    <li style="margin-bottom: 8px;"><strong>Stress Testing</strong> - Run "what-if" scenarios to see how market crashes might affect you</li>
                                                    <li style="margin-bottom: 8px;"><strong>Risk Monitoring</strong> - Get automatic alerts when investments become too risky</li>
                                                    <li style="margin-bottom: 8px;"><strong>Data-Driven Decisions</strong> - Make investment choices based on quantified risk, not gut feelings</li>
                                                </ul>
                                            </div>
                                        </div>
                                    </td>
                                    <td valign="top">
                                        <div class="card">
                                            <div class="card-header">
                                                <h3 class="card-title">Getting Started</h3>
                                            </div>
                                            <div class="card-body">
                                                <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                                    <strong>New to risk analysis?</strong> Start with "Investment Risk Assessment" to calculate potential losses on a single stock.
                                                    Already have a portfolio? Use "Portfolio Risk Analysis" to see your overall risk exposure.
                                                </p>

                                                <!-- Replace CSS Grid with Table Layout -->
                                                <table class="stats-table" cellpadding="0" cellspacing="0">
                                                    <tr>
                                                        <td>
                                                            <div class="stat-card">
                                                                <div class="stat-value">5</div>
                                                                <div class="stat-label">Analysis Tools</div>
                                                            </div>
                                                        </td>
                                                        <td>
                                                            <div class="stat-card">
                                                                <div class="stat-value">Ready</div>
                                                                <div class="stat-label">System Status</div>
                                                            </div>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </div>

                    <!-- Keep the legacy navigation control for compatibility -->
                    <div style="display: none;">
                        <rc:NavigationControl ID="navControl" runat="server" />
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="2" class="app-footer">
                    <p>Current Time: <asp:Label ID="lblCurrentTime" runat="server"></asp:Label> | Session ID: <asp:Label ID="lblSessionId" runat="server"></asp:Label></p>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
